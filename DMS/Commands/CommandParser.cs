using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;

namespace DMS.Commands
{
    public static class CommandParser
    {
        public static void Parse(ECliCommands commandType, string command)
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
                    DropTable(command);
                    break;
                case ECliCommands.ListTables:
                    ListTables();
                    break;
                case ECliCommands.TableInfo:
                    TableInfo(command);
                    break;
                case ECliCommands.Insert:
                    InsertIntoTable(command);
                    break;
                default:
                    Console.WriteLine("Invalid command. Type 'help' for available commands.");
                    break;
            }
        }
        //createtable test(id int primary key, name nvarchar(50) null)
        private static void CreateTable(string command)
        {
            //add a case when there is default values
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
        //Insert INTO Sample (Id, Name) VALUES (1, “Иван”) 
        private static void InsertIntoTable(string command)
        {
            //catch the case when user insert multiple values
            string loweredCommand = command.CustomToLower();
            string[] parts = loweredCommand.CustomSplit(' ');
            string tableName = parts[2];

            string[] columnsAndValues = loweredCommand.CustomSplit($"{tableName.CustomToLower()}");
            string[] values = columnsAndValues[1].CustomSplit("values");

            string columnDefinition = values[0].CustomTrim();
            columnDefinition = columnDefinition.CustomSubstring(1, columnDefinition.Length - 2);
            string[] columnDefinitions = columnDefinition.CustomSplit(',');

            string columnValue = values[1].CustomTrim();
            columnValue = columnValue.CustomSubstring(1, columnValue.Length - 2);
            string[] columnValues = columnValue.CustomSplit(',');

            DataPageManager.InsertIntoTable(columnDefinitions, columnValues, tableName);
        }

        private static void DropTable(string command)
        {
            int firstWhiteSpace = command.CustomIndexOf(' ');
            string tableName = command[firstWhiteSpace..].CustomTrim();
            Directory.Delete($"{Folders.DB_DATA_FOLDER}/{tableName}", true);
            Directory.Delete($"{Folders.DB_IAM_FOLDER}/{tableName}", true);
            Console.WriteLine($"Successfully deleted {tableName}");
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

        //схема и брой записи, заемано пространство и др.
        private static void TableInfo(string command)
        {
            //here what I will list
            //the whole folder how much space it takes- done
            //how much the metadata takes -done
            //how much the IAM file takes - done
            //how much the data pages takes - done
            //how many columns are there in the table
            //how many records are there
            int firstWhiteSpace = command.CustomIndexOf(' ');
            string tableName = command[firstWhiteSpace..].CustomTrim();

            DirectoryInfo directoryTableSize = new($"{Folders.DB_DATA_FOLDER}/{tableName}");
            DirectoryInfo directoryInfoDataPages = new($"{Folders.DB_DATA_FOLDER}/{tableName}");
            DirectoryInfo directoryInfoIAM = new($"{Folders.DB_IAM_FOLDER}/{tableName}");
            FileInfo directoryInfoMetadata = new($"{Folders.DB_DATA_FOLDER}/{tableName}/metadata.bin");

            long totalTableSize = FolderSize(directoryTableSize); 
            long totalFolderSizeDataPages = FolderSize(directoryInfoDataPages);
            long totalFolderSizeIAM = FolderSize(directoryInfoIAM);
            long totalSizeMetadata = directoryInfoMetadata.Length;
        }

        private static long FolderSize(DirectoryInfo folder)
        {
            long totalSizeOfDir = 0;

            FileInfo[] allFiles = folder.GetFiles();
            foreach (FileInfo file in allFiles)
                totalSizeOfDir += file.Length;

            DirectoryInfo[] subFolders = folder.GetDirectories();
            foreach (DirectoryInfo dir in subFolders)
                totalSizeOfDir += FolderSize(dir);

            return totalSizeOfDir;
        }
    }
}
