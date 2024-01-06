using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.DataRecovery;
using System.Text;

namespace DMS.OffsetPages;

public static class OffsetManager
{
    private const long DefaultBufferValue = -5;
    private const int DefaultIndexValue = 0;
    private const long DefaultWordIndexValue = 0;

    public static void WriteOffsetMapper(KeyValuePair<char[], long> entry, int columnCount)
    {
        int freeSpace = DataPageManager.DataPageSize;
        int sizeOfCurrentRecord = RecordSizeForOffset(entry.Key.Length, columnCount);//here I have error when calculating the record size
        long pointerToNextPage = PointerToNextPage();//this is the end byte of the pointer
        long startOfFreeOffsetPage = pointerToNextPage - DataPageManager.DataPageSize;

        //this is the case when the there is no offset and I need to initialize it
        if (pointerToNextPage == -2)
        {
            InitFirstOffsetTable(entry, sizeOfCurrentRecord, columnCount, ref freeSpace);
            return;
        }

        freeSpace = FreeSpaceInOffset(sizeOfCurrentRecord);

        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryWriter writer = new(fs, Encoding.UTF8);

        //the data will overflow the current offset page and I need to create a new one
        if (freeSpace == DefaultBufferValue)
        {
            freeSpace = DataPageManager.DataPageSize - sizeOfCurrentRecord;
            CreateNewOffsetTable(entry, fs, writer, startOfFreeOffsetPage, columnCount, ref freeSpace);
            return;
        }

        WriteToCurrentOffsetTable(entry, fs, writer, startOfFreeOffsetPage, columnCount, ref freeSpace);
    }

    public static DKDictionary<char[], long> ReadTableOffsets()
    {
        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(fs, Encoding.UTF8);

        fs.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);

        DKDictionary<char[], long> offsetMap = new();
        ReadOffsetTable(fs, reader, offsetMap);

        if (DataPageManager.TablesCount == offsetMap.Count)
            return offsetMap;

        long nextPagePointer = reader.ReadInt64();
        while (nextPagePointer != DefaultBufferValue)
        {
            fs.Seek(nextPagePointer, SeekOrigin.Begin);
            ReadOffsetTable(fs, reader, offsetMap);
            nextPagePointer = reader.ReadInt64();
        }

