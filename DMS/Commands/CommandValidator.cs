using DataStructures;
using DMS.Constants;
using DMS.Extensions;

namespace DMS.Commands
{
    public static class CommandValidator
    {
        //remove this hashset and implement your
        private static readonly HashSet<string> AllowedKeywords = new(StringComparer.OrdinalIgnoreCase);
        private static readonly DKList<char> InvalidTableNameCharacters = new();
        private static readonly DKList<string> SqlDataTypes = new();

        static CommandValidator()
        {
            foreach (ESQLCommands keyword in Enum.GetValues(typeof(ESQLCommands)))
                AllowedKeywords.Add(keyword.ToString());

            foreach (InvalidTableNameCharacters keyword in Enum.GetValues(typeof(InvalidTableNameCharacters)))
                InvalidTableNameCharacters.Add((char)keyword);

            foreach (SqlServerDataTypes keyword in Enum.GetValues(typeof(SqlServerDataTypes)))
                SqlDataTypes.Add(keyword.ToString());
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
                if (!SqlDataTypes.CustomAny(x => item.CustomToLower().Contains(x.CustomToLower())))
                {
                    Console.WriteLine("Invalid data type in create table command");
                    return false;
                }

                int firstWhiteSpaceAfterColumnName = item.IndexOf(' ');
                string columnName = item[..firstWhiteSpaceAfterColumnName];
                if (SqlDataTypes.CustomAny(x => columnName.CustomToLower() == x.CustomToLower()))
                {
                    Console.WriteLine("Invalid column name");
                    return false;
                }
            }

            return true;
        }
    }
}
