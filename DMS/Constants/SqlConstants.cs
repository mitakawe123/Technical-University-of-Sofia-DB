namespace DMS.Constants
{
    public enum ECliCommands
    {
        Cls,
        Clear,
        Help,
        CreateTable,
        DropTable,
        ListTables,
        TableInfo,
        Insert,
        Select,
        Delete,
        CreateIndex,
        DropIndex,
        Exit,
        UI
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

    public enum EDataTypes
    {
        DATE,
        INT,
        STRING // 2^31-1 bytes (2 GB). <- nvarchar(MAX) / 4,000char can be store there
    }

    public enum EInvalidTableNameCharacters
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

    public enum EOperatorType
    {
        unknown,
        where, 
        and, 
        not, 
        or, 
        orderby, 
        distinct, 
        join
    }

    public enum StringCompare
    {
        IgnoreCaseSensitivity
    }
}
