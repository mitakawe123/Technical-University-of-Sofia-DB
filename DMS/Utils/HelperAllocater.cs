﻿using DMS.Extensions;
using DMS.Shared;

namespace DMS.Utils
{
    public static class HelperAllocater
    {
        //how much bytes for each data type
        public static ulong AllocatedStorageForType(IReadOnlyList<Column> columns)
        {
            ulong allocatedBytes = 0;
            for (int i = 0; i < columns.Count; i++)
                allocatedBytes += CalculateColumnSize(columns[i]);

            return allocatedBytes;
        }

        public static ulong CalculateColumnSize(Column column)
        {
            ulong allocatedBytes = 0;
            if (column.Type == "int")
                allocatedBytes += 4;
            else if (column.Type == "date")
                allocatedBytes += 3;
            else if (column.Type.CustomContains("string"))
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

            return allocatedBytes;
        }
    }
}
