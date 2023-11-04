using DMS.Constants;
using DMS.Extensions;
using DMS.Utils;

namespace DMS.DataPages
{
    // We will store only string,int and date so we want only in-row and row-overflow data pages
    public class DataPageManager
    {
        //There are three types of data pages in SQL Server: in-row, row-overflow, and LOB data pages.
        private const int PageSize = 8192; //8KB
        private const int HeaderSize = 96;
        private const int RowOffset = 36;
        private const int BufferOverflowPage = 24; // this is when you try to write data in data page but there is not enough space so you leave 24kb that will hold address to the next data page

        static DataPageManager()
        {
            Directory.CreateDirectory(Folders.DB_FOLDER);
            Directory.CreateDirectory(Folders.DB_DATA_FOLDER);
            Directory.CreateDirectory(Folders.DB_IAM_FOLDER);
        }

        //createtable test(id int primary key, name nvarchar(50) null)
        public static void CreateTable(string[] columnNames, string[] columnTypes, string tableName)
        {
            if (Directory.Exists($"{Folders.DB_DATA_FOLDER}/{tableName}"))
                throw new Exception("Already a table with this name");

            Directory.CreateDirectory($"{Folders.DB_DATA_FOLDER}/{tableName}");
            Directory.CreateDirectory($"{Folders.DB_IAM_FOLDER}/{tableName}");

            string iamFilePath = $"{Folders.DB_IAM_FOLDER}/{tableName}/iam_{tableName}.bin";
            using FileStream iamdataStream = File.Open(iamFilePath, FileMode.CreateNew);

            iamdataStream.Close();

            string metadataFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/metadata.bin";
            using FileStream metadataStream = File.Open(metadataFilePath, FileMode.CreateNew);
            using BinaryWriter metadataWriter = new(metadataStream);

            // Write number of columns.
            metadataWriter.Write(columnNames.Length);
            metadataWriter.Write("\n");

            // Write column types.
            for (int i = 0; i < columnNames.Length; i++)
            {
                metadataWriter.Write(columnNames[i]);
                metadataWriter.Write(columnTypes[i]);
            }

            metadataStream.Close();
            metadataWriter.Close();
        }

