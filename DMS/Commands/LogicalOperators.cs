using System.Text;
using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;

namespace DMS.Commands
{
    //here will lay the logic for the logic operators such as WHERE, AND, NOT, OR, ORDER BY, DISTINCT, JOIN
    //The purpose of this class is only filtration  
    public static class LogicalOperators
    {
        private static readonly string[] SqlKeywords = { "JOIN", "WHERE", "ORDER BY", "AND", "OR" };
        private static readonly DKList<string> Operators = new();
        private static DKList<string> Operations = new();

        public static void Parse(
            ref IReadOnlyList<char[]> allData,
            IReadOnlyList<Column> selectedColumns,
            IReadOnlyList<Column> allColumnsForTable,
            ReadOnlySpan<char> logicalOperator,
            int colCount)
        {
            if (logicalOperator.IsEmpty)
                return;

            string operatorStr = new string(logicalOperator).CustomTrim();

            string[] tokens = operatorStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tokens)
            {
                switch (token)
                {
                    case "order":
                        if (tokens.CustomContains("by"))
                            Operators.Add("order by");
                        break;
                    case "where":
                        Operators.Add("where");
                        break;
                    case "and":
                        Operators.Add("and");
                        break;
                    case "or":
                        Operators.Add("or");
                        break;
                    case "not":
                        Operators.Add("not");
                        break;
                    case "join":
                        Operators.Add("join");
                        break;
                    case "distinct":
                        Operators.Add("distinct");
                        break;
                }
            }

            Operations = SplitSqlQuery(logicalOperator);

            ExecuteQuery(ref allData, allColumnsForTable, selectedColumns, colCount);
        }

        private static DKList<string> SplitSqlQuery(ReadOnlySpan<char> sqlQuery)
        {
            DKList<string> parts = new();
            int currentIndex = 0;

            while (currentIndex < sqlQuery.Length)
            {
                int nextKeywordIndex = FindNextKeywordIndex(sqlQuery[currentIndex..], out string foundKeyword);
                if (nextKeywordIndex != -1)
                {
                    nextKeywordIndex += currentIndex;
                    string part = sqlQuery.CustomSlice(currentIndex, nextKeywordIndex - currentIndex).ToString()
                        .CustomTrim();
                    if (!string.IsNullOrEmpty(part))
                        parts.Add(part);
                    currentIndex = nextKeywordIndex + foundKeyword.Length;
                }
                else
                {
                    string part = sqlQuery[currentIndex..].ToString().CustomTrim();
                    if (!string.IsNullOrEmpty(part))
                        parts.Add(part);
                    break;
                }
            }

            return parts;
        }

        private static int FindNextKeywordIndex(ReadOnlySpan<char> span, out string foundKeyword)
        {
            foundKeyword = null;
            int earliestIndex = int.MaxValue;

            foreach (string keyword in SqlKeywords)
            {
                int index = span.CustomIndexOf(keyword.CustomAsSpan(), StringComparison.OrdinalIgnoreCase);
                if (index == -1 || index >= earliestIndex)
                    continue;

                earliestIndex = index;
                foundKeyword = keyword;
            }

            return earliestIndex == int.MaxValue ? -1 : earliestIndex;
        }

        private static void ExecuteQuery(
            ref IReadOnlyList<char[]> allData,
            IReadOnlyList<Column> allColumnsForTable,
            IReadOnlyList<Column> selectedColumns,
            int colCount)
        {
            for (int i = 0; i < Operators.Count; i++)
            {
                switch (Operators[i])
                {
                    case "where":
                        WhereCondition(ref allData, colCount, Operations[i]);
                        break;
                    case "order by":
                        OrderByCondition(ref allData, allColumnsForTable, colCount, Operations[i]);
                        break;
                    case "join":
                        JoinCondition(ref allData, selectedColumns, colCount, Operations[i]);
                        break;
                    case "distinct":
                        DistinctCondition(ref allData);
                        break;
                }
            }

            Operators.Clear();
            Operations.Clear();
        }

        private static void WhereCondition(ref IReadOnlyList<char[]> allData, int colCount, string operation) //<- id = 1
        {
            int equalsIndex = operation.CustomIndexOf('=');
            DKList<int> blockIndexes = new();

            int startIndex = equalsIndex + 1;
            int endIndex = operation[startIndex..].CustomIndexOf(' ');
            if (endIndex == -1)
                endIndex = operation.Length;
            else
            {
                endIndex += startIndex;

                if (startIndex == endIndex)
                {
                    for (int i = endIndex + 1; i < operation.Length; i++)
                    {
                        if (operation[i].CustomIsWhiteSpace())
                        {
                            endIndex = i;
                            break;
                        }
                    }

                    if (startIndex == endIndex)
                        endIndex = operation.Length;
                }
            }

            char[] value = operation[startIndex..endIndex].CustomTrim().CustomToCharArray();

            // Find the block index that contains the target char[]
            for (int i = 0; i < allData.Count; i++)
            {
                if (value.SequenceEqual(allData[i]))
                {
                    int blockIndex = i / colCount; // Determine the block index
                    blockIndexes.Add(blockIndex);
                }
            }

            if (blockIndexes.Count == 0)
            {
                Console.WriteLine("Value not found");
                allData = new DKList<char[]>();
                return;
            }

            DKList<char[]> result = new();
            foreach (int blockIndex in blockIndexes)
            {
                int blockStartIndex = blockIndex * colCount;
                for (int i = blockStartIndex; i < blockStartIndex + colCount && i < allData.Count; i++)
                    result.Add(allData[i]);
            }

            allData = result;
        }

