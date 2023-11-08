using DMS.Commands;
using DMS.Constants;
using DMS.Extensions;

namespace DMS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists(Files.MDF_FILE_NAME))
                File.Create(Files.MDF_FILE_NAME);

            Console.WriteLine("Welcome to DMS");

            bool running = true;

            while (running)
            {
                try
                {
                    Console.Write("Enter a command (or 'exit' to quit): ");
                    string command = Console.ReadLine()!;
                    string input = command.CustomSplit(new char[] { ' ' })[0];

                    switch (Enum.Parse<ECliCommands>(input, true))
                    {
                        case ECliCommands.Help:
                            Console.WriteLine("Available commands: " + string.Join(", ", Enum.GetNames(typeof(ECliCommands))));
                            break;

                        case ECliCommands.CreateTable:
                            CommandParser.Parse(ECliCommands.CreateTable, command);
                            break;

                        case ECliCommands.DropTable:
                            CommandParser.Parse(ECliCommands.DropTable, command);
                            break;

                        case ECliCommands.ListTables:
                            CommandParser.Parse(ECliCommands.ListTables, command);
                            break;

                        case ECliCommands.TableInfo:
                            CommandParser.Parse(ECliCommands.TableInfo, command);
                            break;

                        case ECliCommands.Insert:
                            CommandParser.Parse(ECliCommands.Insert, command);
                            break;

                        case ECliCommands.Exit:
                            running = false;
                            break;

                        default:
                            Console.WriteLine("Invalid command. Type 'help' for available commands.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Invalid command. Type 'help' for available commands. \n" + ex.Message);
                }
            }
        }
    }
}