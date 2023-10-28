using System.Runtime.InteropServices;

namespace DMS.DataPages
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DataPageHeader
    {
        public int PageNumber;  // Page number
        public int NextPage;    // Pointer to the next data page (for chaining)
        public int RecordCount; // Number of records in the page
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Record
    {
        public List<object> Columns = new();
    }

    public class DataPage
    {
        public DataPageHeader Header;
        public List<Record> Records = new();
    }
}
