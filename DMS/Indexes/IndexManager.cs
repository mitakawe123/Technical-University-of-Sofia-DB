using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using DMS.Utils;
using System.Text;
using DMS.Commands;
using DMS.DataRecovery;
using DMS.Extensions;
using DMS.OffsetPages;

namespace DMS.Indexes;

public static class IndexManager
{
    private const int DefaultBufferForIndexDp = -10;
    private const int DefaultOffsetIndexValue = 0;
    private const long DefaultOffsetIndexNameValue = 0;

    public static void CreateIndex(IReadOnlyList<string> columns, ReadOnlySpan<char> tableName, ReadOnlySpan<char> indexName)
    {
        char[] matchingKey = HelperMethods.FindTableWithName(tableName);

        if (matchingKey == Array.Empty<char>())
        {
            Console.WriteLine("There is no table with the given name");
            return;
        }

        (FileStream fs, BinaryReader reader) = OpenFileAndRead();

        fs.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

        var metadata = SqlCommands.ReadTableMetadata(reader);
        int headerSectionForMainDp = DataPageManager.Metadata + metadata.tableLength;

        (headerSectionForMainDp, DKList<Column> columnTypeAndName) = SqlCommands.ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

        bool allElementsContained = columns.CustomAll(x => columnTypeAndName.CustomAny(y => y.Name == x));
        if (!allElementsContained)
        {
            Console.WriteLine("Wrong column in the where clause");
            CloseFileAndReader(fs, reader);
            return;
        }

        DKList<long> offsets = new();
        DKList<int> columnIndexInTheTable = new();
        DKList<string> columnIndexNames = new();

        foreach (string col in columns)
        {
            int columnIndex = HelperMethods.FindColumnIndex(col, columnTypeAndName);
            offsets.AddRange(GetOffsetForIndexColumns(fs, reader, columnIndex, matchingKey, headerSectionForMainDp, columnTypeAndName.Count));
            columnIndexInTheTable.Add(columnIndex);
            columnIndexNames.Add(col);
        }

        CloseFileAndReader(fs, reader);

        var offsetValues = OffsetManager.GetDataPageOffsetByTableName(tableName.CustomToArray());
        WriteBinaryTreeToFile(offsets, out long startOfBinaryTreeDp);

        UpdateOffsetManagerIndexColumns(columnIndexInTheTable, offsets, indexName, offsetValues.offsetValues, offsetValues.endOfRecordOffsetValues, offsetValues.startOfRecordOffsetValue, startOfBinaryTreeDp);
    }

    public static void DropIndex(ReadOnlySpan<char> tableNameFromCommand, ReadOnlySpan<char> indexName)
    {
        char[] matchingKey = HelperMethods.FindTableWithName(tableNameFromCommand);

        if (matchingKey == Array.Empty<char>())
        {
            Console.WriteLine(@"There is no table with the given name");
            return;
        }

        var offsetValues = OffsetManager.GetDataPageOffsetByTableName(tableNameFromCommand.CustomToArray());

        int tableNameLength = BitConverter.ToInt32(offsetValues.offsetValues, 0);

        char[] tableName = new char[tableNameLength];
        Array.Copy(offsetValues.offsetValues, 4, tableName, 0, tableNameLength);

        long offsetValue = BitConverter.ToInt64(offsetValues.offsetValues, 4 + tableNameLength);

        int columnCount = BitConverter.ToInt32(offsetValues.offsetValues, 12 + tableNameLength);

        int[] columnIndexes = new int[columnCount];
        long[] columnIndexNamesAsNumbers = new long[columnCount];

        int byteIndex = 16 + tableNameLength;

        for (int i = 0; i < columnCount; i++)
        {
            columnIndexes[i] = BitConverter.ToInt32(offsetValues.offsetValues, byteIndex);
            byteIndex += 4;

            columnIndexNamesAsNumbers[i] = BitConverter.ToInt64(offsetValues.offsetValues, byteIndex);
            byteIndex += 8;
        }

        long startOfRecordOffsetValues = offsetValues.endOfRecordOffsetValues - sizeof(int) * columnCount - sizeof(long) * columnCount;

        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryWriter writer = new(fs);

        fs.Seek(startOfRecordOffsetValues, SeekOrigin.Begin);

        for (int i = 0; i < columnCount; i++)
        {
            string columnIndexName = WordConverter.ConvertNumberToWord(columnIndexNamesAsNumbers[i]);
            if (columnIndexName == new string(indexName))
            {
                writer.Write(DefaultOffsetIndexValue);
                writer.Write(DefaultOffsetIndexNameValue);

                Console.WriteLine("Successfully drop index");
            }

            fs.Seek(sizeof(int) + sizeof(long), SeekOrigin.Begin);
        }

        FileIntegrityChecker.RecalculateHash(fs, writer, offsetValues.startOfRecordOffsetValue);
    }

