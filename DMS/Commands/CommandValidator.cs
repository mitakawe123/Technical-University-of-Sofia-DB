using DMS.Constants;
using DMS.Extensions;

namespace DMS.Commands
{
    public static class CommandValidator
    {
        //remove this hashset and implement your
        private static readonly HashSet<string> AllowedKeywords = new(StringComparer.OrdinalIgnoreCase);

        static CommandValidator()
        {
            foreach (ESQLCommands keyword in Enum.GetValues(typeof(ESQLCommands)))
                AllowedKeywords.Add(keyword.ToString());
        }

        public static bool ValidateQuery(string input)
        {
            string[] queryWords = input.CustomSplit(new[] { ' ', '\t', '\n', '\r' });

            if (queryWords.Length == 0)
                return false;

            string firstWord = queryWords[0].Trim();

            return AllowedKeywords.Contains(firstWord);
        }
    }
}
