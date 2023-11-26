﻿using DataStructures;
using DMS.Constants;
using DMS.Extensions;

namespace DMS.Commands
{
    public static class CommandValidator
    {
        private static readonly DKList<char> InvalidTableNameCharacters = new();
        private static readonly DKList<string> SqlDataTypes = new();
        private static DKDictionary<ECliCommands, Func<string, bool>> validationActions;

        static CommandValidator()
        {
            foreach (var keyword in Enum.GetValues<EInvalidTableNameCharacters>())
                InvalidTableNameCharacters.Add((char)keyword);

            foreach (var keyword in Enum.GetValues<EDataTypes>())
                SqlDataTypes.Add(keyword.ToString().CustomToLower());

            validationActions = new DKDictionary<ECliCommands, Func<string, bool>>
            {
                { ECliCommands.CreateTable, ValidateCreateTableCommand },
                { ECliCommands.DropTable, ValidateDropTableAndTableInfoCommands },
                { ECliCommands.ListTables, command => true },
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
            if (validationActions.TryGetValue(commandType, out Func<string, bool> validateAction))
            {
                bool isValid = validateAction(command);
                if (!isValid)
                    Console.WriteLine($"Please enter a valid {commandType.ToString().ToLower()} command!");
                
                return isValid;
            }

            Console.WriteLine("Invalid command, please enter a valid command");
            return false;
        }

        private static bool ValidateCreateTableCommand(string command)
        {
            string[] commandSpliced = command.Split(' ');
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

            string tableName = command[(firstWhiteSpace + 1)..openingBracket];

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

            ReadOnlySpan<char> vals = commandSpan.CustomSlice(insetIntoText.Length + 1,
                commandSpan.Length - insetIntoText.Length - 1);
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
                            ReadOnlySpan<char> segment = valuesPart.Slice(start, end - start + 1).Trim();
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
            //maybe it will be good idea to get the here to check if I can parse the values to the correct type that are defined in the data page

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

        private static bool ValidateDropIndex(string arg)
        {
            return true;
        }

        private static bool ValidateCreateIndex(string arg)
        {
            return true;
        }
    }
}
