﻿using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using System.Text;

namespace DMS.OffsetPages
{
    public static class OffsetManager
    {
        private const long DefaultBufferValue = -5;
        private const int DefaultIndexValue = 0;

        public static int RecordSizeForOffset(int tableNameLength, int columnCount) => tableNameLength * sizeof(char) + sizeof(int) + sizeof(int) + sizeof(int) + sizeof(int) * columnCount;

        public static void WriteOffsetMapper(KeyValuePair<char[], long> entry, int columnCount)
        {
            int freeSpace = DataPageManager.DataPageSize;
            int sizeOfCurrentRecord = RecordSizeForOffset(entry.Key.Length, columnCount);
            long pointerToNextPage = PointerToNextPage();//this is the end byte of the pointer
            long startOfFreeOffset = pointerToNextPage - DataPageManager.DataPageSize;

            //this is the case when the there is no offset and I need to initialize it
            if (pointerToNextPage == -2)
            {
                InitFirstOffsetTable(entry, sizeOfCurrentRecord, columnCount, ref freeSpace);
                return;
            }

            freeSpace = FreeSpaceInOffset(sizeOfCurrentRecord);

            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter binaryWriter = new(fs, Encoding.UTF8);

            //the data will overflow the current offset page and I need to create a new one
            if (freeSpace == DefaultBufferValue)
            {
                freeSpace = DataPageManager.DataPageSize - sizeOfCurrentRecord;
                CreateNewOffsetTable(entry, fs, binaryWriter, startOfFreeOffset, columnCount, ref freeSpace);
                return;
            }

            WriteToCurrentOffsetTable(entry, fs, binaryWriter, startOfFreeOffset, sizeOfCurrentRecord, columnCount, ref freeSpace);
        }

        public static DKDictionary<char[], long> ReadTableOffsets()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);
            int freeSpace = reader.ReadInt32();

            DKDictionary<char[], long> offsetMap = new();
            ReadOffsetTable(binaryStream, reader, offsetMap);

            long nextPagePointer = reader.ReadInt64();
            while (nextPagePointer != DefaultBufferValue)
            {
                binaryStream.Seek(nextPagePointer, SeekOrigin.Begin);
                ReadOffsetTable(binaryStream, reader, offsetMap);
                nextPagePointer = reader.ReadInt64();
            }

