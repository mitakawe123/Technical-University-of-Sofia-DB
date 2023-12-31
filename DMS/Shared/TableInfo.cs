using DataStructures;

namespace DMS.Shared;

public ref struct TableInfo
{
    public string TableName;
    public int NumberOfDataPages;
    public int ColumnCount;
    public DKList<string> ColumnTypes;
    public DKList<string> ColumnNames;
    public DKList<string> DefaultValues;
}