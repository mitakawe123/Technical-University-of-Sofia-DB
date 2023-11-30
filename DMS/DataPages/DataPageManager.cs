﻿using DataStructures;
using DMS.Constants;
using DMS.Extensions;
using DMS.OffsetPages;
using DMS.Shared;
using DMS.Utils;
using Microsoft.Win32;
using System.Text;
using static DMS.Utils.ControlTypes;

namespace DMS.DataPages
{
    //in Microsoft SQL Server, there is a limit on the length of table names.
    //The maximum length allowed for a table name is 128 characters.
    //This limit is applicable not just to table names but also to most other identifiers in SQL Server, such as column names, schema names, constraint names, and others.
    public static class DataPageManager
    {
        private static DKDictionary<char[], long> _tableOffsets = new();// <-name of the table and start of the offset for the first data page
        private static int _tablesCount = 0; // 4 bytes for table count

        public const long DefaultBufferForDp = -1;// Default pointer to the next page
        public const long BufferOverflowPointer = 8; //8 bytes for pointer to next page
        public const int CounterSection = 16; // 16 bytes for table count, data page count, all data pages count and offset table start
        public const int Metadata = 20;// 20 bytes for the metadata
        public const int DataPageSize = 8192; // 8KB

        public static int DataPageCounter = 0; // 4 bytes for data page count  
        public static int AllDataPagesCount = 0; // 4 bytes for data page count
        public static int FirstOffsetPageStart = 0; // 4 bytes for offset table 

        public static DKDictionary<char[], long> TableOffsets => _tableOffsets;

        static DataPageManager()
        {
            SetConsoleCtrlHandler(ConsoleCtrlCheck, true);

            PagesCountSection();
        }

        public static void InitDataPageManager()
        {
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;//only supported on windows

            Console.WriteLine("Welcome to DMS");
        }

        public static void ConsoleEventCallback()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.ReadWrite);
            using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(0, SeekOrigin.Begin);

