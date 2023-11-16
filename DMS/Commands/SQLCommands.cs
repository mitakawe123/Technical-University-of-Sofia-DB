﻿using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using System.Text;

namespace DMS.Commands
{
    public static class SQLCommands
    {
        //createtable test(id int primary key, name string(max) null)
        //insert into test (id, name) values (1, 'hellot123'), (2, 'test2main'), (3, 'test3')
        //select * from test
        public static void InsertIntoTable(IReadOnlyList<IReadOnlyList<char[]>> columnsValues, ReadOnlySpan<char> tableName)
        {
            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();
            char[] matchingKey = FindTableWithName(tableName);

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) = ReadTableMetadata(reader);
            int headerSectionForMainDP = 20 + tableLength;
            (headerSectionForMainDP, DKList<Column> columnNameAndType) = ReadColumns(reader, headerSectionForMainDP, columnCount);

            long firstFreeDP = FindFirstFreeDataPageOffsetStart(fileStream, reader, DataPageManager.TableOffsets[matchingKey]);
            bool isMainDP = firstFreeDP == DataPageManager.TableOffsets[matchingKey];

            fileStream.Close();
            reader.Close();

            byte[] allRecords = GetAllData(columnsValues);
            InsertIntoFreeSpace(allRecords, isMainDP, headerSectionForMainDP, firstFreeDP);
        }

        public static void SelectFromTable(ReadOnlySpan<char> valuesToSelect, ReadOnlySpan<char> tableName)
        {
            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();
            char[] matchingKey = FindTableWithName(tableName);

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) = ReadTableMetadata(reader);
            int headerSectionForMainDP = 20 + tableLength;
            (headerSectionForMainDP, DKList<Column> columnNameAndType) = ReadColumns(reader, headerSectionForMainDP, columnCount);

            long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDP;
            long end = DataPageManager.TableOffsets[matchingKey] + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            long lengthToRead = end - start;

            fileStream.Seek(start, SeekOrigin.Begin);

            byte[] buffer = new byte[lengthToRead];
            int bytesRead = fileStream.Read(buffer, 0, (int)lengthToRead);

            fileStream.Seek(end, SeekOrigin.Begin);

            IReadOnlyList<IReadOnlyList<char[]>> allData = ReadAllData(buffer);
            long pointer = reader.ReadInt64();

            while (pointer != DataPageManager.DefaultBufferForDP)
            {
                fileStream.Seek(pointer, SeekOrigin.Begin);
                reader.ReadInt32();//<- free space
                start = pointer + sizeof(int);
                end = pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                lengthToRead = end - start;

                allData = allData.Concat(ReadAllData(new byte[lengthToRead])).ToList().AsReadOnly();
                fileStream.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            fileStream.Close();
            reader.Close();

            PrintSelectedValues(allData, valuesToSelect, columnNameAndType);
        }

        private static void PrintSelectedValues(IReadOnlyList<IReadOnlyList<char[]>> allData, ReadOnlySpan<char> valuesToSelect, IReadOnlyList<Column> columnNameAndType)
        {
            foreach (IReadOnlyList<char[]> value in allData)
            {
                IReadOnlyList<char[]> val = value;
            }
        }

        private static (FileStream, BinaryReader) OpenFileAndReader()
        {
            FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryReader reader = new(fileStream, Encoding.UTF8);
            return (fileStream, reader);
        }

        private static (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) ReadTableMetadata(BinaryReader reader)
        {
            int freeSpace = reader.ReadInt32();
            ulong recordSizeInBytes = reader.ReadUInt64();
            int tableLength = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(tableLength);
            string table = Encoding.UTF8.GetString(bytes);
            int columnCount = reader.ReadInt32();

            return (freeSpace, recordSizeInBytes, tableLength, table, columnCount);
        }

        private static (int headerSectionForMainDP, DKList<Column> columnNameAndType) ReadColumns(BinaryReader reader, int initialHeaderSection, int columnCount)
        {
            int headerSectionForMainDP = initialHeaderSection;
            DKList<Column> columnNameAndType = new();

            for (int i = 0; i < columnCount; i++)
            {
                string columnType = reader.ReadString();
                string columnName = reader.ReadString();
                headerSectionForMainDP += columnName.Length * 2 + columnType.Length * 2;
                columnNameAndType.Add(new Column(columnName, columnType));
            }

            return (headerSectionForMainDP, columnNameAndType);
        }

        private static char[] FindTableWithName(ReadOnlySpan<char> tableName)
        {
            char[]? matchingKey = null;
            foreach (KeyValuePair<char[], long> item in DataPageManager.TableOffsets)
            {
                if (tableName.SequenceEqual(item.Key))
                {
                    matchingKey = item.Key;
                    break;
                }
            }

            if (matchingKey is null || !DataPageManager.TableOffsets.ContainsKey(matchingKey))
                throw new Exception("Cannot find table");

            return matchingKey;
        }

