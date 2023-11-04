using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;

namespace DMS.Commands
{
    public static class CommandValidator
    {
        private static readonly DKList<string> AllowedKeywords = new();
        private static readonly DKList<char> InvalidTableNameCharacters = new();
        private static readonly DKList<string> SqlDataTypes = new();
        private static readonly DKList<string> SupportedSqlDataTypes = new();

        static CommandValidator()
        {
            foreach (ESQLCommands keyword in Enum.GetValues(typeof(ESQLCommands)))
                AllowedKeywords.Add(keyword.ToString());

            foreach (EInvalidTableNameCharacters keyword in Enum.GetValues(typeof(EInvalidTableNameCharacters)))
                InvalidTableNameCharacters.Add((char)keyword);

            foreach (ESqlServerDataTypes keyword in Enum.GetValues(typeof(ESqlServerDataTypes)))
                SqlDataTypes.Add(keyword.ToString());

            foreach (ESupportedDataTypes keyword in Enum.GetValues(typeof(ESupportedDataTypes)))
                SupportedSqlDataTypes.Add(keyword.ToString());
        }

        public static bool ValidateQuery(ECliCommands commandType, string command)
        {
            switch (commandType)
            {
                case ECliCommands.CreateTable:
                    bool isValidCreateTableCommand = ValidateCreateTableCommand(command);
                    if (!isValidCreateTableCommand)
                    {
                        Console.WriteLine("Please enter valid create table command!");
                        return false;
                    }
                    return true;
                case ECliCommands.DropTable:
                    bool isValidDropTableCommand = ValidateDropTableAndTableInfoCommands(command);
                    if (!isValidDropTableCommand)
                    {
                        Console.WriteLine("Please enter a valid dtop table command!");
                        return false;
                    }
                    return true;
                case ECliCommands.ListTables:
                    return true;
                case ECliCommands.TableInfo:
                    bool isValidTableInfoCommand = ValidateDropTableAndTableInfoCommands(command);
                    if (!isValidTableInfoCommand)
                    {
                        Console.WriteLine("Please enter a valid table info command!");
                        return false;
                    }
                    return true;
                case ECliCommands.Insert:
                    bool isValidInsertCommand = ValidateInsertTableCommand(command);
                    if (!isValidInsertCommand)
                    {
                        Console.WriteLine("Please enter a insert command!");
                        return false;
                    }
                    return true;
                default:
                    Console.WriteLine("Invalid command please enter a valid command");
                    return false;
            }
        }

        //createtable test(id int primary key, name nvarchar(50) null)
        private static bool ValidateCreateTableCommand(string command)
        {
            command = command.CustomTrim();

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

            if (tableName.CustomAny(x => InvalidTableNameCharacters.CustomContains(x)) ||
                SqlDataTypes.CustomAny(x => tableName.CustomToLower() == x.CustomToLower()))
            {
                Console.WriteLine("Invalid table name");
                return false;
            }

            string columnDefinition = command[(openingBracket + 1)..closingBracketForColumns].CustomTrim();
            string[] columnDefinitions = columnDefinition.CustomSplit(',');

            foreach (string item in columnDefinitions)
            {
                string commandTrimmed = item.CustomToLower().CustomTrim();
                /* if (!SqlDataTypes.CustomAny(x => item.CustomToLower().Contains(x.CustomToLower())))
                 {
                     Console.WriteLine("Invalid data type in create table command");
                     return false;
                 }*/

                int firstWhiteSpaceAfterColumnName = commandTrimmed.CustomIndexOf(' ');
                string columnName = commandTrimmed[..firstWhiteSpaceAfterColumnName].CustomTrim();
                if (SqlDataTypes.CustomAny(x => columnName.CustomToLower() == x.CustomToLower()))
                {
                    Console.WriteLine("Invalid column name");
                    return false;
                }

                int secondWhiteSpaceAfterColumnName = commandTrimmed.CustomIndexOf(' ', firstWhiteSpaceAfterColumnName + 1);
                string columnType = commandTrimmed[firstWhiteSpaceAfterColumnName..secondWhiteSpaceAfterColumnName].CustomTrim();
                //case when user uses nvarchar for string type because of the brackets
                if (columnType.CustomContains(ESupportedDataTypes.NVARCHAR.ToString()))
                {
                    if (!columnType.CustomContains('(') ||
                        !columnType.CustomContains(')'))
                    {
                        Console.WriteLine("Not supported data type curretly we support STRING/INT/DATE");
                        return false;
                    }
                }
                else if (!SupportedSqlDataTypes.CustomAny(x => x.CustomToLower().CustomContains(columnType)))
                {
                    Console.WriteLine("Not supported data type curretly we support STRING/INT/DATE");
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateDropTableAndTableInfoCommands(string command)
        {
            int firstWhiteSpace = command.CustomIndexOf(' ');
            string tableName = command[firstWhiteSpace..].CustomTrim();
            if (!Directory.Exists($"{Folders.DB_DATA_FOLDER}/{tableName}"))
                throw new Exception($"There is no table with the name {tableName}");

            return true;
        }

        private static bool ValidateInsertTableCommand(string command)
        {
            if (!command.CustomToLower().CustomContains("INSERT INTO".CustomToLower())
                || !command.CustomToLower().CustomContains("VALUES".CustomToLower()))
                throw new Exception("Not a valid insert into command");

            string loweredCommand = command.CustomToLower();
            string[] parts = loweredCommand.CustomSplit(' ');
            string tableName = parts[2];

            if (!Directory.Exists($"{Folders.DB_DATA_FOLDER}/{tableName}"))
                throw new Exception($"There is no table with name {tableName}");

            string[] columnsAndValues = loweredCommand.CustomSplit($"{tableName.CustomToLower()}");
            string[] values = columnsAndValues[1].CustomSplit("values");

            if (!values[0].CustomContains('(') || !values[0].CustomContains(')'))
                throw new Exception("No brackets for column definitions");

            string columnDefinitions = values[0].CustomTrim();
            columnDefinitions = columnDefinitions.CustomSubstring(1, columnDefinitions.Length - 2);

            //will open the metadata file and check for the column defined there
            string[] splitedColumnDefinitions = columnDefinitions.CustomSplit(',');
            (string[], string[]) deserializedMetadata = DataPageManager.DeserializeMetadata(tableName);

            for (int i = 0; i < deserializedMetadata.Item1.Length; i++)
            {
                if (deserializedMetadata.Item1[i].CustomToLower() != splitedColumnDefinitions[i].CustomTrim())
                    throw new Exception($"Invalid column {splitedColumnDefinitions[i]}");
            }

            return true;
        }
    }
}
