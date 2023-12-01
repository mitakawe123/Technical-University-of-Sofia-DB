using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using DMS.Utils;
using System.Text;
using DMS.Commands;
using DMS.Extensions;
using System.IO;
using DMS.OffsetPages;

namespace DMS.Indexes
{
    public static class IndexManager
    {
        private const int DefaultBufferForIndexDp = -10;

        public static void CreateIndex(IReadOnlyList<string> columns, ReadOnlySpan<char> tableName, ReadOnlySpan<char> indexName)
        {
            char[] matchingKey = HelperMethods.FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is no table with the given name");
                return;
            }

            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            var metadata = SqlCommands.ReadTableMetadata(reader);
            int headerSectionForMainDp = DataPageManager.Metadata + metadata.tableLength;

            (headerSectionForMainDp, DKList<Column> columnTypeAndName) = SqlCommands.ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            bool allElementsContained = columns.CustomAll(x => columnTypeAndName.CustomAny(y => y.Name == x));
            if (!allElementsContained)
            {
                Console.WriteLine("Wrong column in the where clause");
                CloseFileAndReader(fileStream, reader);
                return;
            }

            DKList<long> offsets = new();

            foreach (string col in columns)
            {
                int columnIndex = HelperMethods.FindColumnIndex(col, columnTypeAndName);
                offsets.AddRange(GetOffsetForIndexColumns(fileStream, reader, columnIndex, matchingKey, headerSectionForMainDp, columnTypeAndName.Count));
            }

            CloseFileAndReader(fileStream, reader);
            byte[] offsetValues = OffsetManager.GetDataPageOffsetByTableName(tableName.CustomToArray());
            UpdateOffsetIndexManagerIndexColumns(offsetValues);
            WriteBinaryTreeToFile(offsets, columns.Count);
        }

        public static void DropIndex(ReadOnlySpan<char> tableName, ReadOnlySpan<char> indexName)
        {

        }

        private static DKList<long> GetOffsetForIndexColumns(
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
                reader.ReadInt32(); //<- free space
                start = pointer + sizeof(int);
                end = pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                lengthToRead = end - start;

                offsetForIndexColumn.AddRange(GetOffsetForSingleDataPage(reader, lengthToRead, columnIndex, columnsCount));
                fileStream.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            return offsetForIndexColumn;
        }

        private static DKList<long> GetOffsetForSingleDataPage(
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
            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter writer = new(fileStream);

            int metadataForIndex = sizeof(int) + sizeof(long);
            int treeArraySize = CalculateBinaryTreeArraySize(offsets.Count);
            int numberOfPagesNeeded = (int)Math.Ceiling((double)(sizeof(long) * treeArraySize) / (DataPageManager.DataPageSize - metadataForIndex));

            int currentPage = DataPageManager.AllDataPagesCount;
            int offsetIndex = 0;

            while (numberOfPagesNeeded > 0)
            {
                long pageStartOffset = currentPage * DataPageManager.DataPageSize + sizeof(int);//sizeof(int) is for free space
                fileStream.Seek(pageStartOffset, SeekOrigin.Begin);

                int freeSpace = DataPageManager.DataPageSize - metadataForIndex;
                while (offsetIndex < treeArraySize && freeSpace >= sizeof(long))
                {
                    long offsetToWrite = (offsetIndex < offsets.Count) ? offsets.ElementAt(offsetIndex) : 0; // Write 0 for empty nodes
                    writer.Write(offsetToWrite);
                    offsetIndex++;
                    freeSpace -= sizeof(long);
                }

                //catch the case when it will overflow to the next data page

                currentPage++;
                numberOfPagesNeeded--;

                // Update free space
                fileStream.Seek(pageStartOffset - sizeof(int), SeekOrigin.Begin);
                writer.Write(freeSpace);
            }

            DataPageManager.AllDataPagesCount += currentPage - DataPageManager.AllDataPagesCount;
        }

        private static void UpdateOffsetIndexManagerIndexColumns(byte[] offsetValues)
        {

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

        private static void ReadOffsetIndexManagerIndexColumns()
        {
            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();

            CloseFileAndReader(fileStream, reader);
        }

        private static int CalculateBinaryTreeArraySize(int nodeCount)
        {
            int height = (int)Math.Ceiling(Math.Log2(nodeCount + 1));
            return (int)Math.Pow(2, height) - 1;
        }

        private static (FileStream, BinaryReader) OpenFileAndReader()
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
