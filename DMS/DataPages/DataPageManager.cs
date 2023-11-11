using DataStructures;
using DMS.Constants;
using DMS.Shared;
using DMS.Utils;
using System.Text;
using static DMS.Utils.ControlTypes;

namespace DMS.DataPages
{
    public class DataPageManager
    {
        private const int DataPageSize = 8192; //8KB
        private const int HeaderSectionSize = 8; // 4 bytes for data page number and 4 bytes for offset

        private static int DataPageNumberInMDFFile = 0;
        private static int SizeOfOffsetRecords = 0;

        private static Dictionary<char[], DKList<int>> tableOffsets = new();

        private static bool isclosing = false;

        static DataPageManager()
        {
            UpdateHeaderSection();

            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);
        }

        //createtable test1(id int primary key, name string(max) null, name1 string(max) null)
        public static void CreateTable(IReadOnlyList<Column> columns, ReadOnlySpan<char> tableName)
        {
            char[] table = tableName.ToArray();
            if (tableOffsets.ContainsKey(table))
                throw new Exception($"Table {table} already exists");

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
            using BinaryWriter writer = new(binaryStream);

            ulong totalSpaceForColumnTypes = HelperAllocater.AllocatedStorageForTypes(columns);
            int numberOfPagesNeeded = (int)Math.Ceiling((double)totalSpaceForColumnTypes / DataPageSize);

            int currentPage = DataPageNumberInMDFFile;
            int columnIndex = 0;
            int freeSpace = DataPageSize;
            bool firstDataPageForTable = true;

            while (numberOfPagesNeeded > 0)
            {
                binaryStream.Seek((currentPage * DataPageSize) + HeaderSectionSize, SeekOrigin.Begin);

                // Write table name and column count on the first page only
                if (firstDataPageForTable)
                {
                    writer.Write(tableName);
                    writer.Write(columns.Count);
                    firstDataPageForTable = false;
                    tableOffsets.Add(table, new DKList<int>() { currentPage });
                }

                // Write as many columns as fit on the current page
                while (columnIndex < columns.Count &&
                    (freeSpace - 4) > CalculateColumnSize(columns[columnIndex]))
                {
                    writer.Write(columns[columnIndex].Type);
                    writer.Write(columns[columnIndex].Name);
                    freeSpace -= CalculateColumnSize(columns[columnIndex]);
                    columnIndex++;
                }

                // If there are more columns to write, store a reference to the next page
                if (columnIndex < columns.Count)
                {
                    // Last 4 bytes of each page store the next page number
                    binaryStream.Seek(((currentPage + 1) * DataPageSize - 4) + HeaderSectionSize, SeekOrigin.Begin);
                    writer.Write(currentPage + 1); // Next page number
                    freeSpace = DataPageSize;
                    tableOffsets[table].Add(currentPage + 1);
                }

                currentPage++;
                numberOfPagesNeeded--;
            }

            DataPageNumberInMDFFile = currentPage;
            binaryStream.Close();
            writer.Close();

            //Write the offset for the data pages for the given table
            WriteOffsetMapper(tableOffsets.Last());

            //this is maybe for the insert
            /*if (totalSpaceForColumnTypes > DataPageSize)
            {
                int numberOfPagesNeeded = (int)Math.Ceiling((double)totalSpaceForColumnTypes / DataPageSize);

                for (int page = 1; page <= numberOfPagesNeeded; page++)
                {
                    // Determine the space left in the current page
                    ulong spaceLeftInPage = page == 1 ? DataPageSize - (ulong)binaryStream.Position : DataPageSize;

                    // Write data to the current page up to its limit
                    WriteDataToPage(tableName, binaryStream, columns, ref spaceLeftInPage);
                }

                DataPageNumberInMDFFile += numberOfPagesNeeded;
                return;
            }*/
        }

        public static bool DropTable(ReadOnlySpan<char> tableName)
        {
            char[] tableNameAsString = tableName.ToArray();
            if (!tableOffsets.ContainsKey(tableNameAsString))
                return false;

            FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryWriter writer = new(binaryStream);

            DKList<int> pageNumbers = tableOffsets[tableNameAsString];

            for (int i = 0; i < pageNumbers.Count; i++)
            {
                binaryStream.Seek((pageNumbers[i] * DataPageSize) + HeaderSectionSize, SeekOrigin.Begin);
                writer.Write(new byte[DataPageSize]);
            }

            tableOffsets.Remove(tableNameAsString);

            //here I need to update the offset file also
            DeleteOffsetMapperByKey(tableName);

            return true;
        }

        public static void ListTables()
        {
            foreach (char[] table in tableOffsets.Keys)
                Console.WriteLine(table);
        }

        private static int CalculateColumnSize(Column column)
        {
            int typeSize = (sizeof(char) * column.Type.Length); // 2 bytes per char
            int nameSize = (sizeof(char) * column.Name.Length); // 2 bytes per char
            int columnSize = (int)HelperAllocater.CalculateColumnSize(column);

            return typeSize + nameSize + columnSize;
        }
        //createtable test(id int primary key, name string(max) null, name1 string(max) null)
        private static void WriteOffsetMapper(KeyValuePair<char[], DKList<int>> entry)
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
            using BinaryWriter writer = new(binaryStream, Encoding.UTF8);

