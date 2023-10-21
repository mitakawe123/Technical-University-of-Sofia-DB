namespace DMS.Constants
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
        INSERT,
        UPDATE,
        DROP, 
        ALTER, 
        BEGIN, 
        COMMIT, 
        ROLLBACK, 
        MERGE, 
        CALL, 
        DECLARE,
        EXECUTE
    }

    public enum EDataPagesExtensions
    {
        MDF,
        NDF
    }
}
