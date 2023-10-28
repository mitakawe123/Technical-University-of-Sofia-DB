namespace DMS.DataPages
{
    //when I make this class static I need to catch the case when user delete data page lets say so i can reset the static fields
    public static class DataPageManager
    {
        //GAM -> IAM -> data pages
        private const string DB_FOLDER = "/STORAGE";
        private const string DB_IAM_FOLDER = "/STORAGE/IAM";
        private const string DB_DATA_FOLDER = "/STORAGE/DATA_PAGES";

        private static int FileNumber = 0;

        //There are three types of data pages in SQL Server: in-row, row-overflow, and LOB data pages.
        private const int PageSize = 8192; //8KB
        private const int HeaderSize = 96;
        private const int RowOffset = 36;
        private const int BufferOverflowPage = 24; // this is when you try to write data in data page but there is not enough space so you leave 24kb that will hold address to the next data page
       
        //this will hold the address of every row (one slot item needs to take 2Bytes of data for each row // 2bytes * row address)
        private static byte[] _slotArray = new byte[RowOffset];

        public static long AvailableSpace { get; private set; }
        public static byte[] HeaderData { get; private set; }
        public static byte[] Data { get; private set; }

        static DataPageManager()
        {
            //this check automatically for the STORAGE Folder
            Directory.CreateDirectory(DB_FOLDER);
            Directory.CreateDirectory(DB_DATA_FOLDER);
            Directory.CreateDirectory(DB_IAM_FOLDER);

            AvailableSpace = PageSize - HeaderSize - RowOffset;
            HeaderData = new byte[HeaderSize];
            Data = new byte[AvailableSpace];
        }

        //This method will create empty metadata file that will contains info how the table will look
        public static void CreateTable(string[] columnNames, string[] columnTypes, string tableName)
        {
            //here i need to check if for the given table there is IAM file created 
            //if yes just add the addresses of the new data pages
            //if no create the IAM file and then create the data pages and add the addresses of the data pages to the IAM file

            if (Directory.Exists($"{DB_DATA_FOLDER}/{tableName}"))
                throw new Exception("Already a table with this name");

            Directory.CreateDirectory($"{DB_DATA_FOLDER}/{tableName}");

            string metadataFilePath = $"{DB_DATA_FOLDER}/{tableName}/metadata.bin";
            using FileStream metadataStream = File.Open(metadataFilePath, FileMode.CreateNew);
            using BinaryWriter metadataWriter = new(metadataStream);

            // Write number of columns.
            metadataWriter.Write(columnNames.Length);
            metadataWriter.Write("\n");

            // Write column details.
            for (int i = 0; i < columnNames.Length; i++)
            {
                metadataWriter.Write(columnNames[i]);
                metadataWriter.Write(columnTypes[i]);
            }

            metadataStream.Close();
            metadataWriter.Close();
        }

        //This method will fill data inside the tables if they exist
        public static void InsertIntoTable()
        {
            //The page should have a header that contains:
            //Total number of records
            //Pointer / reference to the next page(could be the next filename)
        }

        //this is for test purpose
        private static (string[], string[]) DeserializeMetadata(string tableName)
        {
            string metadataFilePath = $"{DB_DATA_FOLDER}/{tableName}/metadata.bin";

            if (!File.Exists(metadataFilePath))
                throw new Exception("Metadata file does not exist for the specified table.");

            using FileStream metadataStream = File.OpenRead(metadataFilePath);
            using BinaryReader metadataReader = new(metadataStream);

            // Read number of columns.
            int numColumns = metadataReader.ReadInt32();
            metadataReader.ReadString();  // Read the newline.

            string[] columnNames = new string[numColumns];
            string[] columnTypes = new string[numColumns];

            // Read column details.
            for (int i = 0; i < numColumns; i++)
            {
                columnNames[i] = metadataReader.ReadString();
                columnTypes[i] = metadataReader.ReadString();
            }

            return (columnNames, columnTypes);
        }
    }
}
