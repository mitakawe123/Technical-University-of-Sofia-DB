using DMS.Constants;
using DMS.Extensions;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace DMS.DataPages
{
    //when i make this class static i need to catch the case when user delete data page lets say so i can reset the static fields
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

        //createtable test(id int primary key, name nvarchar(50) null)
        public static void CreateTable(string[] columnNames, string[] columnTypes, string tableName)
        {
            //there is no alter table so this case is out of the way

            //here i need to check if for the given table there is IAM file created 
            //if yes just add the addresses of the new data pages
            //if no create the IAM file and then create the data pages and add the addresses of the data pages to the IAM file

            //create metadata.txt for every table

            //The page should have a header that contains:
            //Total number of records
            //Pointer / reference to the next page(could be the next filename)
            if (Directory.Exists($"{DB_DATA_FOLDER}/{tableName}"))
                throw new Exception("Already a table with this name");

            Directory.CreateDirectory($"{DB_DATA_FOLDER}/{tableName}");

            string metadataFilePath = $"{DB_DATA_FOLDER}/{tableName}/metadata.dat";
            using FileStream metadataStream = File.Open(metadataFilePath, FileMode.CreateNew);
            using BinaryWriter metadataWriter = new(metadataStream);

            // Write number of columns.
            metadataWriter.Write(columnNames.Length);

            // Write column details.
            for (int i = 0; i < columnNames.Length; i++)
            {
                metadataWriter.Write(columnNames[i]);
                metadataWriter.Write(columnTypes[i]);
            }

            // Create initial data file.
            string dataFilePath = $"{DB_DATA_FOLDER}/{tableName}/file_{tableName}.dat";
            using FileStream dataStream = File.Open(dataFilePath, FileMode.CreateNew);
            using BinaryWriter dataWriter = new(dataStream);

            /*string filePath = $"{DB_DATA_FOLDER}/{tableName}/file_{tableName}.dat";

            using FileStream fileStream = File.Open(filePath, FileMode.CreateNew);
            FileInfo fileInfo = new(filePath);
            if (!fileInfo.Exists)
                fileStream.SetLength(PageSize);

            BinaryWriter binaryWriter = new(fileStream);*/
        }

        private static void InitializePageHeaders()
        {

        }
    }
}
