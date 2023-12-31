using DataStructures;
using DMS.Constants;
using DMS.Extensions;
using DMS.OffsetPages;
using DMS.Shared;
using DMS.Utils;
using System.Text;
using DMS.DataRecovery;
using static DMS.Utils.ControlTypes;

namespace DMS.DataPages;

//in Microsoft SQL Server, there is a limit on the length of table names.
//The maximum length allowed for a table name is 128 characters.
//This limit is applicable not just to table names but also to most other identifiers in SQL Server, such as column names, schema names, constraint names, and others.
public static class DataPageManager
{
    public const long DefaultBufferForDp = -1;// Default pointer to the next page
    public const long BufferOverflowPointer = 8; //8 bytes for pointer to next page
    public const int CounterSection = 16; // 16 bytes for table count, data page count, all data pages count and offset table start
    public const int Metadata = 28;// 28 bytes for the metadata
    public const int DataPageSize = 8192; // 8KB

    public static int TablesCount; // 4 bytes for table count
    public static int DataPageCounter; // 4 bytes for data page count  
    public static int AllDataPagesCount; // 4 bytes for data page count
    public static int FirstOffsetPageStart; // 4 bytes for offset table 

    public static DKDictionary<char[], long> TableOffsets { get; } = new();

    static DataPageManager()
    {
        SetConsoleCtrlHandler(ConsoleCtrlCheck, true);

        try
        {
            ReadCountsFromFile();
        }
        catch (FileNotFoundException)
        {
            CreateNewFileWithDefaults();
        }
        catch (IOException ex)
        {
            Console.WriteLine($@"An IO exception occurred: {ex.Message}");
        }

        if (AllDataPagesCount != 0)
            TableOffsets = OffsetManager.ReadTableOffsets();
    }

    public static void InitDataPageManager() => Console.WriteLine(@"Welcome to DMS");

    public static void ConsoleEventCallback()
    {
        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryWriter writer = new(fs, Encoding.UTF8);

        fs.Seek(0, SeekOrigin.Begin);

        writer.Write(TablesCount);
        writer.Write(AllDataPagesCount);
        writer.Write(DataPageCounter);
        writer.Write(FirstOffsetPageStart);
    }

    public static void CreateTable(IReadOnlyList<Column> columns, ReadOnlySpan<char> tableName)
    {
        char[] table = tableName.CustomToArray();
        if (TableOffsets.ContainsKey(table))
        {
            Console.WriteLine(@"Table already exists");
            return;
        }

        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.ReadWrite);
        using BinaryWriter writer = new(fs, Encoding.UTF8);

        int columnDefinitionSpace = HelperAllocater.SpaceTakenByColumnsDefinitions(columns);
        ulong totalSpaceForColumnTypes = HelperAllocater.AllocatedStorageForTypes(columns);// this will calc max space required for one record
        if (totalSpaceForColumnTypes == 0)
        {
            Console.WriteLine(@"Invalid create table command");
            return;
        }

        int nonRowDataSpace = (int)Math.Ceiling((double)(columnDefinitionSpace + Metadata + tableName.Length));
        int totalNumberOfPages = (int)Math.Ceiling((double)(columnDefinitionSpace + Metadata + tableName.Length) / DataPageSize);
        int pointerSpaceRequired = totalNumberOfPages * sizeof(long);
        int numberOfPagesNeeded = (int)Math.Ceiling((double)(nonRowDataSpace + pointerSpaceRequired) / DataPageSize);

        int pageNum = 0;
        int columnIndex = 0;
        int freeSpace = DataPageSize;
        int currentPage = AllDataPagesCount;

        //here there is an edge case when I create table drop it, create new one and insert record the logic is broken
        //fix it
        if (DataPageCounter == 0)
            FirstOffsetPageStart = numberOfPagesNeeded * DataPageSize + CounterSection;

