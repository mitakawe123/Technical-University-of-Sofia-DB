using DMS.Constants;
using DMS.Extensions;
using DMS.OffsetPages;
using DMS.Shared;
using DMS.Utils;
using System.Text;
using static DMS.Utils.ControlTypes;

namespace DMS.DataPages
{
    //in Microsoft SQL Server, there is a limit on the length of table names.
    //The maximum length allowed for a table name is 128 characters.
    //This limit is applicable not just to table names but also to most other identifiers in SQL Server, such as column names, schema names, constraint names, and others.
    public class DataPageManager
    {
        private const int CounterSection = 16; // 16 bytes for table count, data page count, all data pages count and offset table start
        private const int DefaultBufferForDP = -1;

        public const int BufferOverflowPointer = 4; //4 bytes for pointer to next page
        public const int DataPageSize = 8192; // 8KB
        
        public static int TablesCount = 0; // 4 bytes for table count
        public static int DataPageCounter = 0; // 4 bytes for data page count  
        public static int AllDataPagesCount = 0; // 4 bytes for data page count
        public static int FirstOffsetPageStart = 0; // 4 bytes for offset table 

        private static Dictionary<char[], long> tableOffsets = new();// <-name of the table and start of the offset for the first data page

        private static bool isclosing = false;

        static DataPageManager()
        {
            PagesCountSection();

            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);
        }

        //createtable test1(id int primary key, name string(max) null, name1 string(max) null)
        public static void CreateTable(IReadOnlyList<Column> columns, ReadOnlySpan<char> tableName)
        {
            char[] table = tableName.CustomToArray();
            if (tableOffsets.ContainsKey(table))
                throw new Exception("Table already exists");

            if (columns.CustomAny(x => x.Name.Length > 128))
                throw new Exception("Column name is too long");

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
            using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

            ulong totalSpaceForColumnTypes = HelperAllocater.AllocatedStorageForTypes(columns);
            int numberOfPagesNeeded = (int)Math.Ceiling((double)totalSpaceForColumnTypes / DataPageSize);

            int currentPage = AllDataPagesCount;
            int pageNum = 0;
            int columnIndex = 0;
            int freeSpaceTemp = DataPageSize;
            int freeSpace = DataPageSize;

            if (DataPageCounter == 0)
                FirstOffsetPageStart = (numberOfPagesNeeded * DataPageSize) + CounterSection;

            while (numberOfPagesNeeded > 0)
            {
                binaryStream.Seek((currentPage * DataPageSize) + CounterSection, SeekOrigin.Begin);

                freeSpace -= 16 + (2 * tableName.Length);

                //header section for the table data page is 16 bytes plus 2 bytes per char for the table name
                writer.Write(freeSpace);// 4 bytes for free space
                writer.Write(HelperAllocater.AllocatedStorageForTypes(columns));// 8 bytes for record size
                writer.Write(tableName);// 2 bytes per char
                writer.Write(columns.Count);// 4 bytes for column count

                if (!tableOffsets.ContainsKey(table))
                    tableOffsets.Add(table, (currentPage * DataPageSize) + CounterSection);

                // Write as many columns as fit on the current page
                while (columnIndex < columns.Count &&
                    (freeSpaceTemp - 4) > CalculateColumnSize(columns[columnIndex]))
                {
                    writer.Write(columns[columnIndex].Type);//2 bytes per char
                    writer.Write(columns[columnIndex].Name);//2 bytes per char

                    freeSpace -= (2 * columns[columnIndex].Type.Length) + (2 * columns[columnIndex].Name.Length);
                    freeSpaceTemp -= CalculateColumnSize(columns[columnIndex]);
                    columnIndex++;
                }

                // If there are more columns to write, store a reference to the next page
                if (columnIndex < columns.Count)
                {
                    //update free space in DP
                    binaryStream.Seek((currentPage * DataPageSize) + CounterSection, SeekOrigin.Begin);
                    writer.Write(freeSpace);

                    // Last 4 bytes of each page store the next page number
                    binaryStream.Seek((currentPage + 1) * DataPageSize + CounterSection - BufferOverflowPointer, SeekOrigin.Begin);
                    writer.Write(((currentPage + 1) * DataPageSize) + CounterSection); // Next page number
                    freeSpaceTemp = DataPageSize;
                    freeSpace = DataPageSize;
                }

                currentPage++;
                pageNum++;
                numberOfPagesNeeded--;
            }

            //update the pointer in the last DP
            binaryStream.Seek((currentPage * DataPageSize) + CounterSection - BufferOverflowPointer, SeekOrigin.Begin);
            writer.Write(DefaultBufferForDP);

            TablesCount++;
            DataPageCounter += pageNum;
            AllDataPagesCount += pageNum;

            binaryStream.Close();
            writer.Close();

            OffsetManager.WriteOffsetMapper(tableOffsets.CustomLast());
        }

