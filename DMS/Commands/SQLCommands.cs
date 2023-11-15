using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Extensions;
using DMS.Shared;
using DMS.Utils;
using System.Text;

namespace DMS.Commands
{
    public static class SQLCommands
    {
        //createtable test(id int primary key, name string(max) null)
        //insert into test (id, name) values (1, 'test'), (2, 'test2'), (3, 'test3')
        public static void InsertIntoTable(IReadOnlyList<IReadOnlyList<char[]>> columnsValues, ReadOnlySpan<char> tableName)
        {
            //1. Get all the data pages and the info about the table
            //2. Check if data page has enough space to insert the data
            //3. If not, create a new data page and insert the data 
            //3.5 !!! Dont forget to add pointer to the next page in the previous page !!!
            //4. If yes, insert the data
            Dictionary<char[], long> offset = DataPageManager.TableOffsets;

            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fileStream, Encoding.UTF8);

            char[]? matchingKey = null;

            foreach (var item in offset)
            {
                if (tableName.SequenceEqual(item.Key))
                {
                    matchingKey = item.Key;
                    break;
                }
            }

            if (matchingKey is null || !offset.ContainsKey(matchingKey))
                throw new Exception("Cannot find table");

            fileStream.Seek(offset[matchingKey], SeekOrigin.Begin);

            //20 bytes + 1 bytes per char for table
            int freeSpace = reader.ReadInt32();
            ulong recordSizeInBytes = reader.ReadUInt64();
            int tableLength = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(tableLength);
            string table = Encoding.UTF8.GetString(bytes);
            int columnCount = reader.ReadInt32();

            ulong currentFreeSpace = (ulong)freeSpace;

            DKList<Column> columnNameAndType = new();

            for (int i = 0; i < columnCount; i++)
            {
                string columnName = reader.ReadString();
                string columnType = reader.ReadString();
                columnNameAndType.Add(new Column(columnName, columnType));
            }

            reader.Close();

            int dataPagesNeeded = HelperAllocater.NumberOfDataPagesForInsert(columnsValues.Count, recordSizeInBytes);

            //Not tested
            if (dataPagesNeeded == 1)
                InsertData(fileStream, recordSizeInBytes, columnsValues, columnNameAndType, ref currentFreeSpace);

            for (int i = 0; i < dataPagesNeeded; i++)
            {
                InsertData(fileStream, recordSizeInBytes, columnsValues, columnNameAndType, ref currentFreeSpace);
                fileStream.Seek(DataPageManager.AllDataPagesCount + DataPageManager.CounterSection, SeekOrigin.Begin);
                DataPageManager.AllDataPagesCount++;
                DataPageManager.DataPageCounter++;
            }
        }

        private static void InsertData(
            FileStream fileStream,
            ulong recordSizeInBytes,
            IReadOnlyList<IReadOnlyList<char[]>> columnsValues,
            IReadOnlyList<Column> columnNameAndType,
            ref ulong currentFreeSpace)
        {
            using BinaryWriter writer = new(fileStream);

            while (currentFreeSpace + DataPageManager.BufferOverflowPointer > recordSizeInBytes)
            {
                currentFreeSpace -= recordSizeInBytes;

                //here write the data
                if (columnsValues.CustomAny(x => x.Count != columnNameAndType.Count))
                    throw new Exception("Invalid number of columns");

                foreach (IReadOnlyList<char[]> value in columnsValues)
                {
                    foreach (char[] val in value)
                    {
                        //write the value length and the value itself this is 4 bytes + 1 byte per char
                        writer.Write(val.Length);
                        writer.Write(val);
                    }
                }
            }
        }
    }
}
