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
            command = command.CustomToLower().CustomTrim();

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

                case ECliCommands.Delete:
                    DeleteFromTable(command);
                    break;
            }
        }

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
                if (typeSpaceIndex != -1)
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

            SqlCommands.InsertIntoTable(valuesList, tableNameSpan);
        }

        private static void SelectFromTable(string command)
        {
            ReadOnlySpan<char> commandSpan = command;

            int startAfterKeyword = commandSpan.CustomIndexOf("select") + "select".Length;
            int startFrom = commandSpan.CustomIndexOf("from");

            ReadOnlySpan<char> values = commandSpan[startAfterKeyword..startFrom].CustomTrim();
            ReadOnlySpan<char> tableSpan = commandSpan[(startFrom + "from".Length)..].CustomTrim();

            int endOfTableName = tableSpan.IndexOf(' ');
            if (endOfTableName == -1)
                endOfTableName = tableSpan.Length; // If no space, the table name goes till the end

            ReadOnlySpan<char> tableName = tableSpan[..endOfTableName].CustomTrim();

            bool isThereLogicalOperator = endOfTableName < tableSpan.Length;
            ReadOnlySpan<char> logicalOperator = ReadOnlySpan<char>.Empty;

            if (isThereLogicalOperator)
                logicalOperator = tableSpan[(tableName.CustomIndexOf(tableName) + tableName.Length + 1)..];

            DKList<string> columnValues = new();
            if (!values.CustomContains(','))
                columnValues.Add(values.ToString());
            else
            {
                int start = 0;
                while (start < values.Length)
                {
                    int commaIndex = values[start..].CustomIndexOf(',');
                    int end = commaIndex == -1 ? values.Length : start + commaIndex;

                    ReadOnlySpan<char> column = values.CustomSlice(start, end - start).CustomTrim();
                    columnValues.Add(column.ToString());

                    if (commaIndex == -1)
                        break;

                    start = end + 1;
                }
            }

            SqlCommands.SelectFromTable(columnValues, tableName, logicalOperator);
        }
        
        private static void DeleteFromTable(string command)
        {
            ReadOnlySpan<char> commandSpan = command;

            int startFrom = commandSpan.CustomIndexOf("from");
            int whereIndex = commandSpan.CustomIndexOf("where");

            ReadOnlySpan<char> tableSpan = commandSpan[(startFrom + "from".Length)..whereIndex].CustomTrim();
            ReadOnlySpan<char> whereCondition = commandSpan[(whereIndex + "where".Length)..].CustomTrim();

            string[] conditions = whereCondition.ToString().Split(new[] { "and" }, StringSplitOptions.RemoveEmptyEntries);

            DKList<string> whereConditions = new();
            DKList<string> columnNames = new();

            foreach (string condition in conditions)
            {
                string[] columns = condition.Split(new[] { "=", ">", "<" }, StringSplitOptions.RemoveEmptyEntries);

                columns[0] = columns[0].CustomTrim();

                columnNames.Add(columns[0]);
                whereConditions.Add(condition.CustomTrim());
            }

            SqlCommands.DeleteFromTable(tableSpan, whereConditions, columnNames);
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