        public static bool DropTable(ReadOnlySpan<char> tableName)
        {
            //one talbe can span accross multiple data pages
            char[]? matchingKey = null;

            foreach (KeyValuePair<char[], long> keyValuePair in tableOffsets)
            {
                if (tableName.SequenceEqual(keyValuePair.Key))
                {
                    matchingKey = keyValuePair.Key;
                    break;
                }
            }

            if (matchingKey is null)
                return false;

            long startingPageOffset = tableOffsets[matchingKey];
            int numberOfDataPagesForTable = FindDataPageNumberForTable(startingPageOffset);

            FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryWriter writer = new(binaryStream);

            byte[] emptyPage = new byte[DataPageSize * numberOfDataPagesForTable];

            binaryStream.Seek(startingPageOffset, SeekOrigin.Begin);
            writer.Write(emptyPage);

            tableOffsets.Remove(matchingKey);

            binaryStream.Close();
            writer.Close();

            //need to remove it from the page offset too
            OffsetManager.RemoveOffsetRecord(matchingKey);

            TablesCount--;
            AllDataPagesCount -= numberOfDataPagesForTable;
            DataPageCounter -= numberOfDataPagesForTable;

            return true;
        }

        public static void ListTables()
        {
            if (tableOffsets.Count == 0)
            {
                Console.WriteLine("There is no tables in the DB");
                return;
            }

            foreach (char[] table in tableOffsets.Keys)
                Console.WriteLine(table);
        }

        public static void TableInfo(ReadOnlySpan<char> tableName)
        {
            char[] table = tableName.CustomToArray();
            byte[] values = OffsetManager.GetDataPageOffsetByTableName(table);

            if (values.Length == 0)
            {
                Console.WriteLine($"No table with the given name {tableName}");
                return;
            }

            int tableNameLength = BitConverter.ToInt32(values, 0);
            string tempTableName = Encoding.UTF8.GetString(values, sizeof(int), tableNameLength);
            char[] extractedTableName = tempTableName.ToCharArray();

            /*int offsetValueStartPosition = sizeof(int) + tableNameLength;
            int offsetValue = BitConverter.ToInt32(values, offsetValueStartPosition);*/
            char[]? tableFromOffset = null;
            foreach (KeyValuePair<char[], long> item in tableOffsets)
            {
                if (item.Key.SequenceEqual(extractedTableName))
                {
                    tableFromOffset = item.Key;
                    break;
                }
            }

            if(tableFromOffset is null)
            {
                Console.WriteLine("No table found with this name");
                return;
            }

            long offset = tableOffsets[tableFromOffset];
            int numberOfDataPagesForTable = FindDataPageNumberForTable(offset);

            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fileStream, Encoding.UTF8);

            fileStream.Seek(offset, SeekOrigin.Begin);

            int freeSpace = reader.ReadInt32();
            ulong recordSize = reader.ReadUInt64();
            string tableNameFromFile = Encoding.UTF8.GetString(reader.ReadBytes(tableNameLength), 0, tableNameLength);
            int columnsCount = reader.ReadInt32();

            Console.WriteLine($"Table name: {tableNameFromFile} \nOccupied space in bytes: {recordSize} \nThe table spans accross {numberOfDataPagesForTable} data pages \nColumns count is {columnsCount}");
        }

        private static int CalculateColumnSize(Column column)
        {
            int typeSize = (sizeof(char) * column.Type.Length); // 2 bytes per char
            int nameSize = (sizeof(char) * column.Name.Length); // 2 bytes per char
            int columnSize = (int)HelperAllocater.CalculateColumnSize(column);

            return typeSize + nameSize + columnSize;
        }

        private static int FindDataPageNumberForTable(long startingPosition)
        {
            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fileStream, Encoding.UTF8);
            
            fileStream.Seek(startingPosition + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);

            int counter = 1;
            int pointer = reader.ReadInt32();

            while (pointer != DefaultBufferForDP)
            {
                fileStream.Seek(pointer + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
                pointer = reader.ReadInt32();
                counter++;
            }

            return counter;
        }

        private static void PagesCountSection()
        {
            try
            {
                using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
                using BinaryReader reader = new(fs, Encoding.UTF8);
                fs.Seek(0, SeekOrigin.Begin);

                TablesCount = reader.ReadInt32();
                AllDataPagesCount = reader.ReadInt32();
                DataPageCounter = reader.ReadInt32();
                FirstOffsetPageStart = reader.ReadInt32();
            }
            catch (Exception)
            {
                using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.CreateNew);
                using BinaryWriter writer = new(fs, Encoding.UTF8);
                fs.Seek(0, SeekOrigin.Begin);

                writer.Write(TablesCount);
                writer.Write(AllDataPagesCount);
                writer.Write(DataPageCounter);
                writer.Write(FirstOffsetPageStart);
            }

            if (AllDataPagesCount != 0)
                tableOffsets = OffsetManager.ReadTableOffsets();
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    isclosing = true;
                    ConsoleEventCallback();
                    Console.WriteLine("CTRL+C received!");
                    break;

                case CtrlTypes.CTRL_BREAK_EVENT:
                    isclosing = true;
                    ConsoleEventCallback();
                    Console.WriteLine("CTRL+BREAK received!");
                    break;

                case CtrlTypes.CTRL_CLOSE_EVENT:
                    isclosing = true;
                    ConsoleEventCallback();
                    break;

                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    isclosing = true;
                    ConsoleEventCallback();
                    Console.WriteLine("User is logging off!");
                    break;
            }
            return true;
        }

        private static void ConsoleEventCallback()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.ReadWrite);
            using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(0, SeekOrigin.Begin);

            writer.Write(TablesCount);
            writer.Write(AllDataPagesCount);
            writer.Write(DataPageCounter);
            writer.Write(FirstOffsetPageStart);
        }
    }
}
