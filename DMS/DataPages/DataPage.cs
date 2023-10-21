using System.Text;

namespace DMS.DataPages
{
    public class DataPage
    {
        //There are three types of data pages in SQL Server: in-row, row-overflow, and LOB data pages.
        private const int PageSize = 8192; //8KB
        private const int HeaderSize = 96; //need to check for headers init kb size

        private readonly byte[] _data;

        public byte[] Data => _data;

        public DataPage() => _data = new byte[PageSize];

        private void InitializeHeader()
        {
            byte[] header = Encoding.ASCII.GetBytes("PAGE HEADER");
            //Buffer.BlockCopy(header, 0, Data, 0, HeaderSize);
            //Add more information to the header as needed, such as metadata.
        }

        public void WriteData(int offset, string content)
        {
            byte[] dataBytes = Encoding.ASCII.GetBytes(content);
            if (offset + dataBytes.Length <= PageSize - HeaderSize)
            {
                //Buffer.BlockCopy(dataBytes, 0, Data, HeaderSize + offset, dataBytes.Length);
            }
            else
            {
                //Here i need to make page splitting so that the new data goes to a new data page and not overflow
            }
        }

        public string ReadData(int offset, int length)
        {
            if (offset + length > PageSize - HeaderSize)
                return "";

            byte[] dataBytes = new byte[length];
            Buffer.BlockCopy(Data, HeaderSize + offset, dataBytes, 0, length);
            return Encoding.ASCII.GetString(dataBytes);
        }
    }
}
