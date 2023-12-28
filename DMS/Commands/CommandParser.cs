using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Indexes;
using DMS.Shared;
using DMS.Utils;

namespace DMS.Commands
{
    public static class CommandParser
    {
        private static readonly DKDictionary<ECliCommands, Action<string>> CommandActions;

        static CommandParser()
        {
            CommandActions = new DKDictionary<ECliCommands, Action<string>>
            {
                { ECliCommands.CreateTable, CreateTable },
                { ECliCommands.DropTable, DropTable },
                { ECliCommands.ListTables, _ => ListTables() },
                { ECliCommands.TableInfo, TableInfo },
                { ECliCommands.Insert, InsertIntoTable },
                { ECliCommands.Select, SelectFromTable },
                { ECliCommands.Delete, DeleteFromTable },
                { ECliCommands.CreateIndex, CreateIndex },
                { ECliCommands.DropIndex, DropIndex }
            };
        }

        public static void Parse(ECliCommands commandType, string command)
        {
            command = command.CustomToLower().CustomTrim();

            bool isValidQuery = CommandValidator.ValidateQuery(commandType, command);
            if (!isValidQuery)
                return;

            if (CommandActions.TryGetValue(commandType, out Action<string> action))
                action(command);
        }

        private static void CreateTable(string command)
        {
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
                ReadOnlySpan<char> fullColumnType = columnDefinition[(spaceIndex + 1)..].CustomTrim();

                int defaultIndex = fullColumnType.CustomIndexOf(" default ");
                ReadOnlySpan<char> columnType = defaultIndex != -1 ? fullColumnType[..defaultIndex] : fullColumnType;

                string defaultValue = defaultIndex != -1 ? new string(fullColumnType[(defaultIndex + 9)..].CustomTrim()) : "-";

                bool isValidType = TypeValidation.CheckIfValidColumnType(columnType);
                if (!isValidType)
                {
                    Console.WriteLine($"Invalid type for column {columnName} with type {columnType}");
                    return;
                }

                int typeSpaceIndex = columnType.CustomIndexOf(' ');
                if (typeSpaceIndex is not -1)
                    columnType = columnType[..typeSpaceIndex];

                columns.Add(new Column(new string(columnName), new string(columnType), defaultValue));

                values = commaIndex is not -1 ? values[(commaIndex + 1)..].CustomTrim() : ReadOnlySpan<char>.Empty;
            }

            if (columns.CustomAny(x => x.Name.Length > 128))
            {
                Console.WriteLine("Column name is too long");
                return;
            }

            DataPageManager.CreateTable(columns, tableNameSpan);
        }

        private static void DropTable(string command)
        {
            ReadOnlySpan<char> commandSpan = command;
            int startAfterKeyword = ECliCommands.DropTable.ToString().Length;
            ReadOnlySpan<char> tableNameSpan = commandSpan[startAfterKeyword..].CustomTrim();

            bool isTableDeleted = DataPageManager.DropTable(tableNameSpan);
            Console.WriteLine(isTableDeleted
                ? $"Table {tableNameSpan} was deleted successfully"
                : $"Table {tableNameSpan} was not deleted successfully");
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
            int tableNameSpanEndIndex = endBeforeParenthesis + startAfterKeyword;

            ReadOnlySpan<char> betweenSpan = commandSpan.CustomSlice(tableNameSpanEndIndex + 1, (commandSpan.CustomIndexOf("values") - 3) - tableNameSpanEndIndex);
            ReadOnlySpan<char> selectedColumnsSpan = betweenSpan.CustomTrim();

            ReadOnlySpan<char> tableNameSpan = commandSpan.CustomSlice(startAfterKeyword, endBeforeParenthesis).CustomTrim();
            ReadOnlySpan<char> valuesSpan = commandSpan[valuesKeyword..].CustomTrim();

            DKList<char[]> selectedColumns = new();
            int lastComma = 0;
            for (int i = 0; i < selectedColumnsSpan.Length; i++)
            {
                if (selectedColumnsSpan[i] != ',')
                    continue;

                ReadOnlySpan<char> column = selectedColumnsSpan.Slice(lastComma, i - lastComma).CustomTrim();
                selectedColumns.Add(column.CustomToArray());
                lastComma = i + 1;
            }

            ReadOnlySpan<char> lastColumn = selectedColumnsSpan[lastComma..].CustomTrim();
            selectedColumns.Add(lastColumn.ToArray());

            DKList<DKList<char[]>> valuesList = new();
            bool inQuotes = false;
            int start = 0;
            for (int i = 0; i < valuesSpan.Length; i++)
            {
                if (valuesSpan[i] == '"' && (i == 0 || valuesSpan[i - 1] != '\\'))
                    inQuotes = !inQuotes;

                switch (inQuotes)
                {
                    case false when valuesSpan[i] == ')':
                    {
                        ReadOnlySpan<char> tupleSpan = valuesSpan[start..i].CustomTrim();
                        valuesList.Add(ProcessTuple(tupleSpan));
                        start = i + 1;
                        break;
                    }
                    case false when valuesSpan[i] == '(':
                        start = i + 1;
                        break;
                }
            }

            SqlCommands.InsertIntoTable(valuesList, selectedColumns, tableNameSpan);
        }

