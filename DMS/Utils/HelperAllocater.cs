using DMS.Constants;
using DMS.Extensions;

namespace DMS.Utils
{
    public static class HelperAllocater
    {
        //how much bytes for each data type
        public static ulong[] AllocatedStorageForType(string[] columnTypes, IEnumerable<string> columnValuesSplitted)
        {
            ulong[] allocatedBytes = new ulong[columnTypes.Length];
            for (int i = 0; i < columnTypes.Length; i++)
            {
                columnTypes[i] = columnTypes[i].CustomTrim().CustomToLower();

                if (columnTypes[i] == ESupportedDataTypes.INT.ToString().CustomToLower())
                    allocatedBytes[i] = 4;
                else if (columnTypes[i] == ESupportedDataTypes.DATE.ToString().CustomToLower())
                    allocatedBytes[i] = 3;
                else if (columnTypes[i].Contains(ESupportedDataTypes.STRING.ToString().CustomToLower()))
                {
                    //this is dynamic case so I need to loop over how much chars are there and for each char allocate 2bytes
                    if (columnTypes[i].CustomContains("max"))
                    {
                        //this check is wrong i need to split first by "," and then check for the 4000 chars
                        if (columnValuesSplitted.CustomElementAt(i).Length > 4000)
                            throw new Exception("nvarchar(max) cannot be over 4000 chars long");

                        uint bytes = 0;
                        for (int j = 0; j < columnValuesSplitted.CustomElementAt(i).Length; j++)
                            bytes += 2;

                        allocatedBytes[i] = bytes;
                    }
                    else
                    {
                        int openingBracket = columnTypes[i].CustomIndexOf('(');
                        int closingBracket = columnTypes[i].CustomIndexOf(')');
                        uint charForNvarchar = uint.Parse(columnTypes[i][(openingBracket+1)..closingBracket]);
                        allocatedBytes[i] = charForNvarchar * 2;
                    }
                }

            }

            return allocatedBytes;
        }

        public static ulong AllocatedSpaceForColumnTypes(ulong[] allocatedSpaceForColumnTypes)
        {
            ulong all = 0;
            foreach (ulong item in allocatedSpaceForColumnTypes)
                all += item;
            return all;
        }
    }
}
