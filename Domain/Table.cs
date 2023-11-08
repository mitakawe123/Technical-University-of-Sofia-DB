using DataStructures;
using System.Data.Common;

namespace Domain
{
    [Serializable]
    public class Table
    {
        public string Name { get; set; } = string.Empty;
        public DKList<Column> Columns { get; set; } = new();
        public DKList<Row> Rows { get; set; } = new();
        public Dictionary<int, int> DataPageOffsets { get; set; } = new();
    }
}