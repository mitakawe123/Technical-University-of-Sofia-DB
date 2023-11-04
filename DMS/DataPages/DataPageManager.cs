using DMS.Constants;
using DMS.Extensions;

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

        //this will hold the logical address of every row (one slot item needs to take 2Bytes of data for each row // 2bytes * row address)
        private static byte[] _slotArray = new byte[RowOffset];

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

        //Insert INTO test (Id, Name) VALUES (1, “Иван”) 
        public static void InsertIntoTable(string[] columnDefinitions, string[] columnValues, string tableName)
        {
            //The page should have a header that contains:
            //Total number of records
            //Pointer / reference to the next page
            //Check for the space of the column type 
            DataPage dataPage = new();
            string[] columnTypes = DeserializeMetadata(tableName).Item2;
            string[] fileNames = Directory.GetFiles($"{Folders.DB_DATA_FOLDER}/{tableName}");
            string lastFileNameInDir = fileNames[^1];
            int pageNumber;

            if (lastFileNameInDir.CustomContains("metadata.bin"))
            {
                pageNumber = 1;
            }
            else
            {
                FileInfo fileInfo = new(lastFileNameInDir);
                long fileSize = fileInfo.Length;
                //here i need to check how much space the new values will take and then increment the page number and create new data page or insert into the current data page
                /*if()
                {

                }*/
                //here i need to check first if the data page is full then add a logical address to the next data page and then increment the page number and create the new page
                int underScoreIndex = lastFileNameInDir.CustomLastIndexOf('_');
                int dotIndex = lastFileNameInDir.CustomLastIndexOf('.');
                pageNumber = int.Parse(lastFileNameInDir[(underScoreIndex+1)..dotIndex]);
                pageNumber++;
            }

            string dataPagesFilePath = $"{Folders.DB_DATA_FOLDER}/{tableName}/{tableName}_datapage_{pageNumber}.bin";
            using FileStream dataPageStream = File.Open(dataPagesFilePath, FileMode.CreateNew);
            using BinaryWriter dataPageWriter = new(dataPageStream);

            dataPageStream.SetLength(PageSize);


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
    }
}
