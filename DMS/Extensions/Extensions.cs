using DataStructures;
using DMS.Constants;
using DMS.Utils;
using System.Text;

namespace DMS.Extensions
{
    public static class Extensions
    {
        #region nomal extensions
        public static DKList<T> CustomToList<T>(this T[] array) => new(array);

        public static DKList<T> CustomToList<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DKList<T> customList = new();
            foreach (T? item in source)
                customList.Add(item);

            return customList;
        }

        public static int CustomSum<T>(this IEnumerable<T> source, Func<T, int> selector)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            int sum = 0;
            foreach (T item in source)
                sum += selector(item);

            return sum;
        }

        public static IEnumerable<T> CustomWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            DKList<T> result = new();
            foreach (T item in source)
            {
                if (predicate(item))
                    result.Add(item);
            }

            return result;
        }

        public static DKList<T> CustomReverse<T>(this IEnumerable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            DKList<T> result = new();
            foreach (T item in source)
                result.Insert(0, item);

            return result;
        }

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

        public static bool CustomIsUpper(this char input) => input is >= 'A' and <= 'Z';

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

        public static string[] CustomSplit(this string input, string separator)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            if (separator is null)
                throw new ArgumentNullException(nameof(separator));

            int separatorLength = separator.Length;
            int inputLength = input.Length;
            int startIndex = 0;
            int matchIndex = -1;
            int count = 0;

            DKList<string> result = new();

            while ((matchIndex = input.IndexOf(separator, startIndex)) >= 0)
            {
                result.Add(input[startIndex..matchIndex]);
                startIndex = matchIndex + separatorLength;
                count++;
            }

            if (count == 0)
                result.Add(input);
            else
                result.Add(input[startIndex..inputLength]);

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

            return startIndex > endIndex ? "" : input.CustomSubstring(startIndex, endIndex - startIndex + 1);
        }

        public static string CustomTrim(this string input)
        {
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

            return input.CustomAny(x => x.CustomEquals(value));
        }

        public static bool CustomAll<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (TSource element in source)
                if (!predicate(element))
                    return false;

            return true;
        }

        public static bool CustomIsWhiteSpace(this char input) => input is ' ';

        public static bool CustomStartsWith(this string input, string value, StringCompare comparisonType)
        {
            if (comparisonType == StringCompare.IgnoreCaseSensitivity && input.CustomToLower() == value.CustomToLower())
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

        public static int CustomIndexOf(this string input, string value)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(value))
                return -1;

            for (int i = 0; i <= input.Length - value.Length; i++)
                if (input.CustomSubstring(i, value.Length) == value)
                    return i;

            return -1;
        }

        public static int CustomIndexOf(this string input, char value, int startPosition)
        {
            for (int i = startPosition; i < input.Length; i++)
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
            if (input is null)
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

        public static string[] CustomSplit(this string input, string[]? separators, StringSplitOptions options)
        {
            if (separators == null || separators.Length == 0)
                return new[] { input };

            DKList<string> results = new();
            string currentSegment = string.Empty;
            int potentialMatchIndex = -1;
            int matchedSeparatorIndex = 0;
            int[] separatorsLengths = new int[separators.Length];

            for (int i = 0; i < separators.Length; i++)
                separatorsLengths[i] = separators[i].Length;

            foreach (char character in input)
            {
                bool matchedSeparator = false;
                foreach (string separator in separators)
                {
                    if (character == separator[matchedSeparatorIndex])
                    {
                        if (potentialMatchIndex == -1)
                            potentialMatchIndex = currentSegment.Length;

                        matchedSeparatorIndex++;

                        if (matchedSeparatorIndex != separator.Length)
                            continue;

                        results.Add(currentSegment[..potentialMatchIndex]);
                        currentSegment = currentSegment[(potentialMatchIndex + separator.Length)..];
                        matchedSeparator = true;
                        matchedSeparatorIndex = 0;
                        potentialMatchIndex = -1;
                        break;
                    }

                    if (matchedSeparatorIndex <= 0)
                        continue;

                    matchedSeparatorIndex = 0;
                    potentialMatchIndex = -1;
                }

                if (!matchedSeparator)
                    currentSegment += character;
            }

            if (currentSegment.Length > 0 || options != StringSplitOptions.RemoveEmptyEntries)
                results.Add(currentSegment);

            return results.CustomToArray();
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

        public static bool CustomEquals<T>(this T input, T? value)
        {
            if (input == null && value == null)
                return true;
            else if (input == null || value == null)
                return false;

            return EqualityComparer<T>.Default.Equals(input, value);
        }

        public static T CustomElementAt<T>(this IEnumerable<T> input, int index)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (index < 0)
                return default;

            int currentIndex = 0;
            foreach (T element in input)
            {
                if (currentIndex == index)
                    return element;
                currentIndex++;
            }

            return default;
        }

        public static IReadOnlyList<T> CustomSkip<T>(this IEnumerable<T> input, int count)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            DKList<T> values = new();
            for (int i = count; i < input.CustomCount(); i++)
                values.Add(input.CustomElementAt(i));

            return values;
        }

        public static int CustomCount<T>(this IEnumerable<T> input)
        {
            int i = 0;
            foreach (var item in input)
                i++;

            return i;
        }

        public static string CustomTrimStart(this string input, char charToTrim)
        {
            string value = "";
            for (int i = 0; i < input.Length; i++)
                if (input[i] != charToTrim)
                    value += input[i];
            return value;
        }

        public static string CustomTrimEnd(this string input, char charToTrim)
        {
            string value = "";
            int j = 0;
            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[j] != charToTrim)
                    value += input[j];
                j++;
            }
            return value;
        }

        public static bool CustomEndsWith(this string str, string suffix)
        {
            if (str == null || suffix == null)
                throw new ArgumentNullException(str == null ? nameof(str) : nameof(suffix));

            if (suffix.Length > str.Length)
                return false;

            for (int i = 0; i < suffix.Length; i++)
                if (str[str.Length - i - 1] != suffix[suffix.Length - i - 1])
                    return false;

            return true;
        }

        public static T CustomLast<T>(this IEnumerable<T> source)
        {
            if (source is IList<T> list)
            {
                int count = list.Count;
                if (count > 0)
                    return list[count - 1];
            }
            else
            {
                using IEnumerator<T> e = source.GetEnumerator();
                if (e.MoveNext())
                {
                    T result;
                    do
                        result = e.Current;
                    while (e.MoveNext());

                    return result;
                }
            }
            throw new InvalidOperationException("Sequence contains no elements");
        }

        public static IEnumerable<T> CustomTake<T>(this IEnumerable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (count <= 0)
                yield break;

            int taken = 0;
            foreach (T item in source)
            {
                if (taken < count)
                {
                    yield return item;
                    taken++;
                }
                else
                    yield break;
            }
        }

        public static TSource? CustomFirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            foreach (TSource? item in source)
                if (predicate(item))
                    return item;

            return default;
        }

        //In C#, the checked keyword is used to explicitly enable overflow checking for arithmetic and conversion operations.
        //This means that if an arithmetic operation results in a value that is outside the range of the data type, an OverflowException will be thrown. 
        public static IEnumerable<TResult> CustomSelect<TSource, TResult>(this IEnumerable<TSource> source,
            Func<TSource, int, TResult> selector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                yield return selector(element, index);
            }
        }

        public static DKDictionary<TKey, TElement> CustomToDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector) where TKey : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));

            DKDictionary<TKey, TElement> dictionary = new();

            foreach (TSource item in source)
            {
                TKey key = keySelector(item);
                TElement value = elementSelector(item);
                // There can be duplicates fix it
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        #endregion

        #region readonly span extensions

        public static ReadOnlySpan<T> CustomSlice<T>(this ReadOnlySpan<T> span, int start, int length) => new(span.CustomToArray(), start, length);

        public static int CustomIndexOf<T>(this ReadOnlySpan<T> span, T value)
        {
            for (int i = 0; i < span.Length; i++)
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    return i;

            return -1;
        }

        public static int CustomIndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value)
        {
            for (int i = 0; i <= span.Length - value.Length; i++)
            {
                ReadOnlySpan<T> slice = span.CustomSlice(i, value.Length);

                if (slice.SequenceEqual(value))
                    return i;
            }

            return -1;
        }

        public static int CustomLastIndexOf<T>(this ReadOnlySpan<T> span, T value)
        {
            //this is not working
            for (int i = span.Length - 1; i >= 0; i++)
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    return i;

            return -1;
        }

        public static ReadOnlySpan<char> CustomTrim(this ReadOnlySpan<char> span)
        {
            int startIndex = 0;
            int endIndex = span.Length - 1;

            while (startIndex <= endIndex && span[startIndex].CustomIsWhiteSpace())
                startIndex++;

            while (endIndex >= startIndex && span[endIndex].CustomIsWhiteSpace())
                endIndex--;

            if (startIndex > endIndex)
                return "";

            return span.CustomSlice(startIndex, endIndex - startIndex + 1);
        }

        public static T[] CustomToArray<T>(this ReadOnlySpan<T> collection)
        {
            T[] result = new T[collection.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = collection[i];

            return result;
        }

        public static bool CustomContains<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        {
            foreach (T item in span)
                if (item.Equals(value))
                    return true;

            return false;
        }

        public static bool CustomContains(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            if (value.IsEmpty)
                return false;

            for (int i = 0; i <= span.Length - value.Length; i++)
                if (span.Slice(i, value.Length).Equals(value, comparisonType))
                    return true;

            return false;
        }

        public static int CustomIndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            if (value.IsEmpty || span.Length < value.Length)
                return -1;

            for (int i = 0; i <= span.Length - value.Length; i++)
                if (span.CustomSlice(i, value.Length).Equals(value, comparisonType))
                    return i;

            return -1;
        }

        public static bool CustomStartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            if (value.Length > span.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
                if (!span[i].Equals(value[i]))
                    return false;

            return true;
        }

        public static bool CustomEndsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>?
        {
            if (value.Length > span.Length)
                return false;

            int startOffset = span.Length - value.Length;
            for (int i = 0; i < value.Length; i++)
                if (!span[startOffset + i].Equals(value[i]))
                    return false;

            return true;
        }

        public static ReadOnlySpan<char> CustomAsSpan(this string? text) => text is null ? ReadOnlySpan<char>.Empty : new ReadOnlySpan<char>(text.CustomToCharArray());

        #endregion
    }
}
