using System.Text;

namespace DMS.DataPages
{
    public class DataPage
    {
        //There are three types of data pages in SQL Server: in-row, row-overflow, and LOB data pages.
        private const int PageSize = 8192; //8KB
        private const int HeaderSize = 96; //need to check for headers init kb size

        public byte[] Data { get; private set; }
        public int PageNumber { get ; private set; }
        public int AvailableSpace { get; private set; }


        public DataPage(int pageNumber)
        {
            PageNumber = pageNumber;
            AvailableSpace = PageSize - sizeof(int) - sizeof(int);
            Data = new byte[AvailableSpace];
        }

        public void WriteData(byte[] data)
        {
            if (Data.Length > AvailableSpace)
                return;

            AvailableSpace -= Data.Length;
            Array.Copy(data, 0, Data, PageSize - AvailableSpace - data.Length, data.Length);
        }

        public void SaveToFile(string filePath)
        {
            using FileStream fileStream = new(filePath, FileMode.Append);
            using BinaryWriter writer = new(fileStream);
            writer.Write(PageNumber);
            writer.Write(AvailableSpace);
            writer.Write(Data);
        }

        public static DataPage LoadFromFile(string filePath)
        {
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
    }
}
