using DataStructures;
using DMS.Constants;
using DMS.Extensions;
using DMS.Shared;

namespace DMS.Commands
{
    //here will lay the logic for the logic operators such as WHERE, AND, NOT, OR, ORDER BY, DISTINCT, JOIN
    //The purpose of this class is only filtration  
    public static class LogicalOperators
    {
        private static readonly string[] SqlKeywords = { "JOIN", "WHERE", "ORDER BY", "AND", "OR" };
        private static DKList<string> operators = new();
        private static DKList<string> operations = new();

        public static void Parse(
            ref DKList<char[]> allData,
            ref DKList<Column> selectedColumns,
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
                        operators.Add("where");
                        break;
                    case "and":
                        operators.Add("and");
                        break;
                    case "or":
                        operators.Add("or");
                        break;
                    case "not":
                        operators.Add("not");
                        break;
                    case "join":
                        operators.Add("join");
                        break;
                    case "distinct":
                        operators.Add("distinct");
                        break;
                    case "order":
                        if (tokens.CustomContains("by"))
                            operators.Add("order by");
                        break;
                }
            }

            operations = SplitSqlQuery(logicalOperator);

            ExecuteQuery(ref allData, ref selectedColumns, colCount);
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
            ref DKList<char[]> allData,
            ref DKList<Column> selectedColumns,
            int colCount)
        {
            for (int i = 0; i < operators.Count; i++)
            {
                switch (operators[i])
                {
                    case "where":
                        WhereCondition(ref allData, operations[i], colCount);
                        break;
                    case "order by":
                        break;
                    case "distinct":
                        DistinctCondition(ref allData);
                        break;
                    default:
                        break;
                }
            }

            operators.Clear();
            operations.Clear();
        }

        private static void WhereCondition(
            ref DKList<char[]> allData,
            string operation, //<- id = 1
            int colCount)
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
        private static void DistinctCondition(ref DKList<char[]> allData)
        {
            DKList<char[]> result = new();

            foreach (char[] rowValue in allData)
                if (!result.CustomAny(existingRow => existingRow.SequenceEqual(rowValue)))
                    result.Add(rowValue);

            allData = result;
        }
    }
}
