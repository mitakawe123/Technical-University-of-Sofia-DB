using DataStructures;

namespace Domain
{
    public class IAMPage
    {
        public int PageID { get; set; }
        public DKList<DataPage> Page { get; set; }
    }
}
