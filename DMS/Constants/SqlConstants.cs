using System.Collections.Generic;
using System.Data;

namespace DMS.Constants
{
    public enum ECliCommands
    {
        Help,
        CreateTable,
        DropTable,
        ListTables,
        TableInfo,
        Insert,
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

    public enum ESqlServerDataTypes
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

    //('2023-11-03'); -- ISO format
    //('11/03/2023'); -- U.S.format
    //('03/11/2023'); -- European format
    //('20231103');   -- ISO basic (unseparated)
    public enum ESupportedDataTypes
    {
        DATE, //DATE: Stores a date in the format YYYY-MM-DD.
        INT, //4 bytes (32 bits)
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
}
