using DMS.Commands;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;

namespace DMS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            File.Delete(Files.MDF_FILE_NAME);
            DataPageManager.InitDataPageManager();

            bool running = true;

            while (running)
            {
                Console.Write("Enter a command (or 'exit' to quit): ");
                string command = Console.ReadLine()!;
                string input = command.CustomSplit(new char[] { ' ' })[0];

                if (!Enum.TryParse(input, true, out ECliCommands cliCommand))
                {
                    Console.WriteLine("Invalid command type help");
                    continue;
                }

                switch (cliCommand)
                {
                    case ECliCommands.Help:
                        Console.WriteLine("Available commands: " + string.Join(", ", Enum.GetNames<ECliCommands>()));
                        break;

                    case ECliCommands.Exit:
                        DataPageManager.ConsoleEventCallback();
                        running = false;
                        break;
                    case ECliCommands.Clear:
                    case ECliCommands.Cls:
                        Console.Clear();
                        break;

                    default:
                        CommandParser.Parse(cliCommand, command);
                        break;
                }
            }
        }
    }
}