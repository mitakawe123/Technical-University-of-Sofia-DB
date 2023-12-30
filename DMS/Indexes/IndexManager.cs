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

namespace DMS.Indexes
{
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
                Console.WriteLine(@"There is no table with the given name");
                return;
            }

            (FileStream fileStream, BinaryReader reader) = OpenFileAndRead();

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            var metadata = SqlCommands.ReadTableMetadata(reader);
            int headerSectionForMainDp = DataPageManager.Metadata + metadata.tableLength;

            (headerSectionForMainDp, DKList<Column> columnTypeAndName) = SqlCommands.ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            bool allElementsContained = columns.CustomAll(x => columnTypeAndName.CustomAny(y => y.Name == x));
            if (!allElementsContained)
            {
                Console.WriteLine(@"Wrong column in the where clause");
                CloseFileAndReader(fileStream, reader);
                return;
            }

            DKList<long> offsets = new();
            DKList<int> columnIndexInTheTable = new();
            DKList<string> columnIndexNames = new();

            foreach (string col in columns)
            {
                int columnIndex = HelperMethods.FindColumnIndex(col, columnTypeAndName);
                offsets.AddRange(GetOffsetForIndexColumns(fileStream, reader, columnIndex, matchingKey, headerSectionForMainDp, columnTypeAndName.Count));
                columnIndexInTheTable.Add(columnIndex);
                columnIndexNames.Add(col);
            }

            CloseFileAndReader(fileStream, reader);

            var offsetValues = OffsetManager.GetDataPageOffsetByTableName(tableName.CustomToArray());
            UpdateOffsetManagerIndexColumns(columnIndexInTheTable, columnIndexNames, offsets, indexName, offsetValues.offsetValues, offsetValues.endOfRecordOffsetValues, offsetValues.startOfRecordOffsetValue);

            WriteBinaryTreeToFile(offsets, columns.Count);
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
                 
                    Console.WriteLine(@"Successfully drop index");
                }

                fs.Seek(sizeof(int) + sizeof(long), SeekOrigin.Begin);
            }

            FileIntegrityChecker.RecalculateHash(fs, writer, offsetValues.startOfRecordOffsetValue);
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

        private static void WriteBinaryTreeToFile(DKList<long> offsets, int indexColumnCount)
        {
            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter writer = new(fs);

            int metadataForIndex = sizeof(int) + sizeof(long);
            int treeArraySize = CalculateBinaryTreeArraySize(offsets.Count);
            int numberOfPagesNeeded = (int)Math.Ceiling((double)(sizeof(long) * treeArraySize) / (DataPageManager.DataPageSize - metadataForIndex));

            int currentPage = DataPageManager.AllDataPagesCount;
            int offsetIndex = 0;

            while (numberOfPagesNeeded > 0)
            {
                long pageStartOffset = currentPage * DataPageManager.DataPageSize + DataPageManager.CounterSection + sizeof(int) + sizeof(ulong);// sizeof(int) is for free space and ulong for hash
                fs.Seek(pageStartOffset, SeekOrigin.Begin);

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
            IReadOnlyList<string> columnIndexNames,
            IReadOnlyList<long> indexOffsets,
            ReadOnlySpan<char> indexName,
            byte[] offsetValues,
            long endOfRecordOffsetValues,
            long startOfOffsetRecord)
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
                    int indexOffset = (int)indexOffsets[i];
                    writer.Write(indexOffset);

                    long columnNameAsNumber = WordConverter.ConvertWordToNumber(indexName.ToString());
                    writer.Write(columnNameAsNumber);   

                    Console.WriteLine(@"Successfully created index");
                }

                fs.Seek(sizeof(int) + sizeof(long), SeekOrigin.Current);
            }

            FileIntegrityChecker.RecalculateHash(fs, writer, startOfOffsetRecord);
        }

        private static DKList<long> ReadBinaryTreeFromFile(int indexColumnCount)
        {
            DKList<long> offsets = new DKList<long>();
            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fileStream);

            int metadataForIndex = sizeof(int) + sizeof(long);
            long fileSize = fileStream.Length;
            int numberOfPages = (int)Math.Ceiling((double)fileSize / DataPageManager.DataPageSize);

            int currentPage = 0;

            while (currentPage < numberOfPages)
            {
                long pageStartOffset = currentPage * DataPageManager.DataPageSize;//here i need to find from the offset manager the offset of the indexed column
                fileStream.Seek(pageStartOffset, SeekOrigin.Begin);

                int freeSpace = reader.ReadInt32();
                int maxOffsetsInPage = (DataPageManager.DataPageSize - metadataForIndex) / sizeof(long);
                int offsetsToRead = Math.Min(maxOffsetsInPage, (int)((fileSize - fileStream.Position) / sizeof(long)));

                for (int i = 0; i < offsetsToRead; i++)
                {
                    long offset = reader.ReadInt64();
                    if (offset != 0) // 0 is used for empty nodes
                        offsets.Add(offset);
                }

                currentPage++;
            }

            return offsets;
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
}
