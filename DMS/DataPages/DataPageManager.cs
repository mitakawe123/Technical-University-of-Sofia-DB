using DataStructures;
using DMS.Constants;
using DMS.Extensions;
using DMS.Shared;
using DMS.Utils;
using System.Text;
using static DMS.Utils.ControlTypes;

namespace DMS.DataPages
{
    //My logic
    //8 bytes for data page count and offset page count
    //8KB bytes for header section if i go over the 8KB I need to make buffer 4 bytes at the end of the header to point to the next free 8KB and so on
    //Inside this 8KB will be 4 bytes integers for the offset of each offset table
    //Inside each offset table will be header section with the name of the table and after that there will be the data page numbers if the offset table overflow make a buffer and point to the next
    //Inside each data page will be header section with 4 byte free space, 4 byte record count then the content (here I will not make a buffer because the offset table will take care of pointing which data pages for which table are)
    public class DataPageManager
    {
        private const int DataPageSize = 8192; // 8KB
        private const int BufferOverflowPointer = 4; //4 bytes for pointer to next page

        private static int CounterSection = 20; // 20 bytes for table count and data page count
        private static int HeaderCounter = 0; // 4 bytes for header counter
        private static int DataPageCounter = 0; // 4 bytes for data page count  
        private static int OffsetPageCounter = 0; // 4 bytes for offset page count
        private static int AllDataPagesCount = 0; // 4 bytes for data page count
        private static int TablesCount = 0; // 4 bytes for table count

        private static Dictionary<char[], DKList<int>> tableOffsets = new();

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
            ulong totalSpaceForColumnTypes = HelperAllocater.AllocatedStorageForTypes(columns);
            int numberOfDataPagesNeeded = (int)Math.Ceiling((double)totalSpaceForColumnTypes / DataPageSize);

            InitHeader(table);

            TablesCount++;
            /*int currentPage = TablesCount;
            int columnIndex = 0;
            int freeSpace = DataPageSize;
            bool firstDataPageForTable = true;

            while (numberOfPagesNeeded > 0)
            {
                binaryStream.Seek((currentPage * DataPageSize) + PagesCountSize + HeaderSize, SeekOrigin.Begin);

                // Write table name and column count on the first page only
                if (firstDataPageForTable)
                {
                    writer.Write(tableName);
                    writer.Write(columns.Count);
                    firstDataPageForTable = false;
                    tableOffsets.Add(table, new DKList<int>() { currentPage });
                }

                // Write as many columns as fit on the current page
                while (columnIndex < columns.Count &&
                    (freeSpace - 4) > CalculateColumnSize(columns[columnIndex]))
                {
                    writer.Write(columns[columnIndex].Type);
                    writer.Write(columns[columnIndex].Name);
                    freeSpace -= CalculateColumnSize(columns[columnIndex]);
                    columnIndex++;
                }

                // If there are more columns to write, store a reference to the next page
                if (columnIndex < columns.Count)
                {
                    // Last 4 bytes of each page store the next page number
                    binaryStream.Seek(((currentPage + 1) * DataPageSize - 4) + PagesCountSize, SeekOrigin.Begin);
                    writer.Write(currentPage + 1); // Next page number
                    freeSpace = DataPageSize;
                    tableOffsets[table].Add(currentPage + 1);
                }

                currentPage++;
                numberOfPagesNeeded--;
            }*/

        }

