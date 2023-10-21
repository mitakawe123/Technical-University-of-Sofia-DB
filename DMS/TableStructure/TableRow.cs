using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.TableStructure
{
    public class TableRow
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool AllowNull { get; set; }
    }
}
