using DataStructures;
using System.Runtime.InteropServices;

namespace Domain
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DataPageHeader
    {
        public int PageNumber;  // Page number
        public int RecordCount; // Number of records in the page
    }

    public class DataPage
    {
        public DataPageHeader Header;
        public DKList<object> Records = new();
    }
}
