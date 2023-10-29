namespace DMS.Constants
{
    public enum ECliCommands
    {
        Help,
        CreateTable,
        DropTable,
        ListTables,
        TableInfo,
        Exit
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

    public enum SqlServerDataTypes
    {
        INT,
        SMALLINT,
        TINYINT,
        BIGINT,
        DECIMAL,
        NUMERIC,
        FLOAT,
        REAL,
        CHAR,
        VARCHAR,
        NCHAR,
        NVARCHAR,
        TEXT,
        DATE,
        TIME,
        DATETIME,
        SMALLDATETIME,
        DATETIME2,
        TIMESTAMP,
        BIT,
        BINARY,
        VARBINARY,
        IMAGE,
        UNIQUEIDENTIFIER,
        XML,
        CURSOR,
        ROWVERSION,
        GEOMETRY,
        GEOGRAPHY,
        SQL_VARIANT,
        MONEY,
        TABLE,
        HIERARCHYID,
    }

    public enum EDataPagesExtensions
    {
        MDF,
        NDF
    }

    public enum InvalidTableNameCharacters
    {
        Underscore = '_',
        Space = ' ',
        Hyphen = '-',
        Period = '.',
        Slash = '/',
        Backslash = '\\',
        QuestionMark = '?',
        ExclamationMark = '!',
        AtSign = '@',
        PoundSign = '#',
        DollarSign = '$',
        PercentSign = '%',
        Caret = '^',
        Ampersand = '&',
        Asterisk = '*',
        LeftParenthesis = '(',
        RightParenthesis = ')',
        LeftBrace = '{',
        RightBrace = '}',
        LeftBracket = '[',
        RightBracket = ']',
        LessThan = '<',
        GreaterThan = '>',
        Comma = ',',
        Semicolon = ';',
        SingleQuote = '\'',
        DoubleQuote = '\"',
        Colon = ':',
        Pipe = '|',
        Plus = '+',
        Equal = '=',
        Tilde = '~',
        GraveAccent = '`'
    }
}
