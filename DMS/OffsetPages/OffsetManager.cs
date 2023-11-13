using DMS.Constants;
using DMS.DataPages;
using System.Linq;
using System.Text;

namespace DMS.OffsetPages
{
    public static class OffsetManager
    {
        private const int DefaultBuffer = -5;

        //createtable test(id int primary key, name string(max) null, name1 string(max) null)
        public static void WriteOffsetMapper(KeyValuePair<char[], long> entry)
        {
            int freeSpace = DataPageManager.DataPageSize;
            int sizeOfCurrentRecord = sizeof(int) + entry.Key.Length + sizeof(int);
            long pointerToNextPage = PointerToNextPage();//this is the end byte of the pointer
            long startOfFreeOffset = pointerToNextPage - DataPageManager.DataPageSize;

            //this is the case when the there is no offset and I need to initialize it
            if (pointerToNextPage == -2)
            {
                using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
                using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

                binaryStream.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);

                freeSpace -= sizeOfCurrentRecord;
                writer.Write(freeSpace); // 4 bytes for free space

                writer.Write(entry.Key.Length); // 4 bytes for the length of the table name
                writer.Write(entry.Key); // 1 byte per char
                writer.Write(entry.Value);// 4 bytes for the start offset of the record

                binaryStream.Seek(DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                writer.Write(DefaultBuffer);

                DataPageManager.AllDataPagesCount++;

                return;
            }

            freeSpace = FreeSpaceInOffset(sizeOfCurrentRecord);

            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter binaryWriter = new(fs, Encoding.UTF8);

            if (freeSpace == DefaultBuffer)
            {
                //create new offset page
                fs.Seek(startOfFreeOffset + DataPageManager.DataPageSize, SeekOrigin.Begin);

                freeSpace = DataPageManager.DataPageSize;
                binaryWriter.Write(freeSpace);

                binaryWriter.Write(entry.Key.Length); // 4 bytes for the length of the table name
                binaryWriter.Write(entry.Key); // 1 byte per char
                binaryWriter.Write(entry.Value);// 4 bytes for the start offset of the record

                fs.Seek(startOfFreeOffset + (DataPageManager.DataPageSize * 2) - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                binaryWriter.Write(DefaultBuffer);

                DataPageManager.AllDataPagesCount++;

                return;
            }

            fs.Seek(startOfFreeOffset, SeekOrigin.Begin);

            //write to the current offset page
            long startingPoint = startOfFreeOffset + (DataPageManager.DataPageSize - freeSpace) + sizeof(int);
            freeSpace -= sizeOfCurrentRecord;

            binaryWriter.Write(freeSpace); // 4 bytes for free space
            fs.Seek(startingPoint, SeekOrigin.Begin);

            binaryWriter.Write(entry.Key.Length); // 4 bytes for the length of the table name
            binaryWriter.Write(entry.Key); // 1 byte per char
            binaryWriter.Write(entry.Value);// 4 bytes for the start offset of the record

            fs.Seek(startOfFreeOffset + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
            binaryWriter.Write(DefaultBuffer);
        }

        public static Dictionary<char[], long> ReadTableOffsets()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);
            int freeSpace = reader.ReadInt32();

            Dictionary<char[], long> offsetMap = new();
            ReadOffsetTable(binaryStream, reader, offsetMap);

            int nextPagePointer = reader.ReadInt32();
            while (nextPagePointer != DefaultBuffer)
            {
                binaryStream.Seek(nextPagePointer, SeekOrigin.Begin);
                ReadOffsetTable(binaryStream, reader, offsetMap);
                nextPagePointer = reader.ReadInt32();
            }

            return offsetMap;
        }

        public static void RemoveOffsetRecord(char[] tableName)
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);
            int freeSpace = reader.ReadInt32();

            long stopPosition = DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            while (binaryStream.Position < stopPosition)
            {
                int tableNameLength = reader.ReadInt32();
                char[] currentTableName = reader.ReadChars(tableNameLength);
                int offsetValue = reader.ReadInt32();

                if (currentTableName.SequenceEqual(tableName) && tableNameLength == tableName.Length)
                {
                    int recordSizeInBytes = tableNameLength + sizeof(int) + sizeof(int);
                    byte[] emptyBuffer = new byte[recordSizeInBytes];
                    binaryStream.Seek(binaryStream.Position - recordSizeInBytes, SeekOrigin.Begin);
                    binaryStream.Write(emptyBuffer);
                    return;
                }
            }
        }

        private static long PointerToNextPage()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream);

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
            try
            {
                int pointer = reader.ReadInt32();
                while (pointer != DefaultBuffer)
                {
                    binaryStream.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
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

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);
            int freeSpace = reader.ReadInt32();
            if (requiredSpace + DataPageManager.BufferOverflowPointer < freeSpace)
                return freeSpace;

            return DefaultBuffer;
        }

        private static void ReadOffsetTable(FileStream stream, BinaryReader reader, Dictionary<char[], long> offsetMap)
        {
            long stopPosition = stream.Position + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer; //<- this sizeof(int) is free space variable

            while (stream.Position < stopPosition)
            {
                int tableNameLength = reader.ReadInt32();
                char[] tableName = reader.ReadChars(tableNameLength);
                int offsetValue = reader.ReadInt32();

                if (tableNameLength == 0)
                {
                    stream.Seek(stopPosition - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                    return;
                }

                if (tableNameLength != 0 && !offsetMap.ContainsKey(tableName))
                    offsetMap.Add(tableName, offsetValue);
            }
        }
    }
}