            return offsetMap;
        }

        public static void RemoveOffsetRecord(char[] tableName)
        {
            byte[]? emptyBuffer = null;

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream, Encoding.UTF8);

            void EraseRecordIfMatch()
            {
                int tableNameLength = reader.ReadInt32();
                char[] currentTableName = reader.ReadChars(tableNameLength);
                int offsetValue = reader.ReadInt32();
                int columnCount = reader.ReadInt32();

                for (int i = 0; i < columnCount; i++)
                    reader.ReadInt32();

                if (!currentTableName.SequenceEqual(tableName) || tableNameLength != tableName.Length)
                    return;

                int recordSizeInBytes = tableNameLength + sizeof(int) + sizeof(int);
                emptyBuffer ??= new byte[recordSizeInBytes];
                binaryStream.Seek(binaryStream.Position - recordSizeInBytes, SeekOrigin.Begin);
                binaryStream.Write(emptyBuffer);
            }

            long stopPosition = DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            int freeSpace = reader.ReadInt32();
            while (binaryStream.Position < stopPosition)
                EraseRecordIfMatch();

            long pointer = reader.ReadInt64();
            while (pointer != DefaultBufferValue)
            {
                binaryStream.Seek(pointer, SeekOrigin.Begin);
                freeSpace = reader.ReadInt32();

                stopPosition = binaryStream.Position + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                while (binaryStream.Position < stopPosition)
                    EraseRecordIfMatch();

                pointer = reader.ReadInt64();
            }
        }

        public static (byte[] offsetValues, long endOfRecordOffsetValues) GetDataPageOffsetByTableName(char[] tableName)
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);
            int freeSpace = reader.ReadInt32();
            long stopPosition = DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;

            while (binaryStream.Position < stopPosition)
            {
                byte[] result = CheckAndGetResult();
                if (result is not null)
                    return (result, binaryStream.Position);
            }

            long pointer = reader.ReadInt64();
            while (pointer is not DefaultBufferValue)
            {
                binaryStream.Seek(pointer, SeekOrigin.Begin);
                freeSpace = reader.ReadInt32();
                long stop = binaryStream.Position + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                while (binaryStream.Position < stop)
                {
                    byte[] result = CheckAndGetResult();
                    if (result is not null)
                        return (result, binaryStream.Position);
                }

                pointer = reader.ReadInt64();
            }

            return (Array.Empty<byte>(), 0);

            byte[] CreateResultArray(
                int tableNameLength,
                char[] currentTableName,
                int offsetValue,
                int columnCount,
                IEnumerable<int> columnOffsetIndex)
            {
                int recordSizeInBytes = RecordSizeForOffset(tableNameLength, columnCount);
                byte[] result = new byte[recordSizeInBytes];
                using MemoryStream memoryStream = new(result);
                using BinaryWriter writer = new(memoryStream, Encoding.UTF8);

                writer.Write(tableNameLength);
                writer.Write(currentTableName);
                writer.Write(offsetValue);
                writer.Write(columnCount);

                foreach (int columnOffset in columnOffsetIndex)
                    writer.Write(columnOffset);

                return result;
            }

            byte[] CheckAndGetResult()
            {
                int tableNameLength = reader.ReadInt32();
                char[] currentTableName = reader.ReadChars(tableNameLength);
                int offsetValue = reader.ReadInt32();
                reader.ReadInt32();//no idea why this is here but there is 0 int after the offset value and cant find why???????? without this the logic it is not working
                int columnCount = reader.ReadInt32();

                DKList<int> columnOffsetIndex = new();
                for (int i = 0; i < columnCount; i++)
                    columnOffsetIndex.Add(reader.ReadInt32());

                if (currentTableName.SequenceEqual(tableName) && tableNameLength == tableName.Length)
                    return CreateResultArray(tableNameLength, currentTableName, offsetValue, columnCount, columnOffsetIndex);

                return null;
            }
        }

        private static void InitFirstOffsetTable(
            KeyValuePair<char[], long> entry,
            int sizeOfCurrentRecord,
            int columnCount,
            ref int freeSpace)
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
            using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart, SeekOrigin.Begin);

            freeSpace -= sizeOfCurrentRecord;
            writer.Write(freeSpace); // 4 bytes for free space

            writer.Write(entry.Key.Length); // 4 bytes for the length of the table name
            writer.Write(entry.Key); // 1 byte per char
            writer.Write(entry.Value);// 4 bytes for the start offset of the record
            writer.Write(columnCount);// 4 bytes for number of columns 

            for (int i = 0; i < columnCount; i++)
                writer.Write(DefaultIndexValue);

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
            writer.Write(DefaultBufferValue);

            DataPageManager.AllDataPagesCount++;
        }

        private static void CreateNewOffsetTable(
            KeyValuePair<char[], long> entry,
            FileStream fs,
            BinaryWriter binaryWriter,
            long startOfFreeOffset,
            int columnCount,
            ref int freeSpace)
        {
            fs.Seek(startOfFreeOffset + DataPageManager.DataPageSize, SeekOrigin.Begin);

            binaryWriter.Write(freeSpace);

            binaryWriter.Write(entry.Key.Length); // 4 bytes for the length of the table name
            binaryWriter.Write(entry.Key); // 1 byte per char
            binaryWriter.Write(entry.Value);// 4 bytes for the start offset of the record
            binaryWriter.Write(columnCount);// 4 bytes for number of columns 

            for (int i = 0; i < columnCount; i++)
                binaryWriter.Write(DefaultIndexValue);

            fs.Seek(startOfFreeOffset + (DataPageManager.DataPageSize * 2) - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
            binaryWriter.Write(DefaultBufferValue);

            DataPageManager.AllDataPagesCount++;
        }

        private static void WriteToCurrentOffsetTable(
            KeyValuePair<char[], long> entry,
            FileStream fs,
            BinaryWriter binaryWriter,
            long startOfFreeOffset,
            int columnCount,
            int sizeOfCurrentRecord,
            ref int freeSpace)
        {
            fs.Seek(startOfFreeOffset, SeekOrigin.Begin);

            //write to the current offset page
            long startingPoint = startOfFreeOffset + (DataPageManager.DataPageSize - freeSpace) + sizeof(int);
            freeSpace -= sizeOfCurrentRecord;

            binaryWriter.Write(freeSpace); // 4 bytes for free space
            fs.Seek(startingPoint, SeekOrigin.Begin);

            binaryWriter.Write(entry.Key.Length); // 4 bytes for the length of the table name
            binaryWriter.Write(entry.Key); // 1 byte per char
            binaryWriter.Write(entry.Value);// 4 bytes for the start offset of the record
            binaryWriter.Write(columnCount);// 4 bytes for number of columns 

            for (int i = 0; i < columnCount; i++)
                binaryWriter.Write(DefaultIndexValue);

            fs.Seek(startOfFreeOffset + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
            binaryWriter.Write(DefaultBufferValue);
        }

        private static long PointerToNextPage()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream);

            binaryStream.Seek(DataPageManager.FirstOffsetPageStart + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
            try
            {
                long pointer = reader.ReadInt64();
                while (pointer != DefaultBufferValue)
                {
                    binaryStream.Seek(pointer, SeekOrigin.Begin);
                    pointer = reader.ReadInt64();
                }

                return binaryStream.Position;
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
            int freeSpace = reader.ReadInt32();
            if (requiredSpace + DataPageManager.BufferOverflowPointer < freeSpace)
                return freeSpace;

            return (int)DefaultBufferValue;
        }

        private static void ReadOffsetTable(FileStream stream, BinaryReader reader, IDictionary<char[], long> offsetMap)
        {
            long stopPosition = stream.Position + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer - sizeof(int); //<- this sizeof(int) is free space variable

            while (stream.Position < stopPosition)
            {
                int tableNameLength = reader.ReadInt32();
                char[] tableName = reader.ReadChars(tableNameLength);
                int offsetValue = reader.ReadInt32();
                int columnCount = reader.ReadInt32(); // number of columns to read for indexing 

                for (int i = 0; i < columnCount; i++)
                    reader.ReadInt32();//the start of the index tree in the file for the given column if the value is 0 that means there is not index for the given column

                if (tableNameLength == 0)
                {
                    stream.Seek(stopPosition, SeekOrigin.Begin);
                    return;
                }

                if (tableNameLength is not 0 && !offsetMap.ContainsKey(tableName))
                    offsetMap.Add(tableName, offsetValue);
            }
        }
    }
}
