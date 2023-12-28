using DataStructures;

namespace DMS.Shared
{
    public ref struct SelectQueryParams
    {
        public IReadOnlyList<char[]> AllData;
        public DKList<string> ValuesToSelect;
        public IReadOnlyList<Column> ColumnTypeAndName;
        public ReadOnlySpan<char> LogicalOperator;
        public int ColumnCount;
    }
}