        private static void SelectFromTable(string command)
        {
            ReadOnlySpan<char> commandSpan = command;

            int startAfterKeyword = commandSpan.CustomIndexOf("select") + "select".Length;
            int startFrom = commandSpan.CustomIndexOf("from");

            ReadOnlySpan<char> values = commandSpan[startAfterKeyword..startFrom].CustomTrim();
            ReadOnlySpan<char> tableSpan = commandSpan[(startFrom + "from".Length)..].CustomTrim();

            int endOfTableName = tableSpan.CustomIndexOf(' ');
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

            string[] conditions = whereCondition.ToString().CustomSplit(new[] { "and" }, StringSplitOptions.RemoveEmptyEntries);

            DKList<string> whereConditions = new();
            DKList<string> columnNames = new();

            string[] separators = new[] { ">=", "<=", "!=", "=", ">", "<" };
            string[] parts = conditions[0].CustomSplit(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                if (Array.IndexOf(separators, parts[i].CustomToLower()) != -1 && i > 0)
                    columnNames.Add(parts[i - 1]);
            }

            whereConditions.Add(conditions[0].CustomTrim());

            SqlCommands.DeleteFromTable(tableSpan, whereConditions, columnNames);
        }

        private static void DropIndex(string command)
        {
            string dropIndexKeyword = "dropindex";
            ReadOnlySpan<char> commandSpan = command;

            int startKeywordIndex = commandSpan.CustomIndexOf(dropIndexKeyword) + dropIndexKeyword.Length;
            int onKeyword = commandSpan.CustomIndexOf("on");

            ReadOnlySpan<char> indexName = commandSpan[startKeywordIndex..onKeyword].CustomTrim();
            ReadOnlySpan<char> tableName = commandSpan[(onKeyword + 2)..].CustomTrim();

            IndexManager.DropIndex(tableName, indexName);
        }

        private static void CreateIndex(string command)
        {
            string createIndexKeyword = "createindex";
            ReadOnlySpan<char> commandSpan = command;

            int startKeywordIndex = commandSpan.CustomIndexOf(createIndexKeyword) + createIndexKeyword.Length;
            int onKeyword = commandSpan.CustomIndexOf("on");
            int firstOpeningBracket = commandSpan.CustomIndexOf('(');
            int firstClosingBracket = commandSpan.CustomIndexOf(')');

            ReadOnlySpan<char> indexName = commandSpan[startKeywordIndex..onKeyword].CustomTrim();
            ReadOnlySpan<char> tableName = commandSpan[(onKeyword + 2)..firstOpeningBracket].CustomTrim();
            ReadOnlySpan<char> columns = commandSpan[(firstOpeningBracket + 1)..firstClosingBracket];

            DKList<string> columnsSpliced = new();

            while (columns.Length > 0)
            {
                int commaIndex = columns.CustomIndexOf(',');
                if (commaIndex == -1)
                {
                    columnsSpliced.Add(columns.CustomTrim().ToString());
                    break;
                }

                columnsSpliced.Add(columns[..commaIndex].CustomTrim().ToString());
                columns = columns[(commaIndex + 1)..];
            }

            IndexManager.CreateIndex(columnsSpliced, tableName, indexName);
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

                if (inQuotes || tuple[i] != ',')
                    continue;

                ReadOnlySpan<char> valueSpan = tuple[start..i].CustomTrim();
                values.Add(ProcessValue(valueSpan));
                start = i + 1;
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
