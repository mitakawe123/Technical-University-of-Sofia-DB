using DMS.Constants;
using DMS.Extensions;
using DMS.Utils;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DMS.DataPages
{
    // We will store only string,int and date so we want only in-row and row-overflow data pages
    public class DataPageManager
    {
        //There are three types of data pages in SQL Server: in-row, row-overflow, and LOB data pages.
        private const int PageSize = 8192; //8KB
        private const int HeaderSize = 96;
        private const int RowOffset = 36;
        private const int BufferOverflowPage = 16; // this is when you try to write data in data page but there is not enough space so you leave 24kb that will hold address to the next data page

        static DataPageManager()
        {
            Directory.CreateDirectory(Folders.DB_FOLDER);
            Directory.CreateDirectory(Folders.DB_DATA_FOLDER);
            Directory.CreateDirectory(Folders.DB_IAM_FOLDER);
        }

        //createtable test(id int primary key, name string null)
        public static void CreateTable(IReadOnlyList<string> columnNames, IReadOnlyList<string> columnTypes, ReadOnlySpan<char> tableName)
        {
            //in the mdf file at the start write the offset mapping



            /*if (Directory.Exists($"{Folders.DB_DATA_FOLDER}/{tableName}"))
                throw new Exception("Already a table with this name");

            Directory.CreateDirectory($"{Folders.DB_DATA_FOLDER}/{tableName}");
            Directory.CreateDirectory($"{Folders.DB_IAM_FOLDER}/{tableName}");

            string iamFilePath = $"{Folders.DB_IAM_FOLDER}/{tableName}/iam_{1}.bin";
            using FileStream iamdataStream = File.Open(iamFilePath, FileMode.CreateNew);

            iamdataStream.Close();

            string metadataFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/{Files.METADATA_NAME}";
            using FileStream metadataStream = File.Open(metadataFilePath, FileMode.CreateNew);
            using BinaryWriter metadataWriter = new(metadataStream);

            // Write number of columns.
            metadataWriter.Write(columnNames.Count);
            metadataWriter.Write("\n");

            // Write column types.
            for (int i = 0; i < columnNames.Count; i++)
            {
                metadataWriter.Write(columnNames[i]);
                metadataWriter.Write(columnTypes[i]);
            }

            metadataStream.Close();
            metadataWriter.Close();*/
        }

        //Insert INTO test (Id, Name) VALUES (1, “pepi”, 3), (2, “mariq”, 6), (3, “georgi”, 1)
        public static void InsertIntoTable(IReadOnlyList<string> columnValues, ReadOnlySpan<char> tableName)
        {
            //add check if column values can be cast to columnDefinitions
            string[] columnTypes = DeserializeMetadata(tableName.ToString()).Item2;
            string[] filesInDir = Directory.GetFiles($"{Folders.DB_DATA_FOLDER}/{tableName}");
            
            ulong[] allocatedSpaceForColumnTypes = HelperAllocater.AllocatedStorageForType(columnTypes, columnValues);
            ulong allAlocatedSpaceForOneRecord = HelperAllocater.AllocatedSpaceForColumnTypes(allocatedSpaceForColumnTypes);
            
            int remainingSpace = PageSize - ((int)allAlocatedSpaceForOneRecord * columnValues.Count);
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

            if (allAlocatedSpaceForOneRecord * (ulong)columnValues.CustomCount() + (ulong)fileSize > PageSize)
            {
                string dataPagesFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/datapage_{pageNumber}.bin";
                using FileStream dataPageStream = File.Open(dataPagesFilePath, FileMode.Append);
                using BinaryWriter dataPageWriter = new(dataPageStream);
                
                ulong tempFileSize = (ulong)fileSize;
                int currentIndexOfColumValues = 0;

                while (allAlocatedSpaceForOneRecord + tempFileSize + BufferOverflowPage < PageSize)
                {
                    foreach (string col in columnValues)
                    {
                        string[] values = col.CustomSplit(',');
                        foreach (string value in values)
                            dataPageWriter.Write(value.CustomTrim());

                        currentIndexOfColumValues++;
                    }
                    tempFileSize += allAlocatedSpaceForOneRecord;
                }

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

        public static (string[], string[]) DeserializeMetadata(string tableName)
        {
            string metadataFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/{Files.METADATA_NAME}";

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
