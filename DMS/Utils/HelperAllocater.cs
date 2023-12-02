using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;

namespace DMS.Utils
{
    public static class HelperAllocater
    {
        //how much bytes for each data type
        public static ulong AllocatedStorageForTypes(IEnumerable<Column> columns)
        {
            ulong allocatedBytes = 0;
            foreach (var column in columns)
                allocatedBytes += CalculateColumnSize(column);

            return allocatedBytes;
        }

        private static ulong CalculateColumnSize(Column column)
        {
            ulong allocatedBytes = 0;
            switch (column.Type)
            {
                case "int":
                    allocatedBytes += 4;
                    break;
                case "date":
                    allocatedBytes += 3;
                    break;
                default:
                {
                    if (column.Type.CustomContains("string"))
                    {
                        int openingBracket = column.Type.CustomIndexOf('(');
                        int closingBracket = column.Type.CustomIndexOf(')');

                        if (column.Type.CustomContains("max"))
                            allocatedBytes += 4000 * 2; // Max length of 4000 characters, 2 bytes per character
                        else if (openingBracket != -1 && closingBracket != -1)
                        {
                            string lengthStr = column.Type.CustomSubstring(openingBracket + 1, closingBracket - openingBracket - 1).CustomTrim();
                            if (uint.TryParse(lengthStr, out uint charForNvarchar))
                                allocatedBytes += charForNvarchar * 2; // Length from the string type, 2 bytes per character
                            else
                                throw new Exception("Invalid length for string type");
                        }
                        else
                            throw new Exception("Invalid format for string type");
                    }

                    break;
                }
            }

            return allocatedBytes;
        }

        public static int SpaceTakenByColumnsDefinitions(IEnumerable<Column> columns) => columns.CustomSum(column => 2 * column.Name.Length + 2 * column.Type.Length);

        public static int SpaceTakenByColumnsDefinition(Column column) => (2 * column.Name.Length) + (2 * column.Type.Length);

        public static ulong CalculateSpaceForInsertRecords(IEnumerable<Column> columns)
        {
            //2 bytes for each char in type and name
            ulong space = 0;
            foreach (Column column in columns)
                space += (ulong)((column.Name.Length * 2) + (column.Type.Length * 2));

            return space;
        }

        public static int NumberOfDataPagesForInsert(int recordCount, ulong recordSizeInBytes)
        {
            double res = (double)recordSizeInBytes * recordCount/DataPageManager.DataPageSize;
            res = Math.Ceiling(res);    
            return (int)res;
        }
    }
}
