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
                    break;
                case ECliCommands.TableInfo:
                    break;
                default:
                    break;
            }


            return new Command();
        }

        private static void CreateTable(string command)
        {
            //create the data pages for the sql
            command = command.CustomToLower();
            int firstWhiteSpace = command.CustomIndexOf(' ');
            int openingBracket = command.CustomIndexOf('(');
            int closingBracketForColumns = command.CustomLastIndexOf(')');
            string tableName = command[(firstWhiteSpace + 1)..openingBracket];


            byte[] data = { 72, 101, 114, 101, 32, 105, 115, 32, 97, 32, 117, 110, 105, 99, 111, 100, 101, 32, 99, 104, 97, 114, 97, 99, 116, 101, 114, 115, 32, 115, 116, 114, 105, 110, 103, 46, 32, 80, 105, 32, 40, 206, 160, 41, };
            DataPage.WriteData(data);
        }
    }
}
