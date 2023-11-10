using DMS.Constants;
using DMS.Shared;
using DMS.Utils;
using System.Text;

namespace DMS.DataPages
{
    public class DataPageManager
    {
        private const int DataPageSize = 8192; //8KB
        private static int DataPageNumberInMDFFile = 0;
        private static Dictionary<string, long> tableOffsets = new();

        public static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
                using BinaryWriter writer = new(binaryStream);
                binaryStream.Seek(0, SeekOrigin.End);
                writer.Write(DataPageNumberInMDFFile);
                //how many data pages are there 
                //save it in the end of the file and delete this number when starting the program
            }
            return false;
        }

        public static void RemoveIntFromEndOfFile()
        {
            if (!File.Exists(Files.MDF_FILE_NAME))
                return;

            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.Write);
            using BinaryReader reader = new(fs);

            DataPageNumberInMDFFile = reader.ReadInt32();

            fs.SetLength(fs.Length - 4);
        }


        //createtable test(id int primary key, name string(max) null, name1 string(max) null)
        public static void CreateTable(IReadOnlyList<Column> columns, ReadOnlySpan<char> tableName)
        {
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
            using BinaryWriter writer = new(binaryStream);

            binaryStream.Seek(0, SeekOrigin.Begin);
            binaryStream.SetLength(DataPageSize);

            ulong totalSpaceForColumnTypes = HelperAllocater.AllocatedStorageForType(columns);
            /*if (totalSpaceForTypes > DataPageSize)
            {
                int numberOfPagesNeeded = (int)Math.Ceiling((double)totalSpaceForTypes / DataPageSize);
                
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

            writer.Write(tableName);
            writer.Write(columns.Count);

            for (int i = 0; i < columns.Count; i++)
            {
                writer.Write(columns[i].Type); // 2 bytes per char
                writer.Write(HelperAllocater.CalculateColumnSize(columns[i])); //8 bytes
                writer.Write(columns[i].Name); // 2 bytes per char
            }

            //I need to create a method that takes page number and calculate how much free space is there 

            DataPageNumberInMDFFile++;

            binaryStream.Close();
            writer.Close();

            WriteOffsetMapper(tableName);
            tableOffsets = ReadOffsetMapper();
        }

        private static void WriteOffsetMapper(ReadOnlySpan<char> tableName)
        {
            using var binaryStream = new FileStream(Files.MDF_FILE_NAME, FileMode.Append);
            using var writer = new BinaryWriter(binaryStream);

            int startOfOffsetMapper = DataPageNumberInMDFFile * DataPageSize + 1;
            binaryStream.Seek(startOfOffsetMapper, SeekOrigin.Begin);

            long remainingLength = binaryStream.Length - binaryStream.Position;

            writer.Write(tableName.Length);
            writer.Write(tableName);

            /* while (remainingLength > 0)
             {
                 remainingLength = binaryStream.Length - binaryStream.Position;
             }*/
        }

        private static Dictionary<string, long> ReadOffsetMapper()
        {
            Dictionary<string, long> tableOffsets = new();
            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(binaryStream);

            int startOfOffsetMapper = DataPageNumberInMDFFile * DataPageSize + 1;
            binaryStream.Seek(startOfOffsetMapper, SeekOrigin.Begin);

            int lengthOfTableName = reader.ReadInt32();
            byte[] tableNameAsBytes = reader.ReadBytes(lengthOfTableName);
            string tableName = Encoding.UTF8.GetString(tableNameAsBytes);

            return tableOffsets;
        }

        private static void WriteDataToPage(ReadOnlySpan<char> tableName, FileStream stream, IReadOnlyList<Column> columns, ref ulong spaceLeft)
        {
            using var writer = new BinaryWriter(stream);

            // Write the table name and column count only on the first page
            if (stream.Position == 0)
            {
                writer.Write(tableName);
                writer.Write(columns.Count);
            }

            foreach (Column column in columns)
            {
                ulong columnSize = HelperAllocater.CalculateColumnSize(column);
                if (spaceLeft >= columnSize)
                {
                    writer.Write(column.Type);
                    writer.Write(column.Name);
                    spaceLeft -= columnSize;
                }
                else
                    break;
            }
        }

        private static int FreeSpaceInDataPage(int pageNumber)
        {
            byte[] buffer = new byte[DataPageSize];
            int freeSpaceCount = 0;

            using FileStream binaryStream = new(Files.MDF_FILE_NAME, FileMode.Append);
            using BinaryReader reader = new(binaryStream);

            binaryStream.Seek((long)(pageNumber - 1) * DataPageSize, SeekOrigin.Begin);

            if (binaryStream.Length < binaryStream.Position + DataPageSize)
                throw new InvalidOperationException("Page number exceeds the file size.");

            int bytesRead = reader.Read(buffer, 0, DataPageSize);

            freeSpaceCount = buffer.Count(b => b == 0x00);

            return freeSpaceCount;
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
        public static IReadOnlyList<Column> DeserializeMetadata(string tableName)
        {
            return default;
            /*string metadataFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/{Files.METADATA_NAME}";

            if (!File.Exists(metadataFilePath))
                throw new Exception("Metadata file does not exist for the specified table.");

            using FileStream metadataStream = File.OpenRead(metadataFilePath);
            using BinaryReader metadataReader = new(metadataStream);

            // Read number of columns.
            int numColumns = metadataReader.ReadInt32();
            metadataReader.ReadString();  // Read the newline.

            string[] columnNames = new string[numColumns];
            string[] columnTypes = new string[numColumns];

            // Read column types.
            for (int i = 0; i < numColumns; i++)
            {
                columnNames[i] = metadataReader.ReadString();
                columnTypes[i] = metadataReader.ReadString();
            }

            return (columnNames, columnTypes);*/
        }

        private static long FindRecordPosition(string filePath, string searchString)
        {
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.ReadWrite);
            using BinaryReader br = new(fs);

            long position = 0;
            bool found = false;

            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                if (br.ReadByte() != searchString[0])
                    continue;

                for (int i = 1; i < searchString.Length; i++)
                {
                    if (br.ReadByte() != searchString[i])
                        break;
                    if (i == searchString.Length - 1)
                        found = true;
                }

                if (found)
                {
                    position = br.BaseStream.Position;
                    break;
                }
            }

            if (!found)
                return -1;

            return position;
        }

        private static bool FindAndReplaceRecord<T>(string filePath, string searchString, T valueToUpdate)
        {
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.ReadWrite);
            using BinaryReader br = new(fs);

            long position = 0;
            bool found = false;

            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                if (br.ReadByte() != searchString[0])
                    continue;

                for (int i = 1; i < searchString.Length; i++)
                {
                    if (br.ReadByte() != searchString[i])
                        break;
                    if (i == searchString.Length - 1)
                        found = true;
                }

                if (found)
                {
                    position = br.BaseStream.Position;
                    break;
                }
            }

            if (!found)
                return false;

            fs.Seek(position, SeekOrigin.Begin);
            T newRemainingSpace = valueToUpdate;
            using BinaryWriter bw = new(fs);
            bw.Write(newRemainingSpace.ToString());
            return true;
        }
    }
}
