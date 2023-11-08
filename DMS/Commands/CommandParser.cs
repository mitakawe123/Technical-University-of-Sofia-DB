using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;

namespace DMS.Commands
{
    public static class CommandParser
    {
        public static void Parse(ECliCommands commandType, string command)
        {
            command = command.CustomToLower();

            bool isValidQuery = CommandValidator.ValidateQuery(commandType, command);

            if (!isValidQuery)
                return;

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
            }
        }
        //createtable test(id int primary key, name string null)
        private static void CreateTable(string command)
        {
            //add a case when there is default values
            ReadOnlySpan<char> commandSpan = command;
            int startAfterKeyword = ECliCommands.CreateTable.ToString().Length;
            int openingBracket = commandSpan.CustomIndexOf('(');
            int closingBracket = commandSpan.CustomLastIndexOf(')');
            int endBeforeParenthesis = commandSpan[startAfterKeyword..].CustomIndexOf('(');
            
            ReadOnlySpan<char> tableNameSpan = commandSpan.CustomSlice(startAfterKeyword, endBeforeParenthesis).CustomTrim();
            ReadOnlySpan<char> values = commandSpan[(openingBracket + 1)..closingBracket];
            DKList<Column> columns = new();

            while (values.Length > 0)
            {
                int commaIndex = values.CustomIndexOf(',');
                ReadOnlySpan<char> columnDefinition = commaIndex != -1 ? values[..commaIndex] : values;

                int spaceIndex = columnDefinition.CustomIndexOf(' ');
                
                ReadOnlySpan<char> columnName = columnDefinition[..spaceIndex].CustomTrim();
                ReadOnlySpan<char> columnType = columnDefinition[(spaceIndex + 1)..].CustomTrim();

                int typeSpaceIndex = columnType.CustomIndexOf(' ');
                columnType = columnType[..typeSpaceIndex];

                columns.Add(new Column(
                    new string(columnName),
                    Enum.Parse<EDataTypes>(columnType, true)));

                values = commaIndex != -1 ? values[(commaIndex + 1)..].CustomTrim() : ReadOnlySpan<char>.Empty;
            }

            DataPageManager.CreateTable(columns, tableNameSpan);
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

        }

        private static void DropTable(string command)
        {
        }

        private static void ListTables()
        {
        }

        //схема и брой записи, заемано пространство и др.
        private static void TableInfo(string command)
        {
            //how many columns are there in the table
            //how many records are there
            string tableName = command[command.CustomIndexOf(' ')..].CustomTrim();


            /*long totalTableSize = FolderSize(directoryTableSize);
            long totalFolderSizeDataPages = FolderSize(directoryInfoDataPages);
            long totalFolderSizeIAM = FolderSize(directoryInfoIAM);
            long totalSizeMetadata = directoryInfoMetadata.Length;*/
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