        //select * from test where id = 2 distinct
        private static void DistinctCondition(ref IReadOnlyList<char[]> allData)
        {
            DKList<char[]> result = new();

            foreach (char[] rowValue in allData)
                if (!result.CustomAny(existingRow => existingRow.SequenceEqual(rowValue)))
                    result.Add(rowValue);

            allData = result;
        }

        private static void OrderByCondition(
            ref IReadOnlyList<char[]> allData,
            IReadOnlyList<Column> allColumnsForTable,
            int colCount,
            string operation)
        {
            string[] multiColOrdering = operation.CustomSplit(',');

            // Map column names to their indices
            Dictionary<string, int> columnMap = allColumnsForTable.Select((col, index) => new { col.Name, Index = index })
                .ToDictionary(x => new string(x.Name), x => x.Index);

            DKList<(int Index, bool IsAscending)> sortingColumns = new();
            foreach (string orderClause in multiColOrdering)
            {
                string trimmedOrderClause = orderClause.CustomTrim();
                string columnName = trimmedOrderClause.CustomSplit(' ')[0].CustomTrim();
                bool isAsc = !trimmedOrderClause.CustomContains("desc") && !trimmedOrderClause.CustomContains("descending");

                if (!columnMap.TryGetValue(columnName, out int columnIndex))
                {
                    Console.WriteLine($"Invalid column in order by condition {columnName}");
                    return;
                }
                sortingColumns.Add((columnIndex, isAsc));
            }

            DKList<DKList<char[]>> rows = new();
            for (int i = 0; i < allData.Count; i += colCount)
                rows.Add(allData.CustomSkip(i).CustomTake(colCount).CustomToList());

            Comparison<DKList<char[]>> rowComparer = (row1, row2) =>
            {
                foreach ((int index, bool isAscending) in sortingColumns)
                {
                    string value1 = new(row1[index]);
                    string value2 = new(row2[index]);

                    int comparison = string.Compare(value1, value2, StringComparison.Ordinal);
                    if (comparison is not 0)
                        return isAscending ? comparison : -comparison;
                }
                return 0;
            };

            rows.Sort(rowComparer);

            DKList<char[]> sortedData = rows.SelectMany(row => row).CustomToList();
            allData = sortedData.AsReadOnly();
        }

        private static void JoinCondition(
            ref IReadOnlyList<char[]> allDataFromMainTable,
            IReadOnlyList<Column> selectedColumns,
            int colCount,
            string operation)
        {
            int indexTableToJoin = operation.CustomIndexOf(' ');
            char[] tableToJoin = operation[..indexTableToJoin].CustomToCharArray();

            int indexOnKeyword = operation.CustomIndexOf("on");
            if (indexOnKeyword is -1)
            {
                Console.WriteLine("There is no \'ON\' keyword after the table that will be joined");
                return;
            }

            char[] matchingKey = null;
            foreach (char[] table in DataPageManager.TableOffsets.Keys)
            {
                if (tableToJoin.SequenceEqual(table))
                {
                    matchingKey = table;
                    break;
                }
            }

            if (matchingKey is null)
            {
                Console.WriteLine("Wrong table name to join");
                return;
            }

            IReadOnlyList<char[]> joinedTableData = AllDataFromJoinedTable(DataPageManager.TableOffsets[matchingKey], matchingKey, colCount);
        }

        private static IReadOnlyList<char[]> AllDataFromJoinedTable(long startOfOffsetForJoinedTable, char[] matchingKey, int colCount)
        {
            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader binaryReader = new(fileStream, Encoding.UTF8);

            int headerSectionForMainDP = 20 + matchingKey.Length;
            fileStream.Seek(startOfOffsetForJoinedTable + headerSectionForMainDP, SeekOrigin.Begin);

            (headerSectionForMainDP, DKList<Column> columnTypeAndName) = SQLCommands.ReadColumns(binaryReader, headerSectionForMainDP, colCount);

            long start = DataPageManager.TableOffsets[matchingKey] + headerSectionForMainDP;
            long end = DataPageManager.TableOffsets[matchingKey] + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
            long lengthToRead = end - start;

            fileStream.Seek(start, SeekOrigin.Begin);

            DKList<char[]> allDataFromJoinedTable = SQLCommands.ReadAllData(lengthToRead, binaryReader);

            fileStream.Seek(end, SeekOrigin.Begin);
            long pointer = binaryReader.ReadInt64();

            while (pointer != DataPageManager.DefaultBufferForDP)
            {
                fileStream.Seek(pointer, SeekOrigin.Begin);
                binaryReader.ReadInt32(); //<- free space
                start = pointer + sizeof(int);
                end = pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer;
                lengthToRead = end - start;

                allDataFromJoinedTable = allDataFromJoinedTable.Concat(SQLCommands.ReadAllData(lengthToRead, binaryReader)).CustomToList();
                fileStream.Seek(pointer + DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                pointer = binaryReader.ReadInt64();
            }

            return allDataFromJoinedTable;
        }
    }
}
