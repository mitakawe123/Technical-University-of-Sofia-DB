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

            DKList<Column> columns = new();

            while (valuesSpan.Length > 0)
            {
                /*int bracketIndex = valuesSpan.CustomIndexOf(')');
                ReadOnlySpan<char> vals = bracketIndex != -1
                    ? valuesSpan[..bracketIndex]
                    : valuesSpan;

                int openingBracket = vals.CustomIndexOf('(');

                columns.Add(vals[(openingBracket + 1)..].ToString());

                valuesSpan = bracketIndex != -1
                    ? valuesSpan[(bracketIndex + 1)..].CustomTrim()
                    : ReadOnlySpan<char>.Empty;*/
            }

            SQLCommands.InsertIntoTable(columns, tableNameSpan);
        }
    }
}
