using System.Net.Sockets;
using System.Net;
using DataStructures;

namespace DMS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BTree<int> bTree = new(1);
            bTree.Insert(5);
            bTree.Insert(59);
            bTree.Insert(555);
            bTree.Insert(5);
            bTree.Insert(44);
            bTree.Insert(5212);
            bTree.Insert(1);


            Console.WriteLine("Welcome to DMS");

            bool running = true;

            while (running)
            {
                Console.Write("Enter a command (or 'exit' to quit): ");
                string input = Console.ReadLine()!;

                switch ((ECliCommands)Enum.Parse(typeof(ECliCommands), input, true))
                {
                    case ECliCommands.Help:
                        Console.WriteLine($"Available commands: " +
                                          $"{ECliCommands.CreateTable}, " +
                                          $"{ECliCommands.DropTable}, " +
                                          $"{ECliCommands.ListTables}, " +
                                          $"{ECliCommands.TableInfo}");
                        break;
                    case ECliCommands.CreateTable:
                        Console.WriteLine("Create Table command logic");
                        break;
                    case ECliCommands.DropTable:
                        Console.WriteLine("Drop Table command logic");
                        break;
                    case ECliCommands.ListTables:
                        Console.WriteLine("List Tables command logic");
                        break;
                    case ECliCommands.TableInfo:
                        Console.WriteLine("Table Info command logic");
                        break;
                    default:
                        Console.WriteLine("Invalid command. Type 'help' for available commands.");
                        break;
                }
            }
        }
    }
}