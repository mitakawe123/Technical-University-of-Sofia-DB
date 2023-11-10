using DMS.Commands;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;
using System.Runtime.InteropServices;

namespace DMS
{
    internal class Program
    {
        private delegate bool ConsoleEventDelegate(int eventType);
        private static readonly ConsoleEventDelegate ConsoleEventHandler = DataPageManager.ConsoleEventCallback;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        static void Main(string[] args)
        {
            DataPageManager.RemoveIntFromEndOfFile();

            SetConsoleCtrlHandler(ConsoleEventHandler, true);

            Console.WriteLine("Welcome to DMS");

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
                        running = false;
                        break;

                    default:
                        CommandParser.Parse(cliCommand, command);
                        break;
                }
            }
        }
    }
}