        //Insert INTO test (Id, Name) VALUES (1, “pepi”, 3), (2, “mariq”, 6), (3, “georgi”, 1)
        public static void InsertIntoTable(string[] columnDefinitions, string tableName, IEnumerable<string> columnValuesSplitted)
        {
            //add check if column values can be cast to columnDefinitions
            string[] columnTypes = DeserializeMetadata(tableName).Item2;
            ulong[] allocatedSpaceForColumnTypes = HelperAllocater.AllocatedStorageForType(columnTypes, columnValuesSplitted);
            ulong allAlocatedSpaceForOneRecord = 0;
            foreach (ulong item in allocatedSpaceForColumnTypes)
                allAlocatedSpaceForOneRecord += item;

            string[] fileNames = Directory.GetFiles($"{Folders.DB_DATA_FOLDER}/{tableName}");
            string lastFileNameInDir = fileNames[^1];

            FileInfo fileInfo = new(lastFileNameInDir);
            //this is the first insertion to the current table
            if (lastFileNameInDir.CustomContains("metadata.bin"))
            {
                //need to assing IAM logical address
                string dataPageFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/{tableName}_datapage_{1}.bin";
                using FileStream dataPageStream = File.Open(dataPageFilePath, FileMode.CreateNew);
                using BinaryWriter dataPageWriter = new(dataPageStream);

                //this is the header section
                dataPageWriter.Write("Identifier:" + Guid.Empty + "|");
                dataPageWriter.Write("Record count:" + columnValuesSplitted.CustomCount() + "|");
                dataPageWriter.Write("Remaining space:" + (PageSize - (allAlocatedSpaceForOneRecord * (ulong)columnValuesSplitted.CustomCount())) + "|");
                dataPageWriter.Write('\n');

                //this is the value section
                foreach (string col in columnValuesSplitted)
                {
                    string[] values = col.CustomSplit(',');
                    foreach (string value in values)
                        dataPageWriter.Write(value.CustomTrim());
                    dataPageWriter.Write('\n');
                }

                dataPageWriter.Write("Pointer next page:" + Guid.Empty + "|");
                dataPageStream.Close();
                dataPageWriter.Close();

                //need to think about how to connect data pages and IAM file and catch the case when IAM file is over 4GB to extend it to new IAM file
                //need to add the bplustree here for managing the data pages
                string iamPageFilePath = $"{Folders.DB_IAM_FOLDER}/{tableName}/iam_{tableName}_{1}.bin";
                using FileStream iamPageStream = File.Open(iamPageFilePath, FileMode.CreateNew);
                using BinaryWriter iamPageWriter = new(dataPageStream);
            }
            else
            {
                int underScoreIndex = lastFileNameInDir.CustomLastIndexOf('_');
                int dotIndex = lastFileNameInDir.CustomLastIndexOf('.');
                int pageNumber = int.Parse(lastFileNameInDir[(underScoreIndex + 1)..dotIndex]);
                long fileSize = fileInfo.Length;
                if (allAlocatedSpaceForOneRecord * (ulong)columnValuesSplitted.CustomCount() + (ulong)fileSize > PageSize)
                {
                    //here if the space is not enough in the data page I need to add as much as possible to the current data page 
                    //make a logical address to the new data page 
                    //create the new data page
                    //assign logical address to the IAM file
                    //need to find where is the "Pointer next page:" and change Guid.Empty with something else
                    string dataPagesFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/{tableName}_datapage_{pageNumber}.bin";
                    using FileStream dataPageStream = File.Open(dataPagesFilePath, FileMode.Append);
                    using BinaryWriter dataPageWriter = new(dataPageStream);
                    ulong tempFileSize = (ulong)fileSize;
                    int currentIndexOfColumValues = 0;

                    while (allAlocatedSpaceForOneRecord + tempFileSize + BufferOverflowPage < PageSize)
                    {
                        foreach (string col in columnValuesSplitted)
                        {
                            string[] values = col.CustomSplit(',');
                            foreach (string value in values)
                                dataPageWriter.Write(value.CustomTrim());

                            dataPageWriter.Write('\n');
                            currentIndexOfColumValues++;
                        }
                        tempFileSize += allAlocatedSpaceForOneRecord;
                    }

                    dataPageStream.Close();
                    dataPageWriter.Close();

                    pageNumber++;
                    string nextDataPagesFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/{tableName}_datapage_{pageNumber}.bin";
                    using FileStream nextDataPageStream = File.Open(dataPagesFilePath, FileMode.CreateNew);
                    using BinaryWriter nextDataPageWriter = new(dataPageStream);

                    IEnumerable<string> leftRecords = columnValuesSplitted.CustomSkip(currentIndexOfColumValues);

                    //this is the header section
                    Guid pageIndentifier = Guid.NewGuid(); //<- add this to the prev page pointer next page 
                    nextDataPageWriter.Write("Identifier:" + pageIndentifier + "|");
                    nextDataPageWriter.Write("Record count:" + leftRecords.CustomCount() + "|");
                    nextDataPageWriter.Write("Remaining space:" + (PageSize - (allAlocatedSpaceForOneRecord * (ulong)leftRecords.CustomCount())) + "|");
                    nextDataPageWriter.Write('\n');

                    nextDataPageWriter.Write("Pointer next page:" + Guid.Empty + "|");
                    nextDataPageWriter.Close();
                    nextDataPageWriter.Close();
                }
                else
                {
                    //just append the info to the current data page wihtout creating a new data page
                    string dataPagesFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/{tableName}_datapage_{pageNumber}.bin";
                    using FileStream dataPageStream = File.Open(dataPagesFilePath, FileMode.Append);
                    using BinaryWriter dataPageWriter = new(dataPageStream);

                    //this is the header section
                    dataPageWriter.Write("Record count:" + columnValuesSplitted.CustomCount() + "|");
                    dataPageWriter.Write("Remaining space:" + (PageSize - (allAlocatedSpaceForOneRecord * (ulong)columnValuesSplitted.CustomCount())) + "|");
                    dataPageWriter.Write("Pointer next page:0" + "|");
                    dataPageWriter.Write('\n');

                    //this is the value section
                    foreach (string col in columnValuesSplitted)
                    {
                        string[] values = col.CustomSplit(',');
                        foreach (string value in values)
                            dataPageWriter.Write(value.CustomTrim());
                        dataPageWriter.Write('\n');
                    }

                    dataPageStream.Close();
                    dataPageWriter.Close();
                }
            }
        }

        public static (string[], string[]) DeserializeMetadata(string tableName)
        {
            string metadataFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/metadata.bin";

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

            return (columnNames, columnTypes);
        }

        private static bool FindAndReplaceBytes(string filePath, string searchString)
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
            ulong newRemainingSpace = 100000;
            using BinaryWriter bw = new(fs);
            bw.Write(newRemainingSpace.ToString());
            return true;
        }
    }
}
