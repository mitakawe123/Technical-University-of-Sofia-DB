using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using System.Text;
using DMS.Extensions;
using DMS.Utils;

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

            var metadata = ReadTableMetadata(reader);

            int headerSectionForMainDp = 20 + metadata.tableLength;
            (headerSectionForMainDp, DKList<Column> columnNameAndType) = ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            long firstFreeDp = FindFirstFreeDataPageOffsetStart(fileStream, reader, DataPageManager.TableOffsets[matchingKey]);

            bool isMainDp = firstFreeDp == DataPageManager.TableOffsets[matchingKey];

            fileStream.Close();
            reader.Close();

            byte[] allRecords = GetAllData(columnsValues);
            InsertIntoFreeSpace(allRecords, isMainDp, headerSectionForMainDp, firstFreeDp);
        }

        public static void SelectFromTable(DKList<string> valuesToSelect, ReadOnlySpan<char> tableName, ReadOnlySpan<char> logicalOperator)
        {
            char[] matchingKey = FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is no table with the given name");
                return;
            }

            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            var metadata = ReadTableMetadata(reader);
            int headerSectionForMainDp = 20 + metadata.tableLength;

            (headerSectionForMainDp, DKList<Column> columnTypeAndName) = ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDp;
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

            allData.RemoveAll(charArray => charArray.Length == 0 || charArray.All(c => c == '\0'));

            PrintSelectedValues(allData, valuesToSelect, columnTypeAndName, logicalOperator, metadata.columnCount);
        }
        //trying to make it work then split and refactor the code
        public static void DeleteFromTable(ReadOnlySpan<char> tableName, IReadOnlyList<string> logicalOperators, IReadOnlyList<string> columns)//<- can contains not keyword
        {
            char[] matchingKey = FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is not table with the given name");
                return;
            }

            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();
            using BinaryWriter writer = new(fileStream);

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            var metadata = ReadTableMetadata(reader);
            int headerSectionForMainDp = 20 + metadata.tableLength;

            (headerSectionForMainDp, DKList<Column> columnNameAndType) = ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            bool allElementsContained = columns.CustomAll(x => columnNameAndType.CustomAny(y => y.Name == x));//there can be case with not in front of the column
            if (!allElementsContained)
            {
                Console.WriteLine("Wrong column in the where clause");
                reader.Close();
                fileStream.Close();
                return;
            }

            long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDp;
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

            allData.RemoveAll(x => x.Length == 0);

            DKList<string> operations = new();

            foreach (string item in logicalOperators)
            {
                string logicalOperator = item.CustomTrim();
                DKList<string> operation = HelperMethods.SplitSqlQuery(logicalOperator);
                operations.Add(operation[0]);

                var operatorAndIndex = LogicalOperators.ParseOperation(operation[0]);
                string op = operatorAndIndex.Item1;
                int operatorIndex = operatorAndIndex.Item2;

                char[] value = LogicalOperators.GetValueFromOperation(operation[0], operatorIndex);

                int whiteSpace = operation[0].CustomIndexOf(' ');
                string column = whiteSpace != -1 ? operation[0][..whiteSpace] : operation[0][..(operatorAndIndex.Item2 - 1)];

                foreach (char[] charArr in allData)
                {
                    if (!LogicalOperators.CompareValues(charArr, value, op))
                        continue;

                    fileStream.Seek(start, SeekOrigin.Begin);
                    int rowsDeleted = ReadAndDeleteData(fileStream, reader, writer, lengthToRead, value, op, metadata.columnCount);
                    if (rowsDeleted > 0)
                        Console.WriteLine($"Rows affected {rowsDeleted}");
                }
            }

            reader.Close();
            fileStream.Close();
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

        private static int ReadAndDeleteData(
            FileStream fileStream,
            BinaryReader reader,
            BinaryWriter writer,
            long lengthToRead,
            char[] value,
            string operation,
            int columnCount)
        {
            int deletedRowsCounter = 0;
            long offset = 0;

            try
            {
                while (offset < lengthToRead)
                {
                    if (TryReadRow(reader, lengthToRead, ref offset, out char[] charArray, out int recordLength))
                    {
                        if (!charArray.SequenceEqual(value) ||
                            !LogicalOperators.CompareValues(charArray, value, operation))
                            continue;

                        DeleteRow(fileStream, reader, writer, columnCount, recordLength);
                        deletedRowsCounter++;
                    }
                    else
                        break;
                }
            }
            catch (Exception)
            {
            }

            return deletedRowsCounter;
        }

        private static bool TryReadRow(BinaryReader reader, long lengthToRead, ref long offset, out char[] charArray, out int recordLength)
        {
            charArray = null;
            recordLength = reader.ReadInt32();
            offset += sizeof(int);

            if (offset >= lengthToRead)
                return false;

            charArray = reader.ReadChars(recordLength);
            offset += recordLength;
            return true;
        }

        private static void DeleteRow(FileStream fileStream, BinaryReader reader, BinaryWriter writer, int columnCount, int recordLength)
        {
            fileStream.Seek(-recordLength, SeekOrigin.Current);
            
            writer.Write(new char[recordLength]);

            for (int i = 0; i < columnCount - 1; i++)
            {
                int length = reader.ReadInt32();
                writer.Write(new char[length]);
            }
        }

        public static (int headerSectionForMainDp, DKList<Column> columnNameAndType) ReadColumns(BinaryReader reader, int initialHeaderSection, int columnCount)
        {
            int headerSectionForMainDp = initialHeaderSection;
            DKList<Column> columnNameAndType = new();

            for (int i = 0; i < columnCount; i++)
            {
                string columnType = reader.ReadString();
                string columnName = reader.ReadString();
                headerSectionForMainDp += columnName.Length * 2 + columnType.Length * 2;
                columnNameAndType.Add(new Column(columnName, columnType));
            }

            return (headerSectionForMainDp, columnNameAndType);
        }

        public static (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) ReadTableMetadata(BinaryReader reader)
        {
            int freeSpace = reader.ReadInt32(); // 4 bytes
            ulong recordSizeInBytes = reader.ReadUInt64();// 8 bytes
            int tableLength = reader.ReadInt32();// 4 bytes
            byte[] bytes = reader.ReadBytes(tableLength); // 1 byte per char
            string table = Encoding.UTF8.GetString(bytes);
            int columnCount = reader.ReadInt32(); // 4 bytes

            return (freeSpace, recordSizeInBytes, tableLength, table, columnCount);
        }

        private static void PrintSelectedValues(
            IReadOnlyList<char[]> allData, 
            DKList<string> valuesToSelect,
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
            if (DataPageManager.TableOffsets.Count is 0)
                return Array.Empty<char>();

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

        private static void InsertIntoFreeSpace(byte[] allRecords, bool isMainDp, int headerSectionForMainDp, long firstFreeDp)
        {
            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open); // <- initiate new file stream because the old one is not writable even through I gave full permissions for the stream
            using BinaryWriter writer = new(fs, Encoding.UTF8);

            int recordIndex = 0;
            int recordLength = allRecords.Length;

            fs.Seek(firstFreeDp, SeekOrigin.Begin);

            byte[] freeSpaceBytes = new byte[4];
            fs.Read(freeSpaceBytes, 0, 4); //<- free space
            int freeSpace = BitConverter.ToInt32(freeSpaceBytes, 0);

            long startingPosition = firstFreeDp + DataPageManager.DataPageSize - freeSpace;

            //fs.Seek(isMainDp ? startingPosition : startingPosition + sizeof(int), SeekOrigin.Begin);
            fs.Seek(startingPosition, SeekOrigin.Begin);

            while (recordIndex < recordLength)
            {
                // Calculate the amount of data to write in this iteration
                int dataToWrite = (int)Math.Min(recordLength - recordIndex, freeSpace - DataPageManager.BufferOverflowPointer);
                freeSpace -= dataToWrite;

                // Write the data
                writer.Write(allRecords, recordIndex, dataToWrite);
                recordIndex += dataToWrite;

                //go back and update free space in the current data page
                fs.Seek(firstFreeDp, SeekOrigin.Begin);
                writer.Write(freeSpace);

                // Move to the end of the current page and read the pointer
                fs.Seek(firstFreeDp + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
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
                    totalSize += charArray.Length; // 1 byte per char
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