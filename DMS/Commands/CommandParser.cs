using DataStructures;
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
                return;

            command = command.CustomToLower();

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
        //createtable test(id int primary key, name nvarchar(50) null, namemain nvarchar(50) null)
        private static void CreateTable(string command)
        {
            //add a case when there is default values
            ReadOnlySpan<char> commandSpan = command;
            int startAfterKeyword = "createtable".Length;
            int openingBracket = commandSpan.CustomIndexOf('(');
            int closingBracket = commandSpan.CustomLastIndexOf(')');
            int endBeforeParenthesis = commandSpan[startAfterKeyword..].IndexOf('(');

            ReadOnlySpan<char> tableNameSpan = commandSpan.Slice(startAfterKeyword, endBeforeParenthesis).Trim();
            ReadOnlySpan<char> values = commandSpan[(openingBracket + 1)..closingBracket];
            DKList<string> columnNames = new();
            DKList<string> columnTypes = new();

            while (values.Length > 0)
            {
                int commaIndex = values.IndexOf(',');
                ReadOnlySpan<char> columnDefinition = commaIndex != -1 ? values[..commaIndex] : values;

                int spaceIndex = columnDefinition.IndexOf(' ');

                ReadOnlySpan<char> columnName = columnDefinition[..spaceIndex].Trim();
                ReadOnlySpan<char> columnType = columnDefinition[(spaceIndex + 1)..].Trim();

                int typeSpaceIndex = columnType.IndexOf(' ');
                columnType = columnType[..typeSpaceIndex];

                columnNames.Add(columnName.ToString());
                columnTypes.Add(columnType.ToString());

                values = commaIndex != -1 ? values[(commaIndex + 1)..].Trim() : ReadOnlySpan<char>.Empty;
            }

            DataPageManager.CreateTable(columnNames, columnTypes, tableNameSpan);
        }
        //Insert INTO test (Id, Name) VALUES (1, “pepi”, 3), (2, “mariq”, 6), (3, “georgi”, 1)
        private static void InsertIntoTable(string command)
        {
            string[] parts = command.CustomSplit(' ');
            string tableName = parts[2];

            string[] columnsAndValues = command.CustomSplit($"{tableName.CustomToLower()}");
            string[] values = columnsAndValues[1].CustomSplit("values");

            values[1] = values[1].CustomTrim();
            string[] tupleStrings = values[1].Split(new string[] { "), (" }, StringSplitOptions.RemoveEmptyEntries);
            DKList<string> columnValuesSplitted = new();
            foreach (string tuple in tupleStrings)
            {
                string cleanedTuple = tuple.CustomTrim(new char[] { '(', ')' });
                columnValuesSplitted.Add(cleanedTuple);
            }

            string columnDefinition = values[0].CustomTrim();
            columnDefinition = columnDefinition.CustomSubstring(1, columnDefinition.Length - 2);
            string[] columnDefinitions = columnDefinition.CustomSplit(',');

            DataPageManager.InsertIntoTable(columnDefinitions, tableName, columnValuesSplitted);
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

            DirectoryInfo dirInfo = new DirectoryInfo(Folders.DB_DATA_FOLDER + "/");
            FileInfo[] files = dirInfo.GetFiles();

            foreach (FileInfo file in files)
            {
                Console.WriteLine(file.Name);
            }
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
            //how many columns are there in the table
            //how many records are there
            string tableName = command[command.CustomIndexOf(' ')..].CustomTrim();

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
