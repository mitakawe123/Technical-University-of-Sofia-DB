using DMS.Constants;

namespace DMS.Commands
{
    public static class CommandParser
    {
        public static Command Parse(ECliCommands commandType, string command)
        {
            bool isValidQuery = CommandValidator.ValidateQuery(command);

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
            Console.WriteLine("Create table");
        }
    }
}
