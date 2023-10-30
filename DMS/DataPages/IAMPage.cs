using DataStructures;

namespace DMS.DataPages
{
    public class IAMPage
    {
        public int PageID { get; set; }
        public DKList<DataPage> Page { get; set; }
    }
}
