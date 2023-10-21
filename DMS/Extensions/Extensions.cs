using DataStructures;
using DMS.Utils;
using System.Text;

namespace DMS.Extensions
{
    public static class Extensions
    {
        public static DKList<T> CustomToList<T>(this T[] array) => new(array);

        public static string[] CustomSplit(this string input, char[] separators)
        {
            DKList<string> result = new();
            int startIndex = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (separators.Contains(input[i]))
                {
                    if (i > startIndex)
                        result.Add(input[startIndex..i]);
                    
                    startIndex = i + 1;
                }
            }

            if (startIndex < input.Length)
                result.Add(input[startIndex..]);

            return result.CustomToArray();
        }

        public static string CustomTrim(this string input, char[] trimChars)
        {
            if (input.CustomIsNullOrEmpty() || trimChars is null || trimChars.Length is 0)
                return input;

            int startIndex = 0;
            int endIndex = input.Length - 1;

            while (startIndex <= endIndex && HelperMethods.CustomExists(trimChars, ch => ch == input[startIndex]))
                startIndex++;

            while (endIndex >= startIndex && HelperMethods.CustomExists(trimChars, ch => ch == input[endIndex]))
                endIndex--;

            if (startIndex > endIndex)
                return "";

            return input.CustomSubstring(startIndex, endIndex - startIndex + 1);
        }

        public static T[] CustomToArray<T>(this IEnumerable<T> collection)
        {
            T[] result = new T[collection.Count()];
            for (int i = 0; i < result.Length; i++)
                result[i] = collection.ElementAt(i);
            
            return result;
        }

        public static string CustomSubstring(this string input, int index, int length)
        {
            StringBuilder stringBuilder = new(input);
            return stringBuilder.ToString(index, length);
        }

        public static bool CustomIsNullOrEmpty(this string input)
        {
            if(input is null || input is "")
                return true;

            return false;
        }
    }
}