    public static IReadOnlyList<long> ReadIndexedColumns(ReadOnlySpan<char> tableName)
    {
        var offsetValues = OffsetManager.GetDataPageOffsetByTableName(tableName.CustomToArray());

        (int[] columnIndexes, long[] columnIndexNamesAsNumbers, int columnCount) = GetColumnIndexes(offsetValues.offsetValues);

        IReadOnlyList<int> offsetForBinaryTrees = ReadOffsetForBinaryTrees(columnIndexes, tableName, columnCount);

        (FileStream fs, BinaryReader reader) = OpenFileAndRead();

        DKList<long> indexForIndexedColumns = new();
        foreach (int offset in offsetForBinaryTrees)
        {
            long pointerOffset = offset - sizeof(ulong) - sizeof(int) + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            fs.Seek(offset, SeekOrigin.Begin);// int for free space and ulong for hash

            while (fs.Position <= pointerOffset)
            {
                long offsetForIndexedColumn = reader.ReadInt64();
                indexForIndexedColumns.Add(offsetForIndexedColumn);
            }

            fs.Seek(pointerOffset, SeekOrigin.Begin);
            long pointer = reader.ReadInt64();

           /* while (pointer != 0)
            {
                fs.Seek(pointer + sizeof(ulong) + sizeof(int), SeekOrigin.Begin);

                long offsetForIndexedColumn = reader.ReadInt64();
            }*/
        }

        return indexForIndexedColumns;
    }

    private static (int[] columnIndexes, long[] columnIndexNamesAsNumbers, int columnCount) GetColumnIndexes(byte[] offsetValues)
    {
        int tableNameLength = BitConverter.ToInt32(offsetValues, 0);

        char[] tableName = new char[tableNameLength];
        Array.Copy(offsetValues, 4, tableName, 0, tableNameLength);

        long offsetValue = BitConverter.ToInt64(offsetValues, 4 + tableNameLength);

        int columnCount = BitConverter.ToInt32(offsetValues, 12 + tableNameLength);

        int[] columnIndexes = new int[columnCount];
        long[] columnIndexNamesAsNumbers = new long[columnCount];

        int byteIndex = 16 + tableNameLength;

        for (int i = 0; i < columnCount; i++)
        {
            columnIndexes[i] = BitConverter.ToInt32(offsetValues, byteIndex);
            byteIndex += 4;

            columnIndexNamesAsNumbers[i] = BitConverter.ToInt64(offsetValues, byteIndex);
            byteIndex += 8;
        }

        return (columnIndexes, columnIndexNamesAsNumbers, columnCount);
    }

