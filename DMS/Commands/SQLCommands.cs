using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using System.Text;
using DMS.DataRecovery;
using DMS.Extensions;
using DMS.Utils;

namespace DMS.Commands
{
    public static class SqlCommands
    {
        public static void InsertIntoTable(IReadOnlyList<IReadOnlyList<char[]>> columnsValues, IReadOnlyList<char[]> selectedColumns,ReadOnlySpan<char> tableName)
        {
            char[] matchingKey = HelperMethods.FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is not table with the given name");
                return;
            }

            (FileStream fs, BinaryReader reader) = OpenFileAndReader();

            fs.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            var metadata = ReadTableMetadata(reader);
                
            int headerSectionForMainDp = DataPageManager.Metadata + metadata.tableLength;
            (headerSectionForMainDp, DKList<Column> columnNameAndType) = ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            bool areValidTypes = TypeValidation.CheckValidityOfColumnValuesBasedOnType(columnNameAndType, columnsValues);
            if (!areValidTypes)
            {
                Console.WriteLine($"Invalid types when inserting into table {tableName}");
                CloseStreamAndReader(fs, reader);
                return;
            }

            long firstFreeDp = FindFirstFreeDataPageOffsetStart(fs, reader, DataPageManager.TableOffsets[matchingKey]);

            CloseStreamAndReader(fs, reader);

            byte[] allRecords = GetAllData(columnsValues);

            InsertIntoFreeSpace(allRecords, firstFreeDp);
        }

        public static SelectQueryParams SelectFromTable(
            DKList<string> valuesToSelect,
            ReadOnlySpan<char> tableName,
            ReadOnlySpan<char> logicalOperator,
            bool isForUi = false)
        {
            char[] matchingKey = HelperMethods.FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is no table with the given name");
                return default;
            }

            (FileStream fileStream, BinaryReader reader) = OpenFileAndReader();

            fileStream.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            var metadata = ReadTableMetadata(reader);
            int headerSectionForMainDp = DataPageManager.Metadata + metadata.tableLength;

            (headerSectionForMainDp, DKList<Column> columnTypeAndName) = ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            long start = ReadAllDataFromAllDataPages(fileStream, reader, matchingKey, headerSectionForMainDp, out var lengthToRead, out var allData);

            CloseStreamAndReader(fileStream, reader);

            allData.RemoveAll(charArray => charArray.Length == 0 || charArray.CustomAll(c => c == '\0'));

            if (isForUi)
                return PrintSelectedValuesInUi(allData, valuesToSelect, columnTypeAndName, logicalOperator, metadata.columnCount);

