using DataStructures;
using DMS.Constants;
using DMS.Extensions;
using DMS.Shared;
using DMS.Utils;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static DMS.Utils.ControlTypes;

namespace DMS.DataPages
{
    //in Microsoft SQL Server, there is a limit on the length of table names.
    //The maximum length allowed for a table name is 128 characters.
    //This limit is applicable not just to table names but also to most other identifiers in SQL Server, such as column names, schema names, constraint names, and others.
    public class DataPageManager
    {
        private const int DataPageSize = 8192; // 8KB
        private const int BufferOverflowPointer = 4; //4 bytes for pointer to next page

        private static int CounterSection = 16; // 16 bytes for table count and data page count
        private static int DataPageCounter = 0; // 4 bytes for data page count  
        private static int AllDataPagesCount = 0; // 4 bytes for data page count
        private static int TablesCount = 0; // 4 bytes for table count
        private static int FirstOffsetPageStart = 0; // 4 bytes for offset table 

        private static Dictionary<char[], int> tableOffsets = new();// <-name of the table and start of the offset for the first data page

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
            writer.Write(-1);

            TablesCount++;
            DataPageCounter += pageNum;
            AllDataPagesCount += pageNum;

            binaryStream.Close();
            writer.Close();

            WriteOffsetMapper(tableOffsets.CustomLast());
        }

        public static bool DropTable(ReadOnlySpan<char> tableName)
        {
            /*char[] tableNameAsChars = tableName.CustomToArray();
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

            DeleteOffsetMapperByKey(tableName);*/

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

        private static int CalculateColumnSize(Column column)
        {
            int typeSize = (sizeof(char) * column.Type.Length); // 2 bytes per char
            int nameSize = (sizeof(char) * column.Name.Length); // 2 bytes per char
            int columnSize = (int)HelperAllocater.CalculateColumnSize(column);

            return typeSize + nameSize + columnSize;
        }

        private static void WriteOffsetMapper(KeyValuePair<char[], int> entry)
        {
            int freeSpace = DataPageSize;
            int sizeOfCurrentRecord = sizeof(int) + entry.Key.Length + sizeof(int);
            long pointerToNextPage = PointerToNextPage();//this is the end byte of the pointer
            long startOfFreeOffset = pointerToNextPage - DataPageSize;

            //this is the case when the there is no offset and I need to initialize it
            if (pointerToNextPage == -2)
            {
                using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
                using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

                binaryStream.Seek(FirstOffsetPageStart, SeekOrigin.Begin);

                freeSpace -= sizeOfCurrentRecord;
                writer.Write(freeSpace); // 4 bytes for free space

                writer.Write(entry.Key.Length); // 4 bytes for the length of the table name
                writer.Write(entry.Key); // 1 byte per char
                writer.Write(entry.Value);// 4 bytes for the start offset of the record

                binaryStream.Seek(FirstOffsetPageStart + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
                writer.Write(-1);

                AllDataPagesCount++;

                return;
            }

            freeSpace = FreeSpaceInOffset(sizeOfCurrentRecord);

            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter binaryWriter = new(fs, Encoding.UTF8);

            if (freeSpace == -1)
            {
                //create new offset page
                fs.Seek(startOfFreeOffset + DataPageSize, SeekOrigin.Begin);

                freeSpace = DataPageSize;
                binaryWriter.Write(freeSpace);

                binaryWriter.Write(entry.Key.Length); // 4 bytes for the length of the table name
                binaryWriter.Write(entry.Key); // 1 byte per char
                binaryWriter.Write(entry.Value);// 4 bytes for the start offset of the record

                fs.Seek(startOfFreeOffset + (DataPageSize * 2) - BufferOverflowPointer, SeekOrigin.Begin);
                binaryWriter.Write(-1);

                AllDataPagesCount++;

                return;
            }

            fs.Seek(startOfFreeOffset, SeekOrigin.Begin);

            //write to the current offset page
            long startingPoint = startOfFreeOffset + (DataPageSize - freeSpace) + sizeof(int);
            freeSpace -= sizeOfCurrentRecord;

            binaryWriter.Write(freeSpace); // 4 bytes for free space
            fs.Seek(startingPoint, SeekOrigin.Begin);

            binaryWriter.Write(entry.Key.Length); // 4 bytes for the length of the table name
            binaryWriter.Write(entry.Key); // 1 byte per char
            binaryWriter.Write(entry.Value);// 4 bytes for the start offset of the record

            fs.Seek(startOfFreeOffset + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
            binaryWriter.Write(-1);
        }

        private static long PointerToNextPage()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream);

            binaryStream.Seek(FirstOffsetPageStart + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
            try
            {
                int pointer = reader.ReadInt32();
                while (pointer != -1)
                {
                    binaryStream.Seek(pointer + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
                    pointer = reader.ReadInt32();
                }

                return binaryStream.Position;
            }
            catch
            {
                return -2;//the offset page is not initialized
            }
        }

        private static int FreeSpaceInOffset(int requiredSpace)
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream);

            binaryStream.Seek(FirstOffsetPageStart, SeekOrigin.Begin);
            int freeSpace = reader.ReadInt32();
            if (requiredSpace + BufferOverflowPointer < freeSpace)
                return freeSpace;

            return -1;
        }

        private static Dictionary<char[], int> ReadTableOffsets()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(FirstOffsetPageStart, SeekOrigin.Begin);
            int freeSpace = reader.ReadInt32();

            Dictionary<char[], int> offsetMap = new();
            ReadOffsetTable(binaryStream, reader, offsetMap);

            int nextPagePointer = reader.ReadInt32();
            while (nextPagePointer != -1)
            {
                binaryStream.Seek(nextPagePointer, SeekOrigin.Begin);
                ReadOffsetTable(binaryStream, reader, offsetMap);
                nextPagePointer = reader.ReadInt32();
            }

            return offsetMap;
        }
        //createtable test(id int primary key, name string(max) null, name1 string(max) null)
        private static void ReadOffsetTable(FileStream stream, BinaryReader reader, Dictionary<char[], int> offsetMap)
        {
            long stopPosition = stream.Position + DataPageSize - BufferOverflowPointer; //<- this sizeof(int) is free space variable

            while (stream.Position < stopPosition)
            {
                int tableNameLength = reader.ReadInt32();
                char[] tableName = reader.ReadChars(tableNameLength);
                int offsetValue = reader.ReadInt32();

                if (tableNameLength == 0)
                {
                    stream.Seek(stopPosition - BufferOverflowPointer, SeekOrigin.Begin);
                    return;
                }

                if (tableNameLength != 0 && !offsetMap.ContainsKey(tableName))
                    offsetMap.Add(tableName, offsetValue);
            }
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
                tableOffsets = ReadTableOffsets();
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