        public static bool DropTable(ReadOnlySpan<char> tableName)
        {
            char[] tableNameAsChars = tableName.CustomToArray();
            char[]? matchingKey = null;

            foreach (KeyValuePair<char[], DKList<int>> keyValuePair in tableOffsets)
            {
                if (tableName.SequenceEqual(keyValuePair.Key))
                {
                    matchingKey = keyValuePair.Key;
                    break;
                }
            }

            if (matchingKey is null)
                return false;

            FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryWriter writer = new(binaryStream);

            DKList<int> pageNumbers = tableOffsets[matchingKey];

            for (int i = 0; i < pageNumbers.Count; i++)
            {
                //binaryStream.Seek((pageNumbers[i] * DataPageSize) + PagesCountSize, SeekOrigin.Begin);
                writer.Write(new byte[DataPageSize]);
            }

            tableOffsets.Remove(matchingKey);

            binaryStream.Close();
            writer.Close();

            DeleteOffsetMapperByKey(tableName);

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

        private static void InitHeader(char[] table)
        {
            if (table.Length > 50)
                throw new Exception("Table name is too long");

            int requiredSpace = table.Length + sizeof(int) + sizeof(int);
            if (HeaderCounter == 0)
            {
                using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
                using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

                HeaderCounter++;
                AllDataPagesCount++;
                binaryStream.Seek(CounterSection, SeekOrigin.Begin);

                writer.Write(DataPageSize - requiredSpace); // 4 bytes for free space
                writer.Write(table.Length); // 4 bytes
                writer.Write(table); // 1 byte per char

                binaryStream.Seek(CounterSection + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
                writer.Write(-1); // 4 bytes for pointer to next header
                return;
            }

            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fs, Encoding.UTF8);
            fs.Seek(CounterSection, SeekOrigin.Begin);

            int freeSpace = reader.ReadInt32();
            fs.Seek(CounterSection + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
            int nextHeaderPointer = reader.ReadInt32();
            if (freeSpace < requiredSpace + BufferOverflowPointer)
            {
                if (nextHeaderPointer == -1)
                {
                    int offset = CounterSection + (DataPageSize * AllDataPagesCount);
                    fs.Close();
                    reader.Close();

                    using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Append);
                    using BinaryWriter writer = new(fileStream, Encoding.UTF8);

                    fileStream.Seek(CounterSection + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
                    writer.Write(offset); // 4 bytes for pointer to next header

                    fileStream.Seek(offset, SeekOrigin.Begin);
                    HeaderCounter++;
                    AllDataPagesCount++;

                    writer.Write(DataPageSize - requiredSpace); // 4 bytes for free space for the new data page
                    writer.Write(table.Length); // 4 bytes for table name length
                    writer.Write(table); // 1 byte per char for table name
                }
            }
            else
            {
                fs.Close();
                reader.Close();

                using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Append);
                using BinaryWriter binaryWriter = new(fileStream, Encoding.UTF8);

                fileStream.Seek(nextHeaderPointer, SeekOrigin.Begin);

                binaryWriter.Write(freeSpace - requiredSpace); // 4 bytes for free space
                binaryWriter.Write(table.Length); // 4 bytes for table name length
                binaryWriter.Write(table); // 1 byte per char for table name
            }
        }

        private static void InitOffsetSection(char[] table, int dataPagesCount) //<- number of pages * sizeof(int)
        {
            InitHeader(table);

            int startingPositionOfOffset = (AllDataPagesCount * DataPageSize) + CounterSection + (DataPageSize * (HeaderCounter + 1));
            AllDataPagesCount++;

            if ((dataPagesCount * sizeof(int)) + BufferOverflowPointer > DataPageSize)
            {
                int freeSpaceInOffsetTable = DataPageSize;
                int pageCounter = 0;
                bool firstOffsetTablePage = true;



                using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
                using BinaryWriter writer = new(binaryStream);

                while (dataPagesCount != pageCounter)
                {
                    binaryStream.Seek(startingPositionOfOffset, SeekOrigin.Begin);

                    if (firstOffsetTablePage)
                    {
                        writer.Write(freeSpaceInOffsetTable); // 4 bytes for free space
                        firstOffsetTablePage = false;
                    }

                    // Write as many data page numbers as fit on the offset table
                    while (freeSpaceInOffsetTable > BufferOverflowPointer - sizeof(int)) // check if there is space for the pointer and data page number which both are integers
                    {
                        writer.Write(pageCounter);
                        freeSpaceInOffsetTable -= sizeof(int);
                        pageCounter++;
                    }

                    /*// If there are more data page number, store a reference to the next page
                    if (dataPagesCount > pageCounter)
                    {
                        // Last 4 bytes of each page store the next page number
                        binaryStream.Seek((currentPage + 1) * DataPageSize - sizeof(int) + PagesCountSize + HeaderSize, SeekOrigin.Begin);
                        writer.Write(currentPage + 1); // Next offset page number pointer
                        freeSpaceInOffsetTable = DataPageSize;
                    }

                    currentPage++;*/
                }

                //TablesCount = currentPage;
            }
        }

        private static int CalculateColumnSize(Column column)
        {
            int typeSize = (sizeof(char) * column.Type.Length); // 2 bytes per char
            int nameSize = (sizeof(char) * column.Name.Length); // 2 bytes per char
            int columnSize = (int)HelperAllocater.CalculateColumnSize(column);

            return typeSize + nameSize + columnSize;
        }
        //createtable test(id int primary key, name string(max) null, name1 string(max) null)
        private static void WriteOffsetMapper(KeyValuePair<char[], DKList<int>> entry)
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
            using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

            int sizeOfCurrentRecord = sizeof(int) + entry.Key.Length + sizeof(int) + (sizeof(int) * entry.Value.Count);
            //header of offset page
            writer.Write(entry.Key.Length); // 4 bytes
            writer.Write(entry.Key); // 1 byte per char

            // Writing data pages for the given table
            writer.Write(entry.Value.Count);// 4 bytes

            //case where the content is bigger than 4 KB
            if ((entry.Value.Count * sizeof(int)) + sizeof(int) + entry.Key.Length - 4 > DataPageSize)
            {
                int currentOffsetPage = 0;
                int freeSpace = DataPageSize - entry.Key.Length - sizeof(int);
                bool firstOffsetPageForTable = true;
                DKList<int> dataPageNumbers = entry.Value;


            }

            foreach (int value in entry.Value)
                writer.Write(value); // 4 bytes per value
        }

