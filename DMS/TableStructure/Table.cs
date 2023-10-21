using DataStructures;
using DMS.Extensions;

namespace DMS.TableStructure
{
    public class Table
    {
        private DKList<TableRow> rows = new();

        public DKList<string> Columns { get; }

        public Table(params string[] columns) => Columns = columns.CustomToList();

        public void InsertRow(params object[] values)
        {

        }
    }
}
