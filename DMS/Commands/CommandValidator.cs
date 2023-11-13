﻿using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;

namespace DMS.Commands
{
    public static class CommandValidator
    {
        private static readonly DKList<string> AllowedKeywords = new();
        private static readonly DKList<char> InvalidTableNameCharacters = new();
        private static readonly DKList<string> SqlDataTypes = new();

        static CommandValidator()
        {
            foreach (ESQLCommands keyword in Enum.GetValues<ESQLCommands>())
                AllowedKeywords.Add(keyword.ToString().CustomToLower());

            foreach (EInvalidTableNameCharacters keyword in Enum.GetValues<EInvalidTableNameCharacters>())
                InvalidTableNameCharacters.Add((char)keyword);

            foreach (EDataTypes keyword in Enum.GetValues<EDataTypes>())
                SqlDataTypes.Add(keyword.ToString().CustomToLower());
        }

        public static bool ValidateQuery(ECliCommands commandType, string command)
        {
            switch (commandType)
            {
                case ECliCommands.CreateTable:
                    bool isValidCreateTableCommand = ValidateCreateTableCommand(command);
                    if (isValidCreateTableCommand)
                        return true;

                    Console.WriteLine("Please enter valid create table command!");
                    return false;

                case ECliCommands.DropTable:
                    bool isValidDropTableCommand = ValidateDropTableAndTableInfoCommands(command);
                    if (isValidDropTableCommand)
                        return true;

                    Console.WriteLine("Please enter a valid drop table command!");
                    return false;

                case ECliCommands.ListTables:
                    return true;

                case ECliCommands.TableInfo:
                    bool isValidTableInfoCommand = ValidateTableInfoCommand(command);
                    if (isValidTableInfoCommand)
                        return true;

                    Console.WriteLine("Please enter a valid table info command!");
                    return true;

                case ECliCommands.Insert:
                    bool isValidInsertCommand = ValidateInsertTableCommand(command);
                    if (isValidInsertCommand)
                        return true;

                    Console.WriteLine("Please enter a insert command!");
                    return false;

                default:
                    Console.WriteLine("Invalid command please enter a valid command");
                    return false;
            }
        }

        //createtable test(id int primary key, name string null)
        private static bool ValidateCreateTableCommand(string command)
        {
            if (!command.CustomContains(ECliCommands.CreateTable.ToString()))
                return false;

            int firstWhiteSpace = command.CustomIndexOf(' ');
            int openingBracket = command.CustomIndexOf('(');
            int closingBracketForColumns = command.CustomLastIndexOf(')');

            if (openingBracket == -1 || closingBracketForColumns == -1)
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

            if (parts.Length != 2)
                return false;

            return true;
        }

        //Insert INTO test (Id, Name) VALUES (1, “pepi”), (2, “mariq”), (3, “georgi”)
        private static bool ValidateInsertTableCommand(string command)
        {
            if (!command.CustomToLower().CustomContains("INSERT INTO".CustomToLower())
                || !command.CustomToLower().CustomContains("VALUES".CustomToLower()))
                throw new Exception("Not a valid insert into command");

            string loweredCommand = command.CustomToLower();
            string[] parts = loweredCommand.CustomSplit(' ');
            string tableName = parts[2];

            string[] columnsAndValues = loweredCommand.CustomSplit($"{tableName.CustomToLower()}");
            string[] values = columnsAndValues[1].CustomSplit("values");

            if (!values[0].CustomContains('(') || !values[0].CustomContains(')'))
                throw new Exception("No brackets for column definitions");

            string columnDefinitions = values[0].CustomTrim();
            columnDefinitions = columnDefinitions.CustomSubstring(1, columnDefinitions.Length - 2);

            //will open the metadata file and check for the column defined there
            string[] splitedColumnDefinitions = columnDefinitions.CustomSplit(',');
            //IReadOnlyList<Column> deserializedMetadata = DataPageManager.DeserializeMetadata(tableName);

            /*for (int i = 0; i < deserializedMetadata.Item1.Length; i++)
            {
                if (deserializedMetadata.Item1[i].CustomToLower() != splitedColumnDefinitions[i].CustomTrim())
                    throw new Exception($"Invalid column {splitedColumnDefinitions[i]}");
            }*/

            return true;
        }

        private static bool ValidateTableInfoCommand(string command)
        {
            command = command.CustomTrim();
            string[] parts = command.CustomSplit(' ');

            if (parts.Length != 2)
                return false;

            return true;
        }
    }
}