            PrintSelectedValues(allData, valuesToSelect, columnTypeAndName, logicalOperator, metadata.columnCount);
            return default;
        }

        public static void DeleteFromTable(ReadOnlySpan<char> tableName, IReadOnlyList<string> logicalOperators, IReadOnlyList<string> columns)//<- can contains not keyword
        {
            char[] matchingKey = HelperMethods.FindTableWithName(tableName);

            if (matchingKey == Array.Empty<char>())
            {
                Console.WriteLine("There is not table with the given name");
                return;
            }

            (FileStream fs, BinaryReader reader) = OpenFileAndReader();
            using BinaryWriter writer = new(fs);

            fs.Seek(DataPageManager.TableOffsets[matchingKey], SeekOrigin.Begin);

            var metadata = ReadTableMetadata(reader);
            int headerSectionForMainDp = DataPageManager.Metadata + metadata.tableLength;

            (headerSectionForMainDp, DKList<Column> columnTypeAndName) = ReadColumns(reader, headerSectionForMainDp, metadata.columnCount);

            bool allElementsContained = columns.CustomAll(x => columnTypeAndName.CustomAny(y => y.Name == x));//there can be case with not in front of the column
            if (!allElementsContained)
            {
                Console.WriteLine("Wrong column in the where clause");
                CloseStreamAndReader(fs, reader);
                return;
            }

            long start = ReadAllDataFromAllDataPages(fs, reader, matchingKey, headerSectionForMainDp, out var lengthToRead, out var allData);

            long snapshotHashStartingPoint = start - headerSectionForMainDp;

            allData.RemoveAll(charArray => charArray.Length == 0 || charArray.CustomAll(c => c == '\0'));

            SplitAndFindRecords(fs, reader, writer, logicalOperators, allData, start, lengthToRead, snapshotHashStartingPoint, metadata);

            CloseStreamAndReader(fs, reader);
        }

        public static DKList<char[]> ReadAllDataForSingleDataPage(long lengthToRead, BinaryReader reader)
        {
            DKList<char[]> columnsValues = new();
            int offset = 0;
            while (offset < lengthToRead)
            {
                int length = reader.ReadInt32();
                offset += sizeof(int);

                if (offset >= lengthToRead || length < 0)
                    return columnsValues;

                char[] charArray = reader.ReadChars(length);
                offset += length;

                columnsValues.Add(charArray);
            }

            columnsValues.RemoveAll(charArray => charArray.Length == 0 || charArray.All(c => c == '\0'));
            return columnsValues;
        }

        private static long ReadAllDataFromAllDataPages(
            FileStream fileStream,
            BinaryReader reader,
            char[] matchingKey,
            int headerSectionForMainDp,
            out long lengthToRead,
            out DKList<char[]> allData)
        {
            long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDp;
            long end = DataPageManager.TableOffsets[matchingKey] + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            lengthToRead = end - start;

            fileStream.Seek(start, SeekOrigin.Begin);

            allData = ReadAllDataForSingleDataPage(lengthToRead, reader);

            fileStream.Seek(end, SeekOrigin.Begin);
            long pointer = reader.ReadInt64();

            while (pointer != DataPageManager.DefaultBufferForDp)
            {
                fileStream.Seek(pointer, SeekOrigin.Begin);

                reader.ReadUInt64();// <- hash
                reader.ReadInt32(); //<- free space

                start = pointer + sizeof(int);
                end = pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                lengthToRead = end - start;

                allData = allData.Concat(ReadAllDataForSingleDataPage(lengthToRead, reader)).CustomToList();
                fileStream.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            return start;
        }

        private static void SplitAndFindRecords(
            FileStream fs,
            BinaryReader reader,
            BinaryWriter writer,
            IReadOnlyList<string> logicalOperators,
            DKList<char[]> allData,
            long start,
            long lengthToRead,
            long snapshotHashStartingPoint,
            (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) metadata)
        {
            int deletedRows = 0;

            foreach (string item in logicalOperators)
            {
                string logicalOperator = item.CustomTrim();
                DKList<string> operationList = HelperMethods.SplitSqlQuery(logicalOperator);

                foreach (string operation in operationList)
                {
                    var operatorAndIndex = LogicalOperators.ParseOperation(operation);
                    string op = operatorAndIndex.Item1;
                    int operatorIndex = operatorAndIndex.Item2;

                    char[] value = LogicalOperators.GetValueFromOperation(operation, operatorIndex);

                    foreach (char[] charArr in allData)
                    {
                        if (!LogicalOperators.CompareValues(charArr, value, op))
                            continue;

                        fs.Seek(start, SeekOrigin.Begin);
                        int rowsDeleted = ReadAndDeleteData(fs, reader, writer, lengthToRead, value, op, metadata.columnCount);

                        FileIntegrityChecker.RecalculateHash(fs, writer, snapshotHashStartingPoint);
                        deletedRows += rowsDeleted;
                    }
                }
            }

            Console.WriteLine($"Rows affected {deletedRows}");
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
                        //&& charArray.SequenceEqual(value)
                        if (!LogicalOperators.CompareValues(charArray, value, operation))
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
                // ignore
            }

            return deletedRowsCounter;
        }

        private static bool TryReadRow(BinaryReader reader, long lengthToRead, ref long offset, out char[] charArray, out int recordLength)
        {
            charArray = Array.Empty<char>();
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
            fileStream.Seek(-recordLength, SeekOrigin.Current);// <- case for the first part of the record
            writer.Write(new char[recordLength]);

            for (int i = 0; i < columnCount - 1; i++)
            {
                int length = reader.ReadInt32();
                writer.Write(new char[length]);
            }
        }

        public static (int headerSectionForMainDp, DKList<Column> columnNameAndType) ReadColumns(BinaryReader reader, int initialHeaderSection, int columnCount)
        {
            //catch the case when the columns will overflow in the next page
            int headerSectionForMainDp = initialHeaderSection;
            DKList<Column> columnNameAndType = new();

            for (int i = 0; i < columnCount; i++)
            {
                string columnType = reader.ReadString();
                string columnName = reader.ReadString();
                string defaultValue = reader.ReadString();
                headerSectionForMainDp += columnName.Length * 2 + columnType.Length * 2 + defaultValue.Length * 2;
                columnNameAndType.Add(new Column(columnName, columnType, defaultValue));
            }

            return (headerSectionForMainDp, columnNameAndType);
        }

        public static (int freeSpace, ulong recordSizeInBytes, int tableLength, string table, int columnCount) ReadTableMetadata(BinaryReader reader)
        {
            ulong hash = reader.ReadUInt64(); //8 bytes
            int freeSpace = reader.ReadInt32(); // 4 bytes
            ulong recordSizeInBytes = reader.ReadUInt64();// 8 bytes
            int tableLength = reader.ReadInt32();// 4 bytes
            byte[] bytes = reader.ReadBytes(tableLength); // 1 byte per char
            string table = Encoding.UTF8.GetString(bytes);
            int columnCount = reader.ReadInt32(); // 4 bytes

            return (freeSpace, recordSizeInBytes, tableLength, table, columnCount);
        }

        private static SelectQueryParams PrintSelectedValuesInUi(
            IReadOnlyList<char[]> allData,
            DKList<string> valuesToSelect,
            IReadOnlyList<Column> columnTypeAndName,
            ReadOnlySpan<char> logicalOperator,
            int colCount)
        {
            DKList<Column> selectedColumns = columnTypeAndName.CustomWhere(c => valuesToSelect.CustomContains(c.Name) || valuesToSelect.CustomContains("*")).CustomToList();

            LogicalOperators.Parse(ref allData, selectedColumns, columnTypeAndName, logicalOperator, colCount);

            return new SelectQueryParams()
            {
                AllData = allData,
                ColumnCount = colCount,
                ColumnTypeAndName = columnTypeAndName,
                LogicalOperator = logicalOperator,
                ValuesToSelect = valuesToSelect
            };
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

        private static void InsertIntoFreeSpace(byte[] allRecords, long firstFreeDp)
        {
            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter writer = new(fs, Encoding.UTF8);

            int recordIndex = 0;
            int recordLength = allRecords.Length;
            long snapshotFirstFreeDp = firstFreeDp;

            fs.Seek(firstFreeDp, SeekOrigin.Begin);

            byte[] buffer = new byte[12];
            int bytesRead = fs.Read(buffer, 0, buffer.Length);

            ulong hash = BitConverter.ToUInt64(buffer, 0);
            int freeSpace = BitConverter.ToInt32(buffer, 8);

            long startingPosition = firstFreeDp + DataPageManager.DataPageSize - freeSpace;

            fs.Seek(startingPosition, SeekOrigin.Begin);

            while (recordIndex < recordLength)
            {
                // Calculate the amount of data to write in this iteration
                int dataToWrite = (int)Math.Min(recordLength - recordIndex, freeSpace - DataPageManager.BufferOverflowPointer);
                freeSpace -= dataToWrite;

                // Write the data
                writer.Write(allRecords, recordIndex, dataToWrite);
                recordIndex += dataToWrite;

                //update free space first then the hash
                fs.Seek(snapshotFirstFreeDp + sizeof(ulong), SeekOrigin.Begin);
                writer.Write(freeSpace);

                fs.Seek(snapshotFirstFreeDp, SeekOrigin.Begin);
                FileIntegrityChecker.RecalculateHash(fs, writer, snapshotFirstFreeDp);

                // Move to the end of the current page and read the pointer
                fs.Seek(firstFreeDp + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                byte[] pointerBytes = new byte[8];
                fs.Read(pointerBytes, 0, 8);
                long pointer = BitConverter.ToInt64(pointerBytes, 0);

                // Check if there is more data to be written and add pointer to next DP
                if (recordIndex >= recordLength)
                    continue;

                pointer = (DataPageManager.AllDataPagesCount + 1) * DataPageManager.DataPageSize;
                snapshotFirstFreeDp = pointer;

                // Write the pointer to the current page
                fs.Seek(-DataPageManager.BufferOverflowPointer, SeekOrigin.Current);
                writer.Write(pointer);

                FileIntegrityChecker.RecalculateHash(fs, writer, pointer);

                freeSpace = DataPageManager.DataPageSize;

                DataPageManager.DataPageCounter++;
                DataPageManager.AllDataPagesCount++;
            }
        }

        private static long FindFirstFreeDataPageOffsetStart(FileStream fs, BinaryReader reader, long currentPosition)
        {
            long startOfFreeDataPageOffset = currentPosition;

            fs.Seek(currentPosition + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);

            long pointer = reader.ReadInt64();
            while (pointer != DataPageManager.DefaultBufferForDp && pointer != 0)
            {
                startOfFreeDataPageOffset = pointer;
                fs.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                pointer = reader.ReadInt64();
            }

            return startOfFreeDataPageOffset == DataPageManager.DefaultBufferForDp
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

        private static (FileStream, BinaryReader) OpenFileAndReader()
        {
            FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryReader reader = new(fileStream, Encoding.UTF8);
            return (fileStream, reader);
        }

        private static void CloseStreamAndReader(FileStream stream, BinaryReader reader)
        {
            stream.Close();
            reader.Close();
        }
    }
}