            writer.Write(_tablesCount);
            writer.Write(AllDataPagesCount);
            writer.Write(DataPageCounter);
            writer.Write(FirstOffsetPageStart);
        }

        public static void CreateTable(IReadOnlyList<Column> columns, ReadOnlySpan<char> tableName)
        {
            char[] table = tableName.CustomToArray();
            if (_tableOffsets.ContainsKey(table))
                throw new Exception("Table already exists");

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

            ulong totalSpaceForColumnTypes = HelperAllocater.AllocatedStorageForTypes(columns);//<- this will calc how much space one record will take
            int spaceTakenByColumnsDefinitions = HelperAllocater.SpaceTakenByColumnsDefinitions(columns);
            int numberOfPagesNeeded = (int)Math.Ceiling((double)spaceTakenByColumnsDefinitions / DataPageSize);//<- this will calc how many pages we need to store all the columns

            int pageNum = 0;
            int columnIndex = 0;
            int freeSpace = DataPageSize;
            int currentPage = AllDataPagesCount;

            if (DataPageCounter == 0)
                FirstOffsetPageStart = (numberOfPagesNeeded * DataPageSize) + CounterSection;

            while (numberOfPagesNeeded > 0)
            {
                binaryStream.Seek((currentPage * DataPageSize) + CounterSection, SeekOrigin.Begin);

                //this is the first Data page for the table and we need to write the header section only in this data page
                if (currentPage == AllDataPagesCount)
                {
                    freeSpace -= Metadata + tableName.Length;//<- minus the header section

                    //header section for the table data page is 16 bytes plus 2 bytes per char for the table name
                    writer.Write(freeSpace);// 4 bytes for free space
                    writer.Write(totalSpaceForColumnTypes);// 8 bytes for record size
                    writer.Write(table.Length); //4 bytes for the table name length
                    writer.Write(table);// 1 bytes per char
                    writer.Write(columns.Count);// 4 bytes for column count

                    if (!_tableOffsets.ContainsKey(table))
                        _tableOffsets.Add(table, (currentPage * DataPageSize) + CounterSection);
                }

                // Write as many columns as fit on the current page
                while (freeSpace - BufferOverflowPointer > HelperAllocater.SpaceTakenByColumnsDefinition(columns[columnIndex]))
                {
                    writer.Write(columns[columnIndex].Type);//2 bytes per char
                    writer.Write(columns[columnIndex].Name);//2 bytes per char

                    freeSpace -= (2 * columns[columnIndex].Type.Length) + (2 * columns[columnIndex].Name.Length);
                    columnIndex++;

                    if (columnIndex == columns.Count)
                        break;
                }

                binaryStream.Seek((currentPage * DataPageSize) + CounterSection, SeekOrigin.Begin);
                writer.Write(freeSpace);

                //I don't think I need this if statement here anymore
                // If there are more columns to write, store a reference to the next page
                if (columnIndex < columns.Count)
                {
                    //update free space in the current data page
                    binaryStream.Seek((currentPage * DataPageSize) + CounterSection, SeekOrigin.Begin);
                    writer.Write(freeSpace);

                    // Last 8 bytes of each page store the next page number
                    binaryStream.Seek((currentPage + 1) * DataPageSize + CounterSection - BufferOverflowPointer, SeekOrigin.Begin);
                    writer.Write(((currentPage + 1) * DataPageSize) + CounterSection); // Next page number
                    freeSpace = DataPageSize;
                }

                currentPage++;
                pageNum++;
                numberOfPagesNeeded--;
            }

            //update the pointer in the last DP
            binaryStream.Seek((currentPage * DataPageSize) + CounterSection - BufferOverflowPointer, SeekOrigin.Begin);
            writer.Write(DefaultBufferForDp);

            _tablesCount++;
            DataPageCounter += pageNum;
            AllDataPagesCount += pageNum;

            binaryStream.Close();
            writer.Close();

            OffsetManager.WriteOffsetMapper(_tableOffsets.CustomLast());
        }

        public static bool DropTable(ReadOnlySpan<char> tableName)
        {
            //one talbe can span accross multiple data pages
            char[]? matchingKey = null;

            foreach (KeyValuePair<char[], long> keyValuePair in _tableOffsets)
            {
                if (tableName.SequenceEqual(keyValuePair.Key))
                {
                    matchingKey = keyValuePair.Key;
                    break;
                }
            }

            if (matchingKey is null)
                return false;

            long startingPageOffset = _tableOffsets[matchingKey];
            int numberOfDataPagesForTable = FindDataPageNumberForTable(startingPageOffset);

            FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryWriter writer = new(binaryStream);

            byte[] emptyPage = new byte[DataPageSize * numberOfDataPagesForTable];

            binaryStream.Seek(startingPageOffset, SeekOrigin.Begin);
            writer.Write(emptyPage);

            _tableOffsets.Remove(matchingKey);

            binaryStream.Close();
            writer.Close();

            //need to remove it from the page offset too
            OffsetManager.RemoveOffsetRecord(matchingKey);

            _tablesCount--;
            AllDataPagesCount -= numberOfDataPagesForTable;
            DataPageCounter -= numberOfDataPagesForTable;

            return true;
        }

        public static void ListTables()
        {
            if (_tableOffsets.Count == 0)
            {
                Console.WriteLine("There is no tables in the DB");
                return;
            }

            foreach (char[] table in _tableOffsets.Keys)
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
            foreach (KeyValuePair<char[], long> item in _tableOffsets)
            {
                if (item.Key.SequenceEqual(extractedTableName))
                {
                    tableFromOffset = item.Key;
                    break;
                }
            }

            if (tableFromOffset is null)
            {
                Console.WriteLine("No table found with this name");
                return;
            }

            long offset = _tableOffsets[tableFromOffset];
            int numberOfDataPagesForTable = FindDataPageNumberForTable(offset);

            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fileStream, Encoding.UTF8);

            fileStream.Seek(offset, SeekOrigin.Begin);

            int freeSpace = reader.ReadInt32();
            ulong recordSize = reader.ReadUInt64();
            int tableNameLengthFromFile = reader.ReadInt32();
            byte[] tableNameInBytes = reader.ReadBytes(tableNameLengthFromFile);
            string tableNameFromFile = Encoding.UTF8.GetString(tableNameInBytes);
            int columnsCount = reader.ReadInt32();

            Console.WriteLine($"Table name: {tableNameFromFile} \nOccupied space in bytes: {recordSize} \nThe table spans across {numberOfDataPagesForTable} data pages \nColumns count is {columnsCount}");
        }

        private static int FindDataPageNumberForTable(long startingPosition)
        {
            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fileStream, Encoding.UTF8);

            fileStream.Seek(startingPosition + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);

            int counter = 1;
            long pointer = reader.ReadInt64();

            while (pointer != DefaultBufferForDp)
            {
                fileStream.Seek(pointer + DataPageSize - BufferOverflowPointer, SeekOrigin.Begin);
                pointer = reader.ReadInt64();
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

                _tablesCount = reader.ReadInt32();
                AllDataPagesCount = reader.ReadInt32();
                DataPageCounter = reader.ReadInt32();
                FirstOffsetPageStart = reader.ReadInt32();
            }
            catch (Exception)
            {
                using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.CreateNew);
                using BinaryWriter writer = new(fs, Encoding.UTF8);
                fs.Seek(0, SeekOrigin.Begin);

                writer.Write(_tablesCount);
                writer.Write(AllDataPagesCount);
                writer.Write(DataPageCounter);
                writer.Write(FirstOffsetPageStart);
            }

            if (AllDataPagesCount != 0)
                _tableOffsets = OffsetManager.ReadTableOffsets();
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                case CtrlTypes.CTRL_CLOSE_EVENT:
                case CtrlTypes.CTRL_BREAK_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    ConsoleEventCallback();
                    Console.WriteLine("Closing the program ....");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ctrlType), ctrlType, null);
            }
            return true;
        }

        private static void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            ConsoleEventCallback();
            Console.WriteLine("System is logging off or shutting down...");
        }
    }
}
