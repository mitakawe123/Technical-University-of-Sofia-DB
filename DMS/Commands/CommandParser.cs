using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;

namespace DMS.Commands
{
    public static class CommandParser
    {
        public static void Parse(ECliCommands commandType, string command)
        {
            command = command.CustomToLower();

            bool isValidQuery = CommandValidator.ValidateQuery(commandType, command);

            if (!isValidQuery)
                return;

            switch (commandType)
            {
                case ECliCommands.CreateTable:
                    CreateTable(command);
                    break;

                case ECliCommands.DropTable:
                    DropTable(command);
                    break;

                case ECliCommands.ListTables:
                    ListTables();
                    break;

                case ECliCommands.TableInfo:
                    TableInfo(command);
                    break;

                case ECliCommands.Insert:
                    InsertIntoTable(command);
                    break;

                case ECliCommands.Select:
                    SelectFromTable(command);
                    break;
            }
        }
        //createtable test(id int primary key, name string(50) null)
        private static void CreateTable(string command)
        {
            //add a case when there is default values
            ReadOnlySpan<char> commandSpan = command;
            int startAfterKeyword = ECliCommands.CreateTable.ToString().Length;
            int openingBracket = commandSpan.CustomIndexOf('(');
            int closingBracket = commandSpan.CustomLastIndexOf(')');
            int endBeforeParenthesis = commandSpan[startAfterKeyword..].CustomIndexOf('(');

            ReadOnlySpan<char> tableNameSpan = commandSpan.CustomSlice(startAfterKeyword, endBeforeParenthesis).CustomTrim();
            ReadOnlySpan<char> values = commandSpan[(openingBracket + 1)..closingBracket];
            DKList<Column> columns = new();

            while (values.Length > 0)
            {
                int commaIndex = values.CustomIndexOf(',');
                ReadOnlySpan<char> columnDefinition = commaIndex != -1 ? values[..commaIndex] : values;

                int spaceIndex = columnDefinition.CustomIndexOf(' ');

                ReadOnlySpan<char> columnName = columnDefinition[..spaceIndex].CustomTrim();
                ReadOnlySpan<char> columnType = columnDefinition[(spaceIndex + 1)..].CustomTrim();

                int typeSpaceIndex = columnType.CustomIndexOf(' ');
                columnType = columnType[..typeSpaceIndex];

                columns.Add(new Column(new string(columnName), new string(columnType)));

                values = commaIndex != -1 ? values[(commaIndex + 1)..].CustomTrim() : ReadOnlySpan<char>.Empty;
            }

            if (columns.CustomAny(x => x.Name.Length > 128))
                throw new Exception("Column name is too long");

            DataPageManager.CreateTable(columns, tableNameSpan);
        }

        private static void DropTable(string command)
        {
            ReadOnlySpan<char> commandSpan = command;
            int startAfterKeyword = ECliCommands.DropTable.ToString().Length;
            ReadOnlySpan<char> tableNameSpan = commandSpan[startAfterKeyword..].CustomTrim();

            bool isTableDeleted = DataPageManager.DropTable(tableNameSpan);
            if (isTableDeleted)
                Console.WriteLine($"Table {tableNameSpan} was deleted successfully");
            else
                Console.WriteLine($"Table {tableNameSpan} was not deleted successfully");
        }

        private static void ListTables() => DataPageManager.ListTables();

        private static void TableInfo(string command)
        {
            command = command.CustomTrim();
            string tableName = command[(ECliCommands.TableInfo.ToString().Length + 1)..];
            ReadOnlySpan<char> table = tableName;
            DataPageManager.TableInfo(table);
        }

        //Insert INTO test (Id, Name) VALUES (1, “pepi”), (2, “mariq”), (3, “georgi”)
        private static void InsertIntoTable(string command)
        {
            ReadOnlySpan<char> commandSpan = command;
            int startAfterKeyword = commandSpan.CustomIndexOf("insert into") + "insert into".Length;
            int valuesKeyword = commandSpan.CustomIndexOf("values") + "values".Length;

            int endBeforeParenthesis = commandSpan[startAfterKeyword..].CustomIndexOf('(');

            ReadOnlySpan<char> tableNameSpan = commandSpan.CustomSlice(startAfterKeyword, endBeforeParenthesis).CustomTrim();
            ReadOnlySpan<char> valuesSpan = commandSpan[valuesKeyword..].CustomTrim();

            DKList<DKList<char[]>> valuesList = new();
            bool inQuotes = false;
            int start = 0;
            for (int i = 0; i < valuesSpan.Length; i++)
            {
                if (valuesSpan[i] == '"' && (i == 0 || valuesSpan[i - 1] != '\\'))
                    inQuotes = !inQuotes;

                if (!inQuotes && valuesSpan[i] == ')')
                {
                    ReadOnlySpan<char> tupleSpan = valuesSpan[start..i].CustomTrim();
                    valuesList.Add(ProcessTuple(tupleSpan));
                    start = i + 1;
                }
                else if (!inQuotes && valuesSpan[i] == '(')
                    start = i + 1;
            }

            SQLCommands.InsertIntoTable(valuesList, tableNameSpan);
        }

        //select * from test where id = 1 
        private static void SelectFromTable(string command)
        {
            ReadOnlySpan<char> commandSpan = command;

            // Find the indices of the keywords
            int startAfterKeyword = commandSpan.CustomIndexOf("select") + "select".Length;
            int startFrom = commandSpan.CustomIndexOf("from");

            ReadOnlySpan<char> values = commandSpan[startAfterKeyword..startFrom].CustomTrim();
            ReadOnlySpan<char> tableSpan = commandSpan[(startFrom + "from".Length)..].CustomTrim();

            int endOfTableName = tableSpan.IndexOf(' ');
            if (endOfTableName == -1)
                endOfTableName = tableSpan.Length; // If no space, the table name goes till the end

            ReadOnlySpan<char> tableName = tableSpan[..endOfTableName].CustomTrim();

            SQLCommands.SelectFromTable(values, tableName);
        }

        private static DKList<char[]> ProcessTuple(ReadOnlySpan<char> tuple)
        {
            DKList<char[]> values = new();
            bool inQuotes = false;
            int start = 0;

            for (int i = 0; i < tuple.Length; i++)
            {
                if (tuple[i] == '"' && (i == 0 || tuple[i - 1] != '\\'))
                    inQuotes = !inQuotes;

                if (!inQuotes && tuple[i] == ',')
                {
                    ReadOnlySpan<char> valueSpan = tuple[start..i].CustomTrim();
                    values.Add(ProcessValue(valueSpan));
                    start = i + 1;
                }
            }

            if (start < tuple.Length)
            {
                ReadOnlySpan<char> valueSpan = tuple[start..].CustomTrim();
                values.Add(ProcessValue(valueSpan));
            }

            return values;
        }

        private static char[] ProcessValue(ReadOnlySpan<char> value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];

            return value.ToString().Replace("\\\"", "").CustomToCharArray();
        }
    }
}
