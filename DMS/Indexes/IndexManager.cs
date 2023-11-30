using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using DMS.Utils;
using System.Text;
using DMS.Commands;
using DMS.Extensions;

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

            DKList<long> result = new();

            foreach (string col in columns)
            {
                int columnIndex = HelperMethods.FindColumnIndex(col, columnTypeAndName);
                result.AddRange(GetOffsetForIndexColumns(fileStream, reader, columnIndex, matchingKey, headerSectionForMainDp, columnTypeAndName.Count));
            }

            fileStream.Seek(DataPageManager.AllDataPagesCount * DataPageManager.DataPageSize + DataPageManager.CounterSection, SeekOrigin.Begin);
            //write the tree till you go to the end of the data page
            //then write a pointer offset
            //continue with writing if there is more data and overflow is happening    
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
