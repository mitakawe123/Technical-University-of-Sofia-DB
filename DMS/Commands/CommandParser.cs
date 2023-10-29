using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;

namespace DMS.Commands
{
    public static class CommandParser
    {
        public static Command Parse(ECliCommands commandType, string command)
        {
            bool isValidQuery = CommandValidator.ValidateQuery(commandType, command);

            if (!isValidQuery)
                throw new Exception("Invalid Query");

            switch (commandType)
            {
                case ECliCommands.CreateTable:
                    CreateTable(command);
                    break;
                case ECliCommands.DropTable:
                    break;
                case ECliCommands.ListTables:
                    ListTables();
                    break;
                case ECliCommands.TableInfo:
                    break;
                default:
                    break;
            }


            return new Command();
        }
        //createtable test(id int primary key, name nvarchar(50) null)
        private static void CreateTable(string command)
        {
            command = command.CustomToLower();
            int firstWhiteSpace = command.CustomIndexOf(' ');
            int openingBracket = command.CustomIndexOf('(');
            int closingBracketForColumns = command.CustomLastIndexOf(')');

            string tableName = command[(firstWhiteSpace + 1)..openingBracket];
            string columnDefinition = command[(openingBracket + 1)..closingBracketForColumns].CustomTrim();
            string[] columnDefinitions = columnDefinition.CustomSplit(',');

            string[] columnNames = new string[columnDefinitions.Length];
            string[] columnTypes = new string[columnDefinitions.Length];
            for (int i = 0; i < columnDefinitions.Length; i++)
            {
                string trimedColumn = columnDefinitions[i].CustomTrim();
                string[] values = trimedColumn.Split(' ');
                columnNames[i] = values[0];
                columnTypes[i] = values[1];
            }

            DataPageManager.CreateTable(columnNames, columnTypes, tableName);
        }

        private static void ListTables()
        {
            string[] filesindirectory = Directory.GetDirectories(Folders.DB_DATA_FOLDER);
            foreach (string dir in filesindirectory)
            {
                char[] pathChars = dir.CustomToCharArray();
                char[] pathCharsReversed = pathChars.CustomArrayReverse();
                string reversedPath = new(pathCharsReversed);

                int slashIndex = reversedPath.CustomIndexOfAny(new char[] { '/', '\\' });

                if (slashIndex < 0)
                    Console.WriteLine(dir);
                else
                {
                    string reversedFirstPart = reversedPath[..slashIndex];

                    char[] firstPartChars = reversedFirstPart.CustomToCharArray();
                    char[] firstPartCharsReversed = firstPartChars.CustomArrayReverse();
                    string firstPart = new(firstPartCharsReversed);

                    Console.WriteLine(firstPart);
                }
            }
        }
    }
}
