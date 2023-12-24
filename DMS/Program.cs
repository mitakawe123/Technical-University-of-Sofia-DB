using DMS.Commands;
using DMS.Constants;
using DMS.DataPages;
using DMS.DataRecovery;
using DMS.Extensions;

namespace DMS
{
    internal static class Program
    {
        private static void Main()
        { 
            bool isThereCorruptedDataPages = FileIntegrityChecker.CheckForCorruptionOnStart();
            if (isThereCorruptedDataPages)
            {
                Console.WriteLine("There is corruption in the DB");
                Environment.Exit(0);
            }

            DataPageManager.InitDataPageManager();

            bool running = true;

            while (running)
            {
                Console.Write("Enter a command (or 'exit' to quit): ");
                string command = Console.ReadLine()!;
                string[] cliInput = command.CustomSplit(new[] { ' ' });
                //string uiPath = @"D:\my_own_projects\DMS\UI\bin\Debug\net7.0-windows\UI.exe";

                string input = string.Empty;
                if (cliInput.Length is not 0)
                    input = cliInput[0];

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

                    /*case ECliCommands.UI:
                        Process.Start(uiPath);
                        DataPageManager.ConsoleEventCallback();
                        running = false;
                        break;*/

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