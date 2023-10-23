using DMS.Constants;
using System.Text;

namespace DMS.DataPages
{
    //when i make this class static i need to catch the case when user delete data page lets say so i can reset the static fields
    public static class DataPage
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

        static DataPage()
        {
            //this check automatically for the STORAGE Folder
            Directory.CreateDirectory(DB_FOLDER);
            Directory.CreateDirectory(DB_DATA_FOLDER);
            Directory.CreateDirectory(DB_IAM_FOLDER);

            AvailableSpace = PageSize - HeaderSize - RowOffset;
            HeaderData = new byte[HeaderSize];
            Data = new byte[AvailableSpace];
        }

        public static void WriteData(byte[] data)
        {
            //here i need to check if for the given table there is IAM file created 
            //if yes just add the addresses of the new data pages
            //if no create the IAM file and then create the data pages and add the addresses of the data pages to the IAM file

            //need to check if file exist and then append or create
            string filePath = $"{DB_DATA_FOLDER}/file_{FileNumber}.dat";

            using FileStream fileStream = File.Open(filePath, FileMode.Append);
            FileInfo fileInfo = new(filePath);
            if (!File.Exists(filePath))
                fileStream.SetLength(PageSize);
            

            if (data.Length > AvailableSpace)
            {
                FileNumber = 0;
                AvailableSpace = PageSize - HeaderSize - RowOffset;
                IAMPages iAMPages = new();
                //create new data page so i can page split the info
                FileNumber++;
            }
            else
            {
                AvailableSpace -= data.Length;
                
                using BinaryWriter binaryWriter = new(fileStream);

                binaryWriter.Write(Encoding.UTF8.GetBytes($"{DataPageConstants.PAGE_HEADER}: {FileNumber}\n"));
                binaryWriter.Write(Encoding.UTF8.GetBytes($"m_freeCnt = {AvailableSpace}\n"));
                binaryWriter.Write(Encoding.UTF8.GetBytes($"{DataPageConstants.DATA}:\n"));
                binaryWriter.Write($"{data}\n");
                binaryWriter.Write(Encoding.UTF8.GetBytes($"{DataPageConstants.ROW_OFFSET}\n"));
            }
        }

        private static void InitializePageHeaders()
        {

        }
    }
}
