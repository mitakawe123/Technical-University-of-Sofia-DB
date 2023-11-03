using DataStructures;
using DMS.DataPages;

namespace DMS.IAM
{
    public class IAMPage
    {
        public int PageID { get; set; }
        public DKList<DataPage> Page { get; set; }
    }
}
