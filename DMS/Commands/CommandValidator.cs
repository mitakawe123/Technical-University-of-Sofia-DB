using DataStructures;
using DMS.Constants;
using DMS.Extensions;

namespace DMS.Commands
{
    public static class CommandValidator
    {
        private static readonly DKList<char> InvalidTableNameCharacters = new();
        private static readonly DKList<string> SqlDataTypes = new();
        private static readonly DKDictionary<ECliCommands, Func<string, bool>> ValidationActions;

        static CommandValidator()
        {
            foreach (var keyword in Enum.GetValues<EInvalidTableNameCharacters>())
                InvalidTableNameCharacters.Add((char)keyword);

            foreach (var keyword in Enum.GetValues<EDataTypes>())
                SqlDataTypes.Add(keyword.ToString().CustomToLower());

            ValidationActions = new DKDictionary<ECliCommands, Func<string, bool>>
            {
                { ECliCommands.CreateTable, ValidateCreateTableCommand },
                { ECliCommands.DropTable, ValidateDropTableAndTableInfoCommands },
                { ECliCommands.ListTables, _ => true },
                { ECliCommands.TableInfo, ValidateTableInfoCommand },
                { ECliCommands.Insert, ValidateInsertTableCommand },
                { ECliCommands.Select, ValidateSelectFromTable },
                { ECliCommands.Delete, ValidateDeleteFromTable },
                { ECliCommands.CreateIndex, ValidateCreateIndex },
                { ECliCommands.DropIndex, ValidateDropIndex }
            };
        }

        public static bool ValidateQuery(ECliCommands commandType, string command)
        {
            if (ValidationActions.TryGetValue(commandType, out Func<string, bool> validateAction))
            {
                bool isValid = validateAction(command);
                if (!isValid)
                    Console.WriteLine($"Please enter a valid {commandType.ToString().CustomToLower()} command!");

                return isValid;
            }

            Console.WriteLine("Invalid command, please enter a valid command");
            return false;
        }