        return offsetMap;
    }

    public static void RemoveOffsetRecord(char[] tableName)
    {
        bool isTableSuccessfulDeleted = false;
        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryReader reader = new(fs, Encoding.UTF8);
        using BinaryWriter writer = new(fs, Encoding.UTF8);

        fs.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);

        long stopPosition = DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
        long snapshotHashStartingPosition = fs.Position;

        ulong hash = reader.ReadUInt64();
        int freeSpace = reader.ReadInt32();

        while (fs.Position < stopPosition)
        {
            if (isTableSuccessfulDeleted)
            {
                FileIntegrityChecker.RecalculateHash(fs, writer, snapshotHashStartingPosition);
                return;
            }

            EraseRecordIfMatch();
        }

        fs.Seek(stopPosition, SeekOrigin.Begin);
        long pointer = reader.ReadInt64();

        while (pointer != DefaultBufferValue && pointer > 0)
        {
            fs.Seek(pointer, SeekOrigin.Begin);
            snapshotHashStartingPosition = fs.Position;

            hash = reader.ReadUInt64();
            freeSpace = reader.ReadInt32();

            stopPosition = pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            while (fs.Position < stopPosition)
            {
                if (isTableSuccessfulDeleted)
                {
                    FileIntegrityChecker.RecalculateHash(fs, writer, snapshotHashStartingPosition);
                    return;
                }

                EraseRecordIfMatch();
            }

            pointer = reader.ReadInt64();
        }

        return;

        void EraseRecordIfMatch()
        {
            int tableNameLength = reader.ReadInt32(); // 4 bytes
            char[] currentTableName = reader.ReadChars(tableNameLength); // 1 byte per char

            long offsetValue = reader.ReadInt64(); // 8 bytes
            int columnCount = reader.ReadInt32(); // 4 bytes

            int totalIndexValueSize = 0;
            int totalIndexValueAsNumberSize = 0;

            for (int i = 0; i < columnCount; i++)
            {
                int indexValue = reader.ReadInt32(); // 4 bytes each
                totalIndexValueSize += sizeof(int);

                long indexValueAsNumber = reader.ReadInt64(); // 8 bytes each
                totalIndexValueAsNumberSize += sizeof(long);
            }

            if (tableNameLength <= 0 || !tableName.SequenceEqual(currentTableName))
                return;

            int recordSizeInBytesBeforeColumnSize = tableNameLength // Size of currentTableName (1 byte per char)
                                                    + sizeof(long); // Size of offsetValue


            int recordSizeInBytesAfterColumnSize = totalIndexValueSize // Total size of all indexValue entries
                                                   + totalIndexValueAsNumberSize; // Total size of all indexValueAsNumber entries

            //don't erase the column count and the table name length
            byte[] emptyBufferBefore = new byte[recordSizeInBytesBeforeColumnSize];
            byte[] emptyBufferAfter = new byte[recordSizeInBytesAfterColumnSize];

            fs.Seek(-(recordSizeInBytesAfterColumnSize + recordSizeInBytesBeforeColumnSize + sizeof(int)), SeekOrigin.Current);//don't include the table name length int
            writer.Write(emptyBufferBefore);

            fs.Seek(sizeof(int), SeekOrigin.Current);//for the column count

            writer.Write(emptyBufferAfter);

            isTableSuccessfulDeleted = true;
        }
    }

    public static (byte[] offsetValues, long endOfRecordOffsetValues, long startOfRecordOffsetValue) GetDataPageOffsetByTableName(char[] tableName)
    {
        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryReader reader = new(fs, Encoding.UTF8);

        fs.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);
        long startOfRecordOffsetValue = fs.Position;

        ulong hash = reader.ReadUInt64();
        int freeSpace = reader.ReadInt32();
        long stopPosition = DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;

        try
        {
            while (fs.Position < stopPosition)
            {
                byte[]? result = CheckAndGetResult();
                if (result is not null)
                    return (result, fs.Position, startOfRecordOffsetValue);
            }

            long pointer = reader.ReadInt64();
            while (pointer is not DefaultBufferValue)
            {
                fs.Seek(pointer, SeekOrigin.Begin);
                startOfRecordOffsetValue = pointer;

                hash = reader.ReadUInt64();
                freeSpace = reader.ReadInt32();
                long stop = fs.Position + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                while (fs.Position < stop)
                {
                    byte[]? result = CheckAndGetResult();
                    if (result is not null)
                        return (result, fs.Position, startOfRecordOffsetValue);
                }

                pointer = reader.ReadInt64();
            }
        }
        catch (Exception)
        {
            Console.WriteLine($"There was unknown error while creating index for {new string(tableName)}");
        }

        return (Array.Empty<byte>(), 0, 0);

        byte[] CreateResultArray(
            int tableNameLength,
            char[] currentTableName,
            long offsetValue,
            int columnCount,
            IReadOnlyList<int> columnOffsetIndex,
            IReadOnlyList<long> columnIndexName)
        {
            int recordSizeInBytes = RecordSizeForOffset(tableNameLength, columnCount);
            byte[] result = new byte[recordSizeInBytes];

            using MemoryStream memoryStream = new(result);
            using BinaryWriter writer = new(memoryStream, Encoding.UTF8);

            writer.Write(tableNameLength);
            writer.Write(currentTableName);
            writer.Write(offsetValue);
            writer.Write(columnCount);

            for (int i = 0; i < columnOffsetIndex.Count; i++)
            {
                writer.Write(columnOffsetIndex.ElementAt(i));
                writer.Write(columnIndexName.ElementAt(i));
            }

            return result;
        }

        byte[]? CheckAndGetResult()
        {
            int tableNameLength = reader.ReadInt32();
            char[] currentTableName = reader.ReadChars(tableNameLength);

            if (!currentTableName.SequenceEqual(tableName) ||
                tableNameLength != tableName.Length) //early check to see if the table match
                return null;

            long offsetValue = reader.ReadInt64();
            int columnCount = reader.ReadInt32();

            DKList<int> columnOffsetIndex = new();
            DKList<long> columnIndexName = new();
            for (int i = 0; i < columnCount; i++)
            {
                columnOffsetIndex.Add(reader.ReadInt32());
                columnIndexName.Add(reader.ReadInt64());
            }

            if (currentTableName.SequenceEqual(tableName) && tableNameLength == tableName.Length)
                return CreateResultArray(tableNameLength, currentTableName, offsetValue, columnCount, columnOffsetIndex, columnIndexName);

            return null;
        }
    }

    //8 bytes hash 
    //4 bytes free space
    //1 byte per char table name
    //4 bytes start of offset
    //4 bytes for column count
    //4 bytes index offset * column Count
    //8 bytes for index name as number * column count
    private static int RecordSizeForOffset(int tableNameLength, int columnCount) => sizeof(ulong) + sizeof(int) + sizeof(int) + tableNameLength + sizeof(long) + sizeof(int) + columnCount * sizeof(int) + columnCount * sizeof(long);

    private static void InitFirstOffsetTable(
        KeyValuePair<char[], long> entry,
        int sizeOfCurrentRecord,
        int columnCount,
        ref int freeSpace)
    {
        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryWriter writer = new(fs, Encoding.UTF8);

        fs.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);

        long snapshotHashStartingPosition = fs.Position;

        freeSpace -= sizeOfCurrentRecord;
        writer.Write(FileIntegrityChecker.DefaultHashValue);// 8 bytes for hash
        writer.Write(freeSpace); // 4 bytes for free space

        writer.Write(entry.Key.Length); // 4 bytes for the length of the table name
        writer.Write(entry.Key); // 1 byte per char
        writer.Write(entry.Value); // 8 bytes for the start offset of the record
        writer.Write(columnCount); // 4 bytes for number of columns 

        for (int i = 0; i < columnCount; i++)
        {
            writer.Write(DefaultIndexValue); // 4 bytes index offset
            writer.Write(DefaultWordIndexValue); //8 bytes index name as number 
        }

        fs.Seek(DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
        writer.Write(DefaultBufferValue);

        FileIntegrityChecker.RecalculateHash(fs, writer, snapshotHashStartingPosition);

        DataPageManager.AllDataPagesCount++;
    }

    private static void CreateNewOffsetTable(
        KeyValuePair<char[], long> entry,
        FileStream fs,
        BinaryWriter writer,
        long startOfFreeOffset,
        int columnCount,
        ref int freeSpace)
    {
        fs.Seek(startOfFreeOffset + DataPageManager.DataPageSize, SeekOrigin.Begin);

        long snapshotHashStartingPosition = fs.Position;

        writer.Write(FileIntegrityChecker.DefaultHashValue);
        writer.Write(freeSpace);

        writer.Write(entry.Key.Length); // 4 bytes for the length of the table name
        writer.Write(entry.Key); // 1 byte per char
        writer.Write(entry.Value); // 8 bytes for the start offset of the record
        writer.Write(columnCount); // 4 bytes for number of columns 

        for (int i = 0; i < columnCount; i++)
        {
            writer.Write(DefaultIndexValue);
            writer.Write(DefaultWordIndexValue);
        }

        fs.Seek(startOfFreeOffset + DataPageManager.DataPageSize * 2 - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
        writer.Write(DefaultBufferValue);

        FileIntegrityChecker.RecalculateHash(fs, writer, snapshotHashStartingPosition);

        DataPageManager.AllDataPagesCount++;
    }

    private static void WriteToCurrentOffsetTable(
        KeyValuePair<char[], long> entry,
        FileStream fs,
        BinaryWriter writer,
        long startOfFreeOffset,
        int columnCount,
        ref int freeSpace)
    {
        long snapshotHashStartingPosition = startOfFreeOffset;

        long startingPoint = startOfFreeOffset + (DataPageManager.DataPageSize - freeSpace);
        int recordSize = sizeof(int) + entry.Key.Length + sizeof(long) + sizeof(int) + sizeof(int) * columnCount + sizeof(long) * columnCount;
        freeSpace -= recordSize;

        //update free space in the data page 
        fs.Seek(startOfFreeOffset + sizeof(ulong), SeekOrigin.Begin);
        writer.Write(freeSpace);

        fs.Seek(startingPoint, SeekOrigin.Begin);

        writer.Write(entry.Key.Length); // 4 bytes for the length of the table name
        writer.Write(entry.Key); // 1 byte per char
        writer.Write(entry.Value); // 8 bytes for the start offset of the record
        writer.Write(columnCount); // 4 bytes for number of columns 

        for (int i = 0; i < columnCount; i++)
        {
            writer.Write(DefaultIndexValue);//4 bytes
            writer.Write(DefaultWordIndexValue);// 8 bytes
        }

        fs.Seek(startOfFreeOffset + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
        writer.Write(DefaultBufferValue);

        FileIntegrityChecker.RecalculateHash(fs, writer, snapshotHashStartingPosition);
    }

    private static long PointerToNextPage()
    {
        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryReader reader = new(fs);

        fs.Seek(DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
        try
        {
            long pointer = reader.ReadInt64();
            while (pointer != DefaultBufferValue)
            {
                fs.Seek(pointer, SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            return fs.Position;
        }
        catch
        {
            return -2; //the offset page is not initialized
        }
    }

    private static int FreeSpaceInOffset(int requiredSpace)
    {
        using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryReader reader = new(binaryStream);

        binaryStream.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);
        ulong hash = reader.ReadUInt64();
        int freeSpace = reader.ReadInt32();
        if (requiredSpace + DataPageManager.BufferOverflowPointer < freeSpace)
            return freeSpace;

        return (int)DefaultBufferValue;
    }

    private static void ReadOffsetTable(FileStream fs, BinaryReader reader, IDictionary<char[], long> offsetMap)
    {
        long stopPosition = fs.Position + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;

        ulong hash = reader.ReadUInt64();
        int freeSpace = reader.ReadInt32();

        while (fs.Position < stopPosition)
        {
            int tableNameLength = reader.ReadInt32();
            char[] tableName = reader.ReadChars(tableNameLength);
            long offsetValue = reader.ReadInt64();
            int columnCount = reader.ReadInt32(); // number of columns to read for indexing 

            for (int i = 0; i < columnCount; i++)
            {
                int indexValue = reader.ReadInt32();
                long indexNameAsNumber = reader.ReadInt64();
            }

            if (tableNameLength is not 0 && offsetValue is not 0)
                offsetMap.TryAdd(tableName, offsetValue);

            if (DataPageManager.TablesCount == offsetMap.Count)
                return;
        }

        fs.Seek(stopPosition, SeekOrigin.Begin);
    }
}