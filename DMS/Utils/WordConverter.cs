using DMS.Extensions;

namespace DMS.Utils
{
    public static class WordConverter
    {
        public static long ConvertWordToNumber(string word)
        {
            long result = 0;
            int power = 0;

            foreach (char c in word.CustomReverse())
            {
                int value = c - 'a' + 1;
                result += value * (long)Math.Pow(27, power);
                power++;
            }

            return result;
        }

        public static string ConvertNumberToWord(long number)
        {
            string word = "";
            while (number > 0)
            {
                number--;
                word = (char)('a' + number % 27) + word;
                number /= 27;
            }

            return word;
        }
    }
}
