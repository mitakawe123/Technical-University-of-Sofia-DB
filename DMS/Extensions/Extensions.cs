using DataStructures;
using DMS.Constants;
using DMS.Utils;
using System.Text;

namespace DMS.Extensions
{
    public static class Extensions
    {
        public static DKList<T> CustomToList<T>(this T[] array) => new(array);

        public static string CustomToLower(this string input)
        {
            if (input is null)
                return "";

            char[] chars = input.CustomToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (chars[i].CustomIsUpper())
                    chars[i] = (char)(chars[i] + 32);

            return new string(chars);
        }

        public static char[] CustomToCharArray(this string input)
        {
            if (input is null)
                return new char[0];

            char[] chars = new char[input.Length];

            for (int i = 0; i < input.Length; i++)
                chars[i] = input[i];

            return chars;
        }

        public static bool CustomIsUpper(this char input) => input >= 'A' && input <= 'Z';

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

        public static string[] CustomSplit(this string input, char separator)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            DKList<string> result = new();
            int startIndex = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == separator)
                {
                    result.Add(input[startIndex..i]);
                    startIndex = i + 1;
                }
            }

            result.Add(input[startIndex..]);

            return result.ToArray();
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

        public static string CustomTrim(this string input)
        {
            if (input is null)
                return "";

            int startIndex = 0;
            int endIndex = input.Length - 1;

            while (startIndex <= endIndex && input[startIndex].CustomIsWhiteSpace())
                startIndex++;

            while (endIndex >= startIndex && input[endIndex].CustomIsWhiteSpace())
                endIndex--;

            if (startIndex > endIndex)
                return "";

            return input.CustomSubstring(startIndex, endIndex - startIndex + 1);
        }

        public static bool CustomContains(this string input, string value)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return input.CustomIndexOf(value, StringCompare.IgnoreCaseSensitivity) >= 0;
        }

        public static bool CustomContains<T>(this IEnumerable<T> input, T value)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            return input.CustomAny(x => x.Equals(value));
        }

        public static bool CustomIsWhiteSpace(this char input) => input is ' ';

        public static bool CustomStartsWith(this string input, string value, StringCompare comparisonType)
        {
            if(comparisonType == StringCompare.IgnoreCaseSensitivity && input.CustomToLower() == value.CustomToLower())
                return true;

            return false;
        }

        public static int CustomIndexOf(this string input, char value)
        {
            for (int i = 0; i < input.Length; i++)
                if (input[i] == value)
                    return i;

            return -1;
        }

        public static int CustomIndexOf(this string input, string value, StringCompare comparisonType)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            int inputLength = input.Length;
            int valueLength = value.Length;

            for (int i = 0; i <= inputLength - valueLength; i++)
            {
                bool found = true;
                for (int j = 0; j < valueLength; j++)
                {
                    char inputChar = input[i + j];
                    char valueChar = value[j];

                    if (comparisonType == StringCompare.IgnoreCaseSensitivity)
                    {
                        inputChar = char.ToLower(inputChar);
                        valueChar = char.ToLower(valueChar);
                    }

                    if (inputChar != valueChar)
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    return i;
            }

            return -1;
        }

        public static int CustomLastIndexOf(this string input, char value)
        {
            if(input is null)
                throw new ArgumentNullException(nameof(input));

            for (int i = input.Length - 1; i > 0; i--)
                if (input[i] == value) 
                    return i;

            return -1;
        }

        public static int CustomIndexOfAny(this string input, char[] anyOf)
        {
            for (int i = 0; i < input.Length; i++)
                for (int j = 0; j < anyOf.Length; j++)
                    if (input[i] == anyOf[j])
                        return i;

            return -1;
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
            if (input is null || input is "")
                return true;

            return false;
        }

        public static bool CustomAny<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (T item in source)
                if (predicate(item))
                    return true;

            return false;
        }
    
        public static char[] CustomArrayReverse(this char[] input)
        {
            char[] output = new char[input.Length];
            int j = 0;
            for (int i = input.Length - 1; i >= 0; i--)
            {
                output[j] = input[i];
                j++;
            }
            return output;
        }
    }
}
