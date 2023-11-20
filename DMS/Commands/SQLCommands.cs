using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using System.Text;
using DMS.Extensions;

namespace DMS.Commands
{
    public static class SQLCommands
    {
        public static void InsertIntoTable(IReadOnlyList<IReadOnlyList<char[]>> columnsValues, ReadOnlySpan<char> tableName)
        {
            char[] matchingKey = FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is not table with the given name");
                return;
            }

            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) =
                ReadTableMetadata(reader);

            int headerSectionForMainDP = 20 + tableLength;
            (headerSectionForMainDP, DKList<Column> columnNameAndType) =
                ReadColumns(reader, headerSectionForMainDP, columnCount);

            long firstFreeDP =
                FindFirstFreeDataPageOffsetStart(fileStream, reader, DataPageManager.TableOffsets[matchingKey]);

            bool isMainDP = firstFreeDP == DataPageManager.TableOffsets[matchingKey];

            fileStream.Close();
            reader.Close();

            byte[] allRecords = GetAllData(columnsValues);
            InsertIntoFreeSpace(allRecords, isMainDP, headerSectionForMainDP, firstFreeDP);
        }

        public static void SelectFromTable(DKList<string> valuesToSelect, ReadOnlySpan<char> tableName, ReadOnlySpan<char> logicalOperator)
        {
            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();
            char[] matchingKey = FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is no table with the given name");
                fileStream.Close();
                reader.Close();
                return;
            }

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) = ReadTableMetadata(reader);
            int headerSectionForMainDP = 20 + tableLength;

            (headerSectionForMainDP, DKList<Column> columnTypeAndName) = ReadColumns(reader, headerSectionForMainDP, columnCount);

            long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDP;
            long end = DataPageManager.TableOffsets[matchingKey] + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            long lengthToRead = end - start;

            fileStream.Seek(start, SeekOrigin.Begin);

            DKList<char[]> allData = ReadAllData(lengthToRead, reader);

            fileStream.Seek(end, SeekOrigin.Begin);
            long pointer = reader.ReadInt64();

            while (pointer != DataPageManager.DefaultBufferForDP)
            {
                fileStream.Seek(pointer, SeekOrigin.Begin);
                reader.ReadInt32(); //<- free space
                start = pointer + sizeof(int);
                end = pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                lengthToRead = end - start;

                allData = allData.Concat(ReadAllData(lengthToRead, reader)).CustomToList();
                fileStream.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            fileStream.Close();
            reader.Close();

            allData.RemoveAll(x => x.Length == 0);

            PrintSelectedValues(allData, valuesToSelect, columnTypeAndName, logicalOperator, columnCount);
        }

        public static void DeleteFromTable(ReadOnlySpan<char> tableName, IReadOnlyList<string> logicalOperators, IReadOnlyList<string> columnsCondition)//<- can contains not keyword
        {
            char[] matchingKey = FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is not table with the given name");
                return;
            }

            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) = ReadTableMetadata(reader);
            int headerSectionForMainDP = 20 + tableLength;

            (int header, DKList<Column> columnNameAndType) = ReadColumns(reader, headerSectionForMainDP, columnCount);

            long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDP;
            long end = DataPageManager.TableOffsets[matchingKey] + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            long lengthToRead = end - start;

            for (int i = 0; i < logicalOperators.Count; i++)
            {
                string logicalOperator = logicalOperators[i].CustomTrim();


            }

            //what needs to happen here 
            //first check if the given columns in the where are valid
            //second split by operands is,not,or,and if they exist
            //third find all the records that meet the criteria and replace them with empty bytes
        }

        public static DKList<char[]> ReadAllData(long lengthToRead, BinaryReader reader)
        {
            DKList<char[]> columnsValues = new();
            int offset = 0;
            while (offset < lengthToRead)
            {
                int length = reader.ReadInt32();
                offset += sizeof(int);

                if (offset >= lengthToRead)
                    return columnsValues;

                char[] charArray = reader.ReadChars(length);
                offset += length;

                columnsValues.Add(charArray);
            }

            columnsValues.RemoveAll(x => x.Length == 0);
            return columnsValues;
        }

        public static (int header, DKList<Column> columnNameAndType) ReadColumns(BinaryReader reader, int initialHeaderSection, int columnCount)
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

        public static (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) ReadTableMetadata(BinaryReader reader)
        {
            int freeSpace = reader.ReadInt32();
            ulong recordSizeInBytes = reader.ReadUInt64();
            int tableLength = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(tableLength);
            string table = Encoding.UTF8.GetString(bytes);
            int columnCount = reader.ReadInt32();

            return (freeSpace, recordSizeInBytes, tableLength, table, columnCount);
        }

        private static void PrintSelectedValues(
            IReadOnlyList<char[]> allData, DKList<string> valuesToSelect,
            IReadOnlyList<Column> columnTypeAndName,
            ReadOnlySpan<char> logicalOperator,
            int colCount)
        {
            DKList<Column> selectedColumns = columnTypeAndName.CustomWhere(c => valuesToSelect.CustomContains(c.Name) || valuesToSelect.CustomContains("*")).CustomToList();
            int tableWidth = selectedColumns.CustomSum(c => c.Name.Length) + (selectedColumns.Count - 1) * 3 + 4;

            LogicalOperators.Parse(ref allData, selectedColumns, columnTypeAndName, logicalOperator, colCount);

            Console.WriteLine(new string('-', tableWidth));

            Console.Write("| ");
            foreach (Column column in selectedColumns)
            {
                Console.Write(column.Name);
                Console.Write(!column.Equals(selectedColumns[^1]) ? " | " : " |");
            }
            Console.WriteLine();

            Console.WriteLine(new string('-', tableWidth));

            int columnCount = selectedColumns.Count;
            for (int i = 0, columnIndex = 0; i < allData.Count; i++)
            {
                if (allData[i].Length <= 0)
                    continue;

                string columnName = columnTypeAndName[columnIndex % columnTypeAndName.Count].Name;
                if (valuesToSelect.CustomContains(columnName) || valuesToSelect.CustomContains("*"))
                {
                    if (columnIndex % columnCount is 0)
                        Console.Write("| ");

                    Console.Write(new string(allData[i]));

                    if ((columnIndex + 1) % columnCount is not 0)
                        Console.Write(" | ");
                    else
                        Console.WriteLine(" |");
                }

                columnIndex++;
            }

            Console.WriteLine(new string('-', tableWidth));
        }

        private static (FileStream, BinaryReader) OpenFileAndReader()
        {
            FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryReader reader = new(fileStream, Encoding.UTF8);
            return (fileStream, reader);
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
                return Array.Empty<char>();

            return matchingKey;
        }

        private static void InsertIntoFreeSpace(byte[] allRecords, bool isMainDP, int headerSectionForMainDP, long firstFreeDP)
        {
            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open); // <- initiate new file stream because the old one is not writable even through I gave full permissions for the stream
            using BinaryWriter writer = new(fs, Encoding.UTF8);

            int recordIndex = 0;
            int recordLength = allRecords.Length;

            fs.Seek(firstFreeDP, SeekOrigin.Begin);

            byte[] freeSpaceBytes = new byte[4];
            fs.Read(freeSpaceBytes, 0, 4); //<- free space
            int freeSpace = BitConverter.ToInt32(freeSpaceBytes, 0);

            fs.Seek(isMainDP ? firstFreeDP + headerSectionForMainDP : firstFreeDP + sizeof(int), SeekOrigin.Begin);

            while (recordIndex < recordLength)
            {
                // Calculate the amount of data to write in this iteration
                int dataToWrite = (int)Math.Min(recordLength - recordIndex, freeSpace - DataPageManager.BufferOverflowPointer);
                freeSpace -= dataToWrite;

                // Write the data
                writer.Write(allRecords, recordIndex, dataToWrite);
                recordIndex += dataToWrite;

                //go back and update free space in the current data page
                fs.Seek(firstFreeDP, SeekOrigin.Begin);
                writer.Write(freeSpace);

                if (recordIndex == recordLength)
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

            fs.Seek(currentPosition + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer,
                SeekOrigin.Begin);

            long pointer = reader.ReadInt64();
            while (pointer != DataPageManager.DefaultBufferForDP && pointer != 0)
            {
                startOfFreeDataPageOffset = pointer;
                fs.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer - sizeof(int),
                    SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            return startOfFreeDataPageOffset == DataPageManager.DefaultBufferForDP
                ? currentPosition
                : startOfFreeDataPageOffset;
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
    }
}