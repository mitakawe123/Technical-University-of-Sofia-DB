using System.Text;

namespace DMS.DataPages
{
    public class DataPage
    {
        //GAM -> IAM -> data pages
        private const string DB_FOLDER = "/STORAGE";

        //There are three types of data pages in SQL Server: in-row, row-overflow, and LOB data pages.
        private const int PageSize = 8192; //8KB
        private const int HeaderSize = 96;
        private const int RowOffset = 36;

        //this will hold the address of every row (one slot item needs to take 2Bytes of data for each row // 2bytes * row address)
        private byte[] _slotArray = new byte[RowOffset];

        public byte[] HeaderData { get; private set; }
        public byte[] Data { get; private set; }
        public int PageNumber { get; private set; }
        public int AvailableSpace { get; private set; }

        public DataPage(int pageNumber)
        {
            //this check automatically for the STORAGE Folder
            Directory.CreateDirectory(DB_FOLDER);

            PageNumber = pageNumber;
            AvailableSpace = PageSize - HeaderSize - RowOffset;
            HeaderData = new byte[HeaderSize];
            Data = new byte[AvailableSpace];
        }

        public void WriteData(byte[] data)
        {
            if (data.Length > AvailableSpace)
            {
                //create new data page so i can page split the info
            }
            else
            {

                AvailableSpace -= data.Length;
            }
        }

        public void SaveToFile(string filePath)
        {
            //test shit
            using FileStream fileStream = new(filePath, FileMode.Append);
            using BinaryWriter writer = new(fileStream);
            writer.Write(PageNumber);
            writer.Write(AvailableSpace);
            writer.Write(Data);
        }

        public static DataPage LoadFromFile(string filePath)
        {
            //test shit
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fileStream);
            int pageNumber = reader.ReadInt32();
            int availableSpace = reader.ReadInt32();
            byte[] data = reader.ReadBytes(PageSize - sizeof(int) - sizeof(int));

            DataPage dataPage = new(pageNumber)
            {
                AvailableSpace = availableSpace,
                Data = data
            };
            return dataPage;
        }

        private void InitializePageHeaders()
        {

        }
    }
}