    private static IReadOnlyList<long> GetOffsetForIndexColumns(
        FileStream fileStream,
        BinaryReader reader,
        int columnIndex,
        char[] matchingKey,
        int headerSectionForMainDp,
        int columnsCount)
    {
        DKList<long> offsetForIndexColumn = new();

        long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDp;
        long end = DataPageManager.TableOffsets[matchingKey] + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
        long lengthToRead = end - start;

        fileStream.Seek(start, SeekOrigin.Begin);

        offsetForIndexColumn.AddRange(GetOffsetForSingleDataPage(reader, lengthToRead, columnIndex, columnsCount));

        fileStream.Seek(end, SeekOrigin.Begin);
        long pointer = reader.ReadInt64();

        while (pointer != DataPageManager.DefaultBufferForDp)
        {
            fileStream.Seek(pointer, SeekOrigin.Begin);
            ulong hash = reader.ReadUInt64();// <- hash
            int freeSpace = reader.ReadInt32(); //<- free space

            start = pointer + sizeof(int) + sizeof(ulong);
            end = pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            lengthToRead = end - start;

            offsetForIndexColumn.AddRange(GetOffsetForSingleDataPage(reader, lengthToRead, columnIndex, columnsCount));
            fileStream.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
            pointer = reader.ReadInt64();
        }

        return offsetForIndexColumn;
    }

    private static IReadOnlyList<long> GetOffsetForSingleDataPage(
        BinaryReader reader,
        long lengthToRead,
        int columnIndex,
        int columnCount)
    {
        DKList<long> offsets = new();
        int offset = 0;
        int columnIndexCounter = 0;
        while (offset < lengthToRead)
        {
            int length = reader.ReadInt32();
            offset += sizeof(int);

            if (offset >= lengthToRead)
                return offsets;

            char[] charArray = reader.ReadChars(length);
            offset += length;

            if (columnIndexCounter == columnIndex && length != 0)
                offsets.Add(reader.BaseStream.Position);

            if (columnIndexCounter + 1 == columnCount)
                columnIndexCounter = 0;
            else
                columnIndexCounter++;
        }

        return offsets;
    }

    private static void WriteBinaryTreeToFile(IReadOnlyCollection<long> offsets, out long startOfBinaryTreeDp)
    {
        startOfBinaryTreeDp = 0;

        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryWriter writer = new(fs);

        int metadataForIndex = sizeof(int) + sizeof(long);
        int treeArraySize = CalculateBinaryTreeArraySize(offsets.Count);
        int numberOfPagesNeeded = (int)Math.Ceiling((double)(sizeof(long) * treeArraySize) / (DataPageManager.DataPageSize - metadataForIndex));

        int currentPage = DataPageManager.AllDataPagesCount;
        int offsetIndex = 0;

        bool isStartBinaryTreeSet = false;

        while (numberOfPagesNeeded > 0)
        {
            long pageStartOffset = currentPage * DataPageManager.DataPageSize + DataPageManager.CounterSection +
                                   sizeof(int) + sizeof(ulong); // sizeof(int) is for free space and ulong for hash
            fs.Seek(pageStartOffset, SeekOrigin.Begin);

            if (!isStartBinaryTreeSet)
            {
                startOfBinaryTreeDp = pageStartOffset;
                isStartBinaryTreeSet = true;
            }

            int freeSpace = DataPageManager.DataPageSize - metadataForIndex;
            while (offsetIndex < treeArraySize && freeSpace >= sizeof(long))
            {
                long offsetToWrite = offsetIndex < offsets.Count ? offsets.ElementAt(offsetIndex) : 0; // Write 0 for empty nodes
                writer.Write(offsetToWrite);
                offsetIndex++;
                freeSpace -= sizeof(long);
            }

            //catch the case when it will overflow to the next data page
            if (offsetIndex < treeArraySize)
            {
                long startOfNewDp = (currentPage + 1) * DataPageManager.DataPageSize;
                fs.Seek(startOfNewDp - sizeof(long), SeekOrigin.Begin);
                writer.Write(startOfNewDp);
            }

            // Update free space in the current page
            fs.Seek(pageStartOffset - sizeof(int), SeekOrigin.Begin);
            writer.Write(freeSpace);

            currentPage++;
            numberOfPagesNeeded--;

            FileIntegrityChecker.RecalculateHash(fs, writer, pageStartOffset - sizeof(int) - sizeof(ulong));
        }

        DataPageManager.AllDataPagesCount += currentPage - DataPageManager.AllDataPagesCount;
    }

