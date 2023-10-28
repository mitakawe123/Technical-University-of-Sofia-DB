using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Constants
{
    internal class SqlDataTypesSizes
    {
        private static readonly Dictionary<SqlServerDataTypes, int> DataTypeSizes = new()
        {
            { SqlServerDataTypes.INT, 4 },
            { SqlServerDataTypes.SMALLINT, 2 },
            { SqlServerDataTypes.TINYINT, 1 },
            { SqlServerDataTypes.BIGINT, 8 },
            { SqlServerDataTypes.DECIMAL, 17 },
            { SqlServerDataTypes.NUMERIC, 17 },
            { SqlServerDataTypes.FLOAT, 8 },
            { SqlServerDataTypes.REAL, 4 },
            { SqlServerDataTypes.CHAR, -1 },
            { SqlServerDataTypes.VARCHAR, -1 },
            { SqlServerDataTypes.NCHAR, -1 },
            { SqlServerDataTypes.NVARCHAR, -1 },
            { SqlServerDataTypes.TEXT, -1 },
            { SqlServerDataTypes.DATE, 3 },
            { SqlServerDataTypes.TIME, 5 },
            { SqlServerDataTypes.DATETIME, 8 },
            { SqlServerDataTypes.SMALLDATETIME, 4 },
            { SqlServerDataTypes.DATETIME2, 8 },
            { SqlServerDataTypes.TIMESTAMP, 8 },
            { SqlServerDataTypes.BIT, 1 },
            { SqlServerDataTypes.BINARY, -1 },
            { SqlServerDataTypes.VARBINARY, -1 },
            { SqlServerDataTypes.IMAGE, -1 },
            { SqlServerDataTypes.UNIQUEIDENTIFIER, 16 },
            { SqlServerDataTypes.XML, -1 },
            { SqlServerDataTypes.CURSOR, -1 },
            { SqlServerDataTypes.ROWVERSION, 8 },
            { SqlServerDataTypes.GEOMETRY, -1 },
            { SqlServerDataTypes.GEOGRAPHY, -1 },
            { SqlServerDataTypes.SQL_VARIANT, -1 },
            { SqlServerDataTypes.MONEY, 8 },
            { SqlServerDataTypes.TABLE, -1 },
            { SqlServerDataTypes.HIERARCHYID, -1 }
        };
        public static int GetByteSize(SqlServerDataTypes dataType) => DataTypeSizes.TryGetValue(dataType, out int size) ? size : -1;
    }
}
