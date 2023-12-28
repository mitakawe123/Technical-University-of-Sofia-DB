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
            foreach (IReadOnlyList<char[]> rowValues in columnsValues)
            {
                if (rowValues.Count != columnNameAndType.Count)
                    return false;

                for (int columnIndex = 0; columnIndex < rowValues.Count; columnIndex++)
                {
                    Column column = columnNameAndType[columnIndex];
                    char[] value = rowValues[columnIndex];

                    try
                    {
                        EDataTypes dataType = Enum.Parse<EDataTypes>(column.Type, true);
                        if (!IsValidValue(value, dataType))
                            return false;
                    }
                    catch (ArgumentException)
                    {
                        if (column.Type.CustomStartsWith(EDataTypes.STRING.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            int lengthSpecStart = column.Type.CustomIndexOf('(') + 1;
                            int lengthSpecEnd = column.Type.CustomIndexOf(')');
                            string lengthSpec = column.Type[lengthSpecStart..lengthSpecEnd];
                            int allowedLength = lengthSpec.Equals("max", StringComparison.OrdinalIgnoreCase) ? 4000 : int.Parse(lengthSpec);

                            if (value.Length > allowedLength)
                                return false;
                        }
                        else
                            return false;
                    }
                }
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