    private static void UpdateOffsetManagerIndexColumns(
        IReadOnlyList<int> columnIndexInTheTable,
        IReadOnlyList<long> indexOffsets,
        ReadOnlySpan<char> indexName,
        byte[] offsetValues,
        long endOfRecordOffsetValues,
        long startOfOffsetRecord,
        long startOfBinaryTreeDp)
    {
        int tableNameLength = BitConverter.ToInt32(offsetValues, 0);

        char[] tableName = new char[tableNameLength];
        Array.Copy(offsetValues, 4, tableName, 0, tableNameLength);

        long offsetValue = BitConverter.ToInt64(offsetValues, 4 + tableNameLength);

        int columnCount = BitConverter.ToInt32(offsetValues, 12 + tableNameLength);

        int[] columnIndexes = new int[columnCount];
        long[] columnIndexNamesAsNumbers = new long[columnCount];

        int byteIndex = 16 + tableNameLength;

        for (int i = 0; i < columnCount; i++)
        {
            columnIndexes[i] = BitConverter.ToInt32(offsetValues, byteIndex);
            byteIndex += 4;

            columnIndexNamesAsNumbers[i] = BitConverter.ToInt64(offsetValues, byteIndex);
            byteIndex += 8;
        }

        long startOfRecordOffsetValues = endOfRecordOffsetValues - sizeof(int) * columnCount - sizeof(long) * columnCount;

        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
        using BinaryWriter writer = new(fs);

        fs.Seek(startOfRecordOffsetValues, SeekOrigin.Begin);

        for (int i = 0; i < columnCount; i++)
        {
            if (columnIndexInTheTable.CustomContains(i))
            {
                int startOfBt = (int)startOfBinaryTreeDp;
                writer.Write(startOfBt);// write in the offset manager the start offset of the binary tree

                long columnNameAsNumber = WordConverter.ConvertWordToNumber(indexName.ToString());
                writer.Write(columnNameAsNumber);

                Console.WriteLine("Successfully created index");
            }

            fs.Seek(sizeof(int) + sizeof(long), SeekOrigin.Current);
        }

        FileIntegrityChecker.RecalculateHash(fs, writer, startOfOffsetRecord);
    }

    private static IReadOnlyList<int> ReadOffsetForBinaryTrees(
        IReadOnlyList<int> columnIndexForIndexer,
        ReadOnlySpan<char> tableName,
        int columnCount)
    {
        DKList<int> offsetForBinaryTrees = new();

        var offsetValues = OffsetManager.GetDataPageOffsetByTableName(tableName.CustomToArray());
        long startOfRecordOffsetValues = offsetValues.endOfRecordOffsetValues - sizeof(int) * columnCount - sizeof(long) * columnCount;

        using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(fs);

        fs.Seek(startOfRecordOffsetValues, SeekOrigin.Begin);

        for (int i = 0; i < columnCount; i++)
        {
            if (columnIndexForIndexer.CustomContains(i))
            {
                int startOfBinaryTree = reader.ReadInt32();
                long indexedColumnNameAsNumber = reader.ReadInt64();

                offsetForBinaryTrees.Add(startOfBinaryTree);
            }

            fs.Seek(sizeof(int) + sizeof(long), SeekOrigin.Current);
        }

        return offsetForBinaryTrees;
    }

    private static int CalculateBinaryTreeArraySize(int nodeCount)
    {
        int height = (int)Math.Ceiling(Math.Log2(nodeCount + 1));
        return (int)Math.Pow(2, height) - 1;
    }

    private static (FileStream, BinaryReader) OpenFileAndRead()
    {
        FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
        BinaryReader reader = new(fileStream, Encoding.UTF8);
        return (fileStream, reader);
    }

    private static void CloseFileAndReader(FileStream stream, BinaryReader reader)
    {
        stream.Close();
        reader.Close();
    }
}