        private static Dictionary<char[], DKList<int>> ReadOffsetMapper()
        {
            return default;
            /*Dictionary<char[], DKList<int>> result = new();

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(-SizeOfOffsetRecords, SeekOrigin.End);

            while (binaryStream.Position < binaryStream.Length)
            {
                int tableNameLength = reader.ReadInt32();
                byte[] tableNameAsBytes = reader.ReadBytes(tableNameLength);
                char[] tableName = Encoding.UTF8.GetString(tableNameAsBytes).CustomToArray();

                int listCount = reader.ReadInt32();
                DKList<int> list = new(listCount);

                for (int i = 0; i < listCount; i++)
                    list.Add(reader.ReadInt32());

                result.Add(tableName, list);
            }

            return result;*/
        }

        private static void DeleteOffsetMapperByKey(ReadOnlySpan<char> tableName)
        {
            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter writer = new(fs, Encoding.UTF8);

            char[] tableNameArray = tableName.CustomToArray();
            bool entryFound = false;

            foreach (char[] key in tableOffsets.Keys)
            {
                if (key.SequenceEqual(tableNameArray))
                {
                    //emtpy the space in the offset section and delete data pages

                    tableOffsets.Remove(key);
                    entryFound = true;
                    break;
                }
            }

            if (!entryFound)
                Console.WriteLine("Table name not found in offset mapper.");
        }

        private static ulong FreeSpaceDataPage(int pageNumber)
        {
            byte[] buffer = new byte[DataPageSize];

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream);

            binaryStream.Seek(pageNumber * DataPageSize, SeekOrigin.Begin);

            ulong bytesRead = (ulong)reader.Read(buffer, 0, DataPageSize);

            return DataPageSize - bytesRead;
        }

        // 8 bytes for data page count and offset page count
        private static void PagesCountSection()
        {
            try
            {
                using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
                using BinaryReader reader = new(fs);
                fs.Seek(0, SeekOrigin.Begin);

                TablesCount = reader.ReadInt32();
                AllDataPagesCount = reader.ReadInt32();
                OffsetPageCounter = reader.ReadInt32();
                DataPageCounter = reader.ReadInt32();
                HeaderCounter = reader.ReadInt32();
            }
            catch (Exception)
            {
                using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.CreateNew);
                using BinaryWriter writer = new(fs);
                fs.Seek(0, SeekOrigin.Begin);

                writer.Write(TablesCount);
                writer.Write(AllDataPagesCount);
                writer.Write(OffsetPageCounter);
                writer.Write(DataPageCounter);
                writer.Write(HeaderCounter);
            }

            tableOffsets = ReadOffsetMapper();
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
            using BinaryWriter writer = new(binaryStream);

            binaryStream.Seek(0, SeekOrigin.Begin);

            writer.Write(TablesCount);
            writer.Write(AllDataPagesCount);
            writer.Write(OffsetPageCounter);
            writer.Write(DataPageCounter);
            writer.Write(HeaderCounter);
        }
    }
}