            // Writing the string and its count
            writer.Write(entry.Key.Length);
            SizeOfOffsetRecords += sizeof(int); // 4 bytes for int

            writer.Write(entry.Key);
            SizeOfOffsetRecords += entry.Key.Length; // 1 bytes for each char

            writer.Write(entry.Value.Count);
            SizeOfOffsetRecords += sizeof(int); // 4 bytes for int

            foreach (int value in entry.Value)
            {
                writer.Write(value);
                SizeOfOffsetRecords += sizeof(int); // 4 bytes for each int
            }
        }

        private static Dictionary<char[], DKList<int>> ReadOffsetMapper()
        {
            Dictionary<char[], DKList<int>> result = new();

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream, Encoding.UTF8);

            binaryStream.Seek(-SizeOfOffsetRecords, SeekOrigin.End);

            while (binaryStream.Position < binaryStream.Length)
            {
                int tableNameLength = reader.ReadInt32();
                byte[] tableNameAsBytes = reader.ReadBytes(tableNameLength);
                char[] tableName = Encoding.UTF8.GetString(tableNameAsBytes).ToArray();

                int listCount = reader.ReadInt32();
                DKList<int> list = new(listCount);

                for (int i = 0; i < listCount; i++)
                    list.Add(reader.ReadInt32());

                result.Add(tableName, list);
            }

            return result;
        }

        private static void DeleteOffsetMapperByKey(ReadOnlySpan<char> tableName)
        {
            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryWriter writer = new(fs, Encoding.UTF8);
            //think of a way to add a offset for the offset mapper maybe
            char[] tableNameArray = tableName.ToArray();
            bool entryFound = false;

            foreach (char[] key in tableOffsets.Keys)
            {
                if (key.SequenceEqual(tableNameArray))
                {
                    //emtpy the space in the offset section and delete data pages

                    tableOffsets.Remove(key);
                    entryFound = true;
                    break;
                }
            }

            if (!entryFound)
                Console.WriteLine("Table name not found in offset mapper.");
        }

        private static ulong FreeSpaceInDataPage(int pageNumber)
        {
            byte[] buffer = new byte[DataPageSize];

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream);

            binaryStream.Seek(pageNumber * DataPageSize, SeekOrigin.Begin);

            ulong bytesRead = (ulong)reader.Read(buffer, 0, DataPageSize);

            return DataPageSize - bytesRead;
        }

        private static void UpdateHeaderSection()
        {
            try
            {
                using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);
                using BinaryReader reader = new(fs);
                fs.Seek(0, SeekOrigin.Begin);

                DataPageNumberInMDFFile = reader.ReadInt32();
                SizeOfOffsetRecords = reader.ReadInt32();
            }
            catch (Exception)
            {
                using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.OpenOrCreate);
                using BinaryWriter writer = new(fs);
                fs.Seek(0, SeekOrigin.Begin);

                writer.Write(DataPageNumberInMDFFile);
                writer.Write(SizeOfOffsetRecords);
            }

            tableOffsets = ReadOffsetMapper();
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    isclosing = true;
                    ConsoleEventCallback();
                    Console.WriteLine("CTRL+C received!");
                    break;

                case CtrlTypes.CTRL_BREAK_EVENT:
                    isclosing = true;
                    ConsoleEventCallback();
                    Console.WriteLine("CTRL+BREAK received!");
                    break;

                case CtrlTypes.CTRL_CLOSE_EVENT:
                    isclosing = true;
                    ConsoleEventCallback();
                    break;

                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    isclosing = true;
                    ConsoleEventCallback();
                    Console.WriteLine("User is logging off!");
                    break;
            }
            return true;
        }

        private static void ConsoleEventCallback()
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.ReadWrite);
            using BinaryWriter writer = new(binaryStream);

            binaryStream.Seek(0, SeekOrigin.Begin);

            writer.Write(DataPageNumberInMDFFile);
            writer.Write(SizeOfOffsetRecords);
        }

        //Insert INTO test (Id, Name) VALUES (1, “pepi”, 3), (2, “mariq”, 6), (3, “georgi”, 1)
        /*        public static void InsertIntoTable(IReadOnlyList<string> columnValues, ReadOnlySpan<char> tableName)
                {
                    //add check if column values can be cast to columnDefinitions
                    string[] columnTypes = DeserializeMetadata(tableName.ToString()).Item2;
                    string[] filesInDir = Directory.GetFiles($"{Folders.DB_DATA_FOLDER}/{tableName}");

                    ulong[] allocatedSpaceForColumnTypes = HelperAllocater.AllocatedStorageForType(columnTypes, columnValues);
                    ulong allAlocatedSpaceForOneRecord = HelperAllocater.AllocatedSpaceForColumnTypes(allocatedSpaceForColumnTypes);

                    int remainingSpace = DataPageSize - ((int)allAlocatedSpaceForOneRecord * columnValues.Count);
                    int pageNumber = 1;
                    int iamPageNumber = 1;

                    //this is the first insertion to the current table
                    if (filesInDir.Length == 1)
                    {
                        //need to assing IAM logical address
                        string dataPageFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/datapage_{pageNumber}.bin";
                        using FileStream dataPageStream = File.Open(dataPageFilePath, FileMode.CreateNew);
                        using BinaryWriter dataPageWriter = new(dataPageStream);

                        //this is the header section
                        dataPageWriter.Write(remainingSpace);// 1 - 4 byte
                        dataPageWriter.Write(columnValues.Count);// 5 - 8 byte

                        dataPageWriter.Seek(0, SeekOrigin.End);

                        //this is the value section
                        foreach (string col in columnValues)
                        {
                            string[] values = col.CustomSplit(',');
                            //here i need to add a check if this is nvarchar
                            foreach (string value in values)
                                dataPageWriter.Write(value.CustomTrim());//2bytes per char
                        }

                        //fill the reset with zeros so that the data page is 8KB
                        dataPageWriter.Write(new byte[remainingSpace]);

                        dataPageStream.Close();
                        dataPageWriter.Close();

                        //need to think about how to connect data pages and IAM file and catch the case when IAM file is over 4GB to extend it to new IAM file
                        //need to add the bplustree here for managing the data pages
                        string iamPageFilePath = $"{Folders.DB_IAM_FOLDER}/{tableName}/iam_{iamPageNumber}.bin";
                        using FileStream iamPageStream = File.Open(iamPageFilePath, FileMode.OpenOrCreate);
                        using BinaryWriter iamPageWriter = new(iamPageStream);

                        iamPageWriter.Seek(0, SeekOrigin.End);
                        iamPageWriter.Write(pageNumber);

                        iamPageStream.Close();
                        iamPageWriter.Close();

                        return;
                    }

                    string lastFileNameInDir = filesInDir[^1];
                    FileInfo fileInfo = new(lastFileNameInDir);

                    int underScoreIndex = lastFileNameInDir.CustomLastIndexOf('_');
                    int dotIndex = lastFileNameInDir.CustomLastIndexOf('.');

                    long fileSize = fileInfo.Length;

                    pageNumber = int.Parse(lastFileNameInDir[(underScoreIndex + 1)..dotIndex]);

                    if (allAlocatedSpaceForOneRecord * (ulong)columnValues.CustomCount() + (ulong)fileSize > DataPageSize)
                    {
                        string dataPagesFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/datapage_{pageNumber}.bin";
                        using FileStream dataPageStream = File.Open(dataPagesFilePath, FileMode.Append);
                        using BinaryWriter dataPageWriter = new(dataPageStream);

                        ulong tempFileSize = (ulong)fileSize;
                        int currentIndexOfColumValues = 0;

                        *//*while (allAlocatedSpaceForOneRecord + tempFileSize + BufferOverflowPage < PageSize)
                        {
                            foreach (string col in columnValues)
                            {
                                string[] values = col.CustomSplit(',');
                                foreach (string value in values)
                                    dataPageWriter.Write(value.CustomTrim());

                                currentIndexOfColumValues++;
                            }
                            tempFileSize += allAlocatedSpaceForOneRecord;
                        }*//*

                        dataPageStream.Close();
                        dataPageWriter.Close();

                        pageNumber++;
                        string nextDataPagesFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/datapage_{pageNumber}.bin";
                        using FileStream nextDataPageStream = File.Open(nextDataPagesFilePath, FileMode.CreateNew);
                        using BinaryWriter nextDataPageWriter = new(nextDataPageStream);

                        IReadOnlyList<string> leftRecords = columnValues.CustomSkip(currentIndexOfColumValues);

                        //this is the header section
                        nextDataPageWriter.Write(remainingSpace);
                        nextDataPageWriter.Write(leftRecords.Count);

                        nextDataPageWriter.Close();
                        nextDataPageWriter.Close();

                        string iamPageFilePath = $"{Folders.DB_IAM_FOLDER}/{tableName}/iam_{iamPageNumber}.bin";
                        using FileStream iamPageStream = File.Open(iamPageFilePath, FileMode.Append);
                        using BinaryWriter iamPageWriter = new(iamPageStream);

                        iamPageWriter.Write(pageNumber);

                        iamPageStream.Close();
                        iamPageWriter.Close();
                    }
                    else
                    {
                        //need to update Record count and Remaining space

                        //just append the info to the current data page wihtout creating a new data page
                        string dataPagesFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/datapage_{pageNumber}.bin";
                        using FileStream dataPageStream = File.Open(dataPagesFilePath, FileMode.Append);
                        using BinaryWriter dataPageWriter = new(dataPageStream);

                        //Marshal


                        //this is the value section
                        foreach (string col in columnValues)
                        {
                            string[] values = col.CustomSplit(',');
                            foreach (string value in values)
                                dataPageWriter.Write(value.CustomTrim());
                        }

                        dataPageStream.Close();
                        dataPageWriter.Close();

                        //FindAndReplaceRecord(dataPagesFilePath, "Record count:", )
                    }
                }
        */
    }
}
