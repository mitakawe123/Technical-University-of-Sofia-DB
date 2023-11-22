using DataStructures;
using DMS.Extensions;

namespace DMS.Utils
{
    public static class HelperMethods
    {
        private static readonly string[] SqlKeywords = { "join", "where", "order by", "and", "or", "distinct" };

        public static bool CustomExists<T>(T[] array, Predicate<T> match) => FindIndex(array, 0, array.Length, match) != -1;

        public static int FindIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
                if (match(array[i]))
                    return i;
            
            return -1;
        }

        public static DKList<string> SplitSqlQuery(ReadOnlySpan<char> sqlQuery)
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

        private static int FindNextKeywordIndex(ReadOnlySpan<char> span, out string? foundKeyword)
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
    }
}