        while (numberOfPagesNeeded > 0)
        {
            fs.Seek(currentPage * DataPageSize + CounterSection, SeekOrigin.Begin);

            writer.Write(FileIntegrityChecker.DefaultHashValue); // 8 bytes hash

            //this is the first Data page for the table, and we need to write the header section only in this data page
            if (currentPage == AllDataPagesCount)
            {
                freeSpace -= Metadata + tableName.Length;// minus the header section

                //header section for the table data page is 20 bytes plus 1 byte per char for the table name
                writer.Write(freeSpace);// 4 bytes for free space
                writer.Write(totalSpaceForColumnTypes);// 8 bytes (max size in bytes for record)
                writer.Write(table.Length); //4 bytes for the table name length
                writer.Write(table);// 1 byte per char
                writer.Write(columns.Count);// 4 bytes for column count

                if (!TableOffsets.ContainsKey(table))
                    TableOffsets.Add(table, currentPage * DataPageSize + CounterSection);
            }

            // Write as many columns as fit on the current page
            while (freeSpace - BufferOverflowPointer > HelperAllocater.SpaceTakenByColumnsDefinition(columns[columnIndex]))
            {
                writer.Write(columns[columnIndex].Type);//2 bytes per char
                writer.Write(columns[columnIndex].Name);//2 bytes per char
                writer.Write(columns[columnIndex].DefaultValue);// 2 bytes per char

                freeSpace -= 2 * columns[columnIndex].Type.Length + 2 * columns[columnIndex].Name.Length + 2 * columns[columnIndex].DefaultValue.Length;
                columnIndex++;

                if (columnIndex == columns.Count)
                    break;
            }

            long snapshotHashStartingPosition = currentPage * DataPageSize + CounterSection;

            fs.Seek(currentPage * DataPageSize + CounterSection + FileIntegrityChecker.HashSize, SeekOrigin.Begin);
            writer.Write(freeSpace);

            currentPage++;
            pageNum++;
            numberOfPagesNeeded--;

            FileIntegrityChecker.RecalculateHash(fs, writer, snapshotHashStartingPosition);

            // If there are more columns to write, store a reference to the next page
            if (columnIndex >= columns.Count)
                continue;

            // Last 8 bytes of each page store the next page offset
            fs.Seek(currentPage * DataPageSize + CounterSection - BufferOverflowPointer, SeekOrigin.Begin);
            long pointerToNextPage = currentPage * DataPageSize + CounterSection;

            writer.Write(pointerToNextPage); // Next page offset
            freeSpace = DataPageSize;

            FileIntegrityChecker.RecalculateHash(fs, writer, snapshotHashStartingPosition);
        }

        //update the pointer in the last DP
        fs.Seek(currentPage * DataPageSize + CounterSection - BufferOverflowPointer, SeekOrigin.Begin);
        writer.Write(DefaultBufferForDp);

        TablesCount++;
        DataPageCounter += pageNum;
        AllDataPagesCount += pageNum;

        fs.Close();
        writer.Close();