        private static bool ValidateCreateTableCommand(string command)
        {
            string[] commandSpliced = command.CustomSplit(' ');
            if (commandSpliced[0] != ECliCommands.CreateTable.ToString().CustomToLower())
                return false;

            int firstWhiteSpace = command.CustomIndexOf(' ');
            int openingBracket = command.CustomIndexOf('(');
            int closingBracketForColumns = command.CustomLastIndexOf(')');

            if (openingBracket is -1 || closingBracketForColumns is -1)
            {
                Console.WriteLine("Add closing and opening brackets before and after the table name");
                return false;
            }

            string tableName = command[(firstWhiteSpace + 1)..openingBracket].CustomTrim();

            if (tableName.Length > 128)
            {
                Console.WriteLine("Table name length is too long");
                return false;
            }

            if (tableName.CustomAny(x => InvalidTableNameCharacters.CustomContains(x)) ||
                SqlDataTypes.CustomAny(x => tableName == x))
            {
                Console.WriteLine("Invalid table name");
                return false;
            }

            string columnDefinition = command[(openingBracket + 1)..closingBracketForColumns].CustomTrim();
            string[] columnDefinitions = columnDefinition.CustomSplit(',');

            foreach (string columnDef in columnDefinitions)
            {
                string itemTrimmed = columnDef.CustomTrim();

                int firstWhiteSpaceAfterColumnName = itemTrimmed.CustomIndexOf(' ');
                string columnName = itemTrimmed[..firstWhiteSpaceAfterColumnName].CustomTrim();
                if (SqlDataTypes.CustomAny(x => columnName == x))
                {
                    Console.WriteLine("Invalid column name");
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateDropTableAndTableInfoCommands(string command)
        {
            command = command.CustomTrim();
            string[] parts = command.CustomSplit(' ');

            return parts.Length is 2;
        }

        private static bool ValidateInsertTableCommand(string command)
        {
            ReadOnlySpan<char> commandSpan = command;
            ReadOnlySpan<char> insetIntoText = "insert into";
            ReadOnlySpan<char> valuesTest = "values";

            if (!commandSpan.CustomContains(insetIntoText, StringComparison.OrdinalIgnoreCase)
                || !commandSpan.CustomContains(valuesTest, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Not a valid insert into command");

            int firstBracket = commandSpan.CustomIndexOf('(');

            ReadOnlySpan<char> vals = commandSpan.CustomSlice(insetIntoText.Length + 1, commandSpan.Length - insetIntoText.Length - 1);
            ReadOnlySpan<char> tableName = vals[..firstBracket].CustomTrim();

            if (tableName.Length > 128)
                throw new Exception("Table name is too long");

            if (tableName.IsEmpty)
                throw new Exception("Table name is empty");

            int valuesKeyWordIndex = vals.CustomIndexOf(valuesTest);
            ReadOnlySpan<char> valuesPart = vals[(valuesKeyWordIndex + valuesTest.Length + 1)..];

            int start = 0;
            while (start < valuesPart.Length)
            {
                int end = start;
                int bracketCount = 0;
                bool inQuote = false;

                while (end < valuesPart.Length)
                {
                    if (valuesPart[end] == '\'' && (end == 0 || valuesPart[end - 1] != '\\'))
                        inQuote = !inQuote;

                    if (!inQuote)
                    {
                        if (valuesPart[end] == '(')
                            bracketCount++;
                        else if (valuesPart[end] == ')')
                            bracketCount--;

                        if (bracketCount == 0 && (valuesPart[end] == ',' || end == valuesPart.Length - 1))
                        {
                            ReadOnlySpan<char> segment = valuesPart.CustomSlice(start, end - start + 1).CustomTrim();
                            if (!segment.CustomContains('(')
                                || !segment.CustomContains(')'))
                                throw new Exception(
                                    "Invalid value format. Each value must be enclosed in parentheses.");

                            start = end + 1;
                            break;
                        }
                    }

                    end++;
                }

                if (bracketCount != 0 || inQuote)
                {
                    Console.WriteLine("Unbalanced parentheses or quotes in values.");
                    return false;
                }
            }
            //maybe it will be good idea to check if I can parse the values to the correct type that are defined in the data page

            return true;
        }

        private static bool ValidateTableInfoCommand(string command)
        {
            command = command.CustomTrim();
            string[] parts = command.CustomSplit(' ');

            return parts.Length == 2;
        }

        private static bool ValidateSelectFromTable(string command)
        {
            if (command.CustomIsNullOrEmpty())
                return false;

            string[] parts = command.CustomSplit(' ');

            if (parts.Length < 4)
                return false;

            ReadOnlySpan<char> commandSpan = command;

            return commandSpan.CustomStartsWith("select") && commandSpan.CustomContains("from", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValidateDeleteFromTable(string command)
        {
            if (command.CustomIsNullOrEmpty())
                return false;

            string[] parts = command.CustomSplit(' ');

            return parts is ["delete", "from", _, _, ..] && command.CustomContains("where");
        }

        private static bool ValidateDropIndex(string command)
        {
            string dropIndex = ECliCommands.DropIndex.ToString().CustomToLower();
            if (!command.StartsWith(dropIndex, StringComparison.OrdinalIgnoreCase))
                return false;

            string remainingCommand = command[dropIndex.Length..].TrimStart();

            string[] parts = remainingCommand.CustomSplit(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
                return false;

            return parts[1] == "on";
        }

        private static bool ValidateCreateIndex(string command)
        {
            string createIndex = ECliCommands.CreateIndex.ToString().CustomToLower();
            if (!command.StartsWith(createIndex, StringComparison.OrdinalIgnoreCase))//write start with extension method
                return false;

            string remainingCommand = command[createIndex.Length..].CustomTrim();

            string[] parts = remainingCommand.CustomSplit(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);//split extension with char too

            if (parts.Length < 4)
                return false;

            if (parts[1].Length > 128)
                return false;

            if (parts[1] != "on")
                return false;

            if (!remainingCommand.CustomContains('(') || !remainingCommand.CustomContains(')'))
                return false;

            int indexOfOpenBracket = remainingCommand.CustomIndexOf('(');
            int indexOfCloseBracket = remainingCommand.CustomIndexOf(')', indexOfOpenBracket);
            if (indexOfCloseBracket == -1 || indexOfCloseBracket < indexOfOpenBracket)
                return false;

            string columnsPart = remainingCommand.CustomSubstring(indexOfOpenBracket + 1, indexOfCloseBracket - indexOfOpenBracket - 1);

            string[] columns = columnsPart.CustomSplit(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return columns.Length != 0 && !columns.CustomAny(col => col.CustomIsNullOrEmpty());
        }
    }
}
