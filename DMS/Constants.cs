using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS
{
    public enum ECliCommands
    {
        Help,
        CreateTable,
        DropTable,
        ListTables,
        TableInfo
    }

    public enum ESQLCommands
    {
        SELECT,
        WHERE,
        JOIN,
        OR,
        NOT,
        AND,
        ORDER_BY,
        DISTINCT,
        DELETE,
        INSERT
    }
}
