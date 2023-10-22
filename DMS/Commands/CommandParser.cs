using DMS.Constants;
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

            switch(commandType)
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



        }
    }
}