        OffsetManager.WriteOffsetMapper(TableOffsets.CustomLast(), columns.Count);
    }

    public static bool DropTable(ReadOnlySpan<char> tableName)
    {
        char[] matchingKey = HelperMethods.FindTableWithName(tableName);

        if (matchingKey == Array.Empty<char>())
            return false;

        long startingPageOffset = TableOffsets[matchingKey];
        int numberOfDataPagesForTable = FindDataPageNumberForTable(startingPageOffset);

        FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        BinaryWriter writer = new(fs);

        for (int i = 0; i < numberOfDataPagesForTable; i++)
        {
            fs.Seek(startingPageOffset + DataPageSize * i, SeekOrigin.Begin);

            long snapshot = fs.Position;
            byte[] emptyPage = new byte[DataPageSize];

            writer.Write(emptyPage);
            FileIntegrityChecker.RecalculateHash(fs, writer, snapshot);
        }

        TableOffsets.Remove(matchingKey);

        fs.Close();
        writer.Close();

        //remove it from the page offset
        OffsetManager.RemoveOffsetRecord(matchingKey);

        TablesCount--;
        AllDataPagesCount -= numberOfDataPagesForTable;
        DataPageCounter -= numberOfDataPagesForTable;

        return true;
    }

    public static void ListTables()
    {
        if (TableOffsets.Count == 0)
        {
            Console.WriteLine(@"There are no tables in the DB.");
            return;
        }

        Console.WriteLine(@"List of Tables in the Database:");
        Console.WriteLine(new string('-', 30)); // Print a separator line

        int index = 1;
        foreach (char[] tableCharArray in TableOffsets.Keys)
        {
            string tableName = new(tableCharArray);
            Console.WriteLine($@"{index}. {tableName}");
            index++;
        }

        Console.WriteLine(new string('-', 30)); // Print a separator line
        Console.WriteLine($@"{TableOffsets.Count} tables listed.");
    }

    public static TableInfo TableInfo(ReadOnlySpan<char> tableName, bool isForUi = false)
    {
        char[] tableFromOffset = HelperMethods.FindTableWithName(tableName);

        if (tableFromOffset == Array.Empty<char>())
        {
            Console.WriteLine(@"No table found with this name");
            return default;
        }

        TableInfo tableInfoForUi = new()
        {
            ColumnTypes = new(),
            ColumnNames = new(),
            DefaultValues = new()
        };

        long offset = TableOffsets[tableFromOffset];
        int numberOfDataPagesForTable = FindDataPageNumberForTable(offset);

        tableInfoForUi.NumberOfDataPages = numberOfDataPagesForTable;

        using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryReader reader = new(fileStream, Encoding.UTF8);

        fileStream.Seek(offset, SeekOrigin.Begin);

        ulong hash = reader.ReadUInt64();
        int freeSpace = reader.ReadInt32();
        ulong recordSize = reader.ReadUInt64();
        int tableNameLengthFromFile = reader.ReadInt32();
        char[] tableNameFromFile = reader.ReadChars(tableNameLengthFromFile);
        int columnsCount = reader.ReadInt32();

        tableInfoForUi.ColumnCount = columnsCount;
        tableInfoForUi.TableName = new string(tableNameFromFile);

        Console.WriteLine($@"Table name: {new string(tableNameFromFile)}");
        Console.WriteLine($@"The table spans across {numberOfDataPagesForTable} data pages");
        Console.WriteLine($@"Columns count is {columnsCount}");
        Console.WriteLine(@"
Column Details:");

        Console.WriteLine($@"{"Column Name",-20} Column Type");
        Console.WriteLine(new string('-', 40));
        for (int i = 0; i < columnsCount; i++)
        {
            string columnType = reader.ReadString();
            string columnName = reader.ReadString();
            string defaultValue = reader.ReadString();

            tableInfoForUi.ColumnTypes.Add(columnType);
            tableInfoForUi.ColumnNames.Add(columnName);
            tableInfoForUi.DefaultValues.Add(defaultValue);

            Console.WriteLine($@"{columnName,-20} {columnType}");
        }
        Console.WriteLine(new string('-', 40));

        return isForUi ? tableInfoForUi : default;
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

    private static void ReadCountsFromFile()
    {
        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryReader reader = new(fs, Encoding.UTF8);

        TablesCount = reader.ReadInt32();
        AllDataPagesCount = reader.ReadInt32();
        DataPageCounter = reader.ReadInt32();
        FirstOffsetPageStart = reader.ReadInt32();
    }

    private static void CreateNewFileWithDefaults()
    {
        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.CreateNew);
        using BinaryWriter writer = new(fs, Encoding.UTF8);

        writer.Write(TablesCount);
        writer.Write(AllDataPagesCount);
        writer.Write(DataPageCounter);
        writer.Write(FirstOffsetPageStart);
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
                Console.WriteLine(@"Closing the program ....");
                ConsoleEventCallback();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ctrlType), ctrlType, null);
        }
        return true;
    }
}