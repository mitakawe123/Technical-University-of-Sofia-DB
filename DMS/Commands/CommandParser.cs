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
        //createtable test(id int primary key, name nvarchar(50) null)
        private static void CreateTable(string command)
        {
            //add a case when there is default values
            ReadOnlySpan<char> commandSpan = command;
            int startAfterKeyword = "createtable".Length;
            int openingBracket = commandSpan.CustomIndexOf('(');
            int closingBracket = commandSpan.CustomLastIndexOf(')');
            int endBeforeParenthesis = commandSpan[startAfterKeyword..].CustomIndexOf('(');

            ReadOnlySpan<char> tableNameSpan = commandSpan.CustomSlice(startAfterKeyword, endBeforeParenthesis).CustomTrim();
            ReadOnlySpan<char> values = commandSpan[(openingBracket + 1)..closingBracket];
            DKList<string> columnNames = new();
            DKList<string> columnTypes = new();

            while (values.Length > 0)
            {
                int commaIndex = values.CustomIndexOf(',');
                ReadOnlySpan<char> columnDefinition = commaIndex != -1 ? values[..commaIndex] : values;

                int spaceIndex = columnDefinition.CustomIndexOf(' ');

                ReadOnlySpan<char> columnName = columnDefinition[..spaceIndex].CustomTrim();
                ReadOnlySpan<char> columnType = columnDefinition[(spaceIndex + 1)..].CustomTrim();

                int typeSpaceIndex = columnType.CustomIndexOf(' ');
                columnType = columnType[..typeSpaceIndex];

                columnNames.Add(columnName.ToString());
                columnTypes.Add(columnType.ToString());

                values = commaIndex != -1 ? values[(commaIndex + 1)..].CustomTrim() : ReadOnlySpan<char>.Empty;
            }

            DataPageManager.CreateTable(columnNames, columnTypes, tableNameSpan);
        }
        //Insert INTO test (Id, Name) VALUES (1, “pepi”), (2, “mariq”), (3, “georgi”)
        private static void InsertIntoTable(string command)
        {
            ReadOnlySpan<char> commandSpan = command;
            int startAfterKeyword = commandSpan.CustomIndexOf("insert into") + "insert into".Length;
            int valuesKeyword = commandSpan.CustomIndexOf("values") + "values".Length;

            int endBeforeParenthesis = commandSpan[startAfterKeyword..].CustomIndexOf('(');

            ReadOnlySpan<char> tableNameSpan = commandSpan.CustomSlice(startAfterKeyword, endBeforeParenthesis).CustomTrim();
            ReadOnlySpan<char> valuesSpan = commandSpan[valuesKeyword..].CustomTrim();

            DKList<string> columnValues = new();

            while (valuesSpan.Length > 0)
            {
                int bracketIndex = valuesSpan.CustomIndexOf(')');
                ReadOnlySpan<char> vals = bracketIndex != -1
                    ? valuesSpan[..bracketIndex]
                    : valuesSpan;

                int openingBracket = vals.CustomIndexOf('(');

                columnValues.Add(vals[(openingBracket + 1)..].ToString());

                valuesSpan = bracketIndex != -1
                    ? valuesSpan[(bracketIndex + 1)..].CustomTrim()
                    : ReadOnlySpan<char>.Empty;
            }

            DataPageManager.InsertIntoTable(columnValues, tableNameSpan);
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

            DirectoryInfo dirInfo = new(Folders.DB_DATA_FOLDER + "/");
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
            FileInfo directoryInfoMetadata = new($"{Folders.DB_DATA_FOLDER}/{tableName}/{Files.METADATA_NAME}");

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
