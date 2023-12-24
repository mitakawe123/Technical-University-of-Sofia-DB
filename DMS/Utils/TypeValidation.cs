using DMS.Constants;
using DMS.Extensions;
using DMS.Shared;
using System.Globalization;

namespace DMS.Utils
{
    public static class TypeValidation
    {
        //('2023-11-03'); -- ISO format
        //('11/03/2023'); -- U.S.format
        //('03/11/2023'); -- European format
        private static readonly string[] Formats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy" };

        public static bool CheckIfValidColumnType(ReadOnlySpan<char> type)
        {
            if (type.SequenceEqual(EDataTypes.DATE.ToString().CustomToLower())
                || type.SequenceEqual(EDataTypes.INT.ToString().CustomToLower()))
                return true;

            if (type[..6].SequenceEqual(EDataTypes.STRING.ToString().CustomToLower()))
                return true;

            return false;
        }

        public static bool CheckValidityOfColumnValuesBasedOnType(IReadOnlyList<Column> columnNameAndType, IReadOnlyList<IReadOnlyList<char[]>> columnsValues)
        {
            for (int i = 0; i < columnNameAndType.Count; i++)
            {
                Column column = columnNameAndType[i];
                int indexSnapshot = i;

                for (int j = 0; j < columnNameAndType.Count; j++)
                {
                    IReadOnlyList<char[]> values = columnsValues[j];
                    char[] value = values[indexSnapshot];

                    try
                    {
                        EDataTypes dataType = Enum.Parse<EDataTypes>(column.Type, true);
                        if (!IsValidValue(value, dataType))
                            return false;
                    }
                    catch (ArgumentException)
                    {
                        // This block is executed if Enum.Parse throws an error.
                        if (column.Type[..6] == EDataTypes.STRING.ToString().CustomToLower())
                        {
                            int indexOfOpeningBracket = column.Type.CustomIndexOf('(');
                            int indexOfClosingBracket = column.Type.CustomIndexOf(')');

                            string allowedLengthForString = column.Type[(indexOfOpeningBracket + 1)..indexOfClosingBracket];
                            int allowedLength = allowedLengthForString == "max" ? 4000 : int.Parse(allowedLengthForString);

                            if (value.Length > allowedLength)
                                return false;
                        }
                        else
                            return false;
                    }

                    i++;
                }

                i = indexSnapshot;
            }

            return true;
        }

        private static bool IsValidValue(char[] value, EDataTypes type)
        {
            return type switch
            {
                EDataTypes.INT => int.TryParse(value, out _),
                EDataTypes.DATE => DateTime.TryParseExact(value, Formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _),
                _ => true
            };
        }
    }
}