        private static void InsertIntoFreeSpace(byte[] allRecords, bool isMainDP, int headerSectionForMainDP, long firstFreeDP)
        {
            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);// <- initiate new file stream because the old one is not writable even through I gave full permissions for the stream
            using BinaryWriter writer = new(fs, Encoding.UTF8);

            int recordIndex = 0;
            int recordLength = allRecords.Length;

            fs.Seek(firstFreeDP, SeekOrigin.Begin);

            byte[] freeSpaceBytes = new byte[4];
            fs.Read(freeSpaceBytes, 0, 4);//<- free space
            int freeSpace = BitConverter.ToInt32(freeSpaceBytes, 0);

            if (isMainDP)
                fs.Seek(firstFreeDP + headerSectionForMainDP, SeekOrigin.Begin);
            else
                fs.Seek(firstFreeDP + sizeof(int), SeekOrigin.Begin);

            while (recordIndex < recordLength)
            {
                // Calculate the amount of data to write in this iteration
                int dataToWrite = (int)Math.Min(recordLength - recordIndex, freeSpace - DataPageManager.BufferOverflowPointer);//<- this is some times 0
                freeSpace -= dataToWrite;

                // Write the data
                writer.Write(allRecords, recordIndex, dataToWrite);
                recordIndex += dataToWrite;

                //go back and update free space in the current data page
                fs.Seek(firstFreeDP, SeekOrigin.Begin);
                writer.Write(freeSpace);

                if (dataToWrite == 0)
                    return;

                // Move to the end of the current page and read the pointer
                fs.Seek(firstFreeDP + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                byte[] pointerBytes = new byte[8];
                fs.Read(pointerBytes, 0, 8);
                long pointer = BitConverter.ToInt64(pointerBytes, 0);

                // Check if the pointer is default, indicating a new page is needed
                if (pointer == DataPageManager.DefaultBufferForDP)
                {
                    // Allocate new page
                    pointer = (DataPageManager.AllDataPagesCount + 1) * DataPageManager.DataPageSize;

                    // Write the pointer to the current page
                    fs.Seek(DataPageManager.AllDataPagesCount * DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                    writer.Write(pointer);

                    DataPageManager.DataPageCounter++;
                    DataPageManager.AllDataPagesCount++;
                }

                // Move to the new page
                fs.Seek(pointer, SeekOrigin.Begin);
            }
        }

        private static long FindFirstFreeDataPageOffsetStart(FileStream fs, BinaryReader reader, long currentPosition)
        {
            long startOfFreeDataPageOffset = currentPosition;

            fs.Seek(currentPosition + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);

            long pointer = reader.ReadInt64();
            while (pointer != DataPageManager.DefaultBufferForDP && pointer != 0)
            {
                startOfFreeDataPageOffset = pointer;
                fs.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer - sizeof(int), SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            return startOfFreeDataPageOffset == DataPageManager.DefaultBufferForDP ? currentPosition : startOfFreeDataPageOffset;
        }

        private static byte[] GetAllData(IReadOnlyList<IReadOnlyList<char[]>> columnsValues)
        {
            int totalSize = 0;
            foreach (IReadOnlyList<char[]> column in columnsValues)
            {
                foreach (char[] charArray in column)
                {
                    totalSize += sizeof(int); // Add 4 bytes for the length of char[]
                    totalSize += Encoding.UTF8.GetByteCount(charArray);
                }
            }

            byte[] allBytes = new byte[totalSize];

            int currentPosition = 0;
            foreach (IReadOnlyList<char[]> column in columnsValues)
            {
                foreach (char[] charArray in column)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(charArray);
                    byte[] lengthPrefix = BitConverter.GetBytes(byteArray.Length);

                    lengthPrefix.CopyTo(allBytes, currentPosition);
                    currentPosition += sizeof(int);

                    byteArray.CopyTo(allBytes, currentPosition);
                    currentPosition += byteArray.Length;
                }
            }

            return allBytes;
        }

        private static IReadOnlyList<IReadOnlyList<char[]>> ReadAllData(byte[] allBytes)//<- this will come in handy for the Select 
        {
            DKList<DKList<char[]>> columnsValues = new();
            int currentPosition = 0;

            while (currentPosition < allBytes.Length)
            {
                DKList<char[]> column = new();

                while (currentPosition < allBytes.Length)
                {
                    // Read the length of the next char[]
                    int charArrayLength = BitConverter.ToInt32(allBytes, currentPosition);
                    currentPosition += sizeof(int);

                    byte[] byteArray = new byte[charArrayLength];
                    Array.Copy(allBytes, currentPosition, byteArray, 0, charArrayLength);
                    currentPosition += charArrayLength;

                    char[] charArray = Encoding.UTF8.GetChars(byteArray);
                    column.Add(charArray);
                }

                columnsValues.Add(column);
            }

            return columnsValues.AsReadOnly();
        }
    }
}
