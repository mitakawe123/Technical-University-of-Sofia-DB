using DataStructures;
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
                    case "order":
                        if (tokens.CustomContains("by"))
                            Operators.Add("order by");
                        break;
                }
            }

            Operations = SplitSqlQuery(logicalOperator);

            ExecuteQuery(ref allData, selectedColumns, allColumnsForTable, colCount);
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

        private static void ExecuteQuery(ref IReadOnlyList<char[]> allData, IReadOnlyList<Column> allColumnsForTable, IReadOnlyList<Column> selectedColumns, int colCount)
        {
            for (int i = 0; i < Operators.Count; i++)
            {
                switch (Operators[i])
                {
                    case "where":
                        WhereCondition(ref allData, colCount, Operations[i]);
                        break;
                    case "order by":
                        OrderByCondition(ref allData, selectedColumns, colCount, Operations[i]);
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
            int equalsIndex = operation.IndexOf('=');
            DKList<int> blockIndexes = new();

            int startIndex = equalsIndex + 1;
            int endIndex = operation[startIndex..].IndexOf(' ');
            if (endIndex == -1)
                endIndex = operation.Length;
            else
            {
                endIndex += startIndex;

                if (startIndex == endIndex)
                {
                    for (int i = endIndex + 1; i < operation.Length; i++)
                    {
                        if (char.IsWhiteSpace(operation[i]))
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

        private static void OrderByCondition(ref IReadOnlyList<char[]> allData, IReadOnlyList<Column> selectedColumns, int colCount, string operation)
        {
            DKList<char[]> result = new();
            bool isAsc = !operation.CustomContains("desc") && !operation.CustomContains("descending");

            int indexOfOrderType = isAsc ? operation.CustomIndexOf("asc") : operation.CustomIndexOf("desc");
            string columnNames = operation[..indexOfOrderType].CustomTrim();
            DKList<char[]> cols = new();

            if (!columnNames.CustomContains(','))
                cols.Add(columnNames.ToCharArray());
            else
            {
                string[] spitedColumns = columnNames.CustomSplit(',');
                foreach (string colName in spitedColumns)
                    cols.Add(colName.CustomTrim().ToCharArray());
            }

            foreach (char[] col in cols)
            {
                if (!selectedColumns.CustomAny(x => x.Name.SequenceEqual(col)))
                {
                    Console.WriteLine($"Invalid column in order by condition {col}");
                    return;
                }
            }

            DKList<DKList<char[]>> records = new();
            int dataIndex = 0;
            while (dataIndex < allData.Count)
            {
                DKList<char[]> innerList = new();

                for (int i = 0; i < colCount && dataIndex < allData.Count; i++)
                {
                    innerList.Add(allData[dataIndex]);
                    dataIndex++;
                }

                records.Add(innerList);
            }
        }
    }
}
