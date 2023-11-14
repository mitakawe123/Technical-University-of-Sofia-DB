using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using System.Text;

namespace DMS.Commands
{
    public static class SQLCommands
    {
        //insert into test (id, name) values (1, 'test'), (2, 'test2'), (3, 'test3')
        public static void InsertIntoTable(IReadOnlyList<Column> columns, ReadOnlySpan<char> tableName)
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

            if (matchingKey is null)
                throw new Exception("Cannot find table");

            fileStream.Seek(offset[matchingKey], SeekOrigin.Begin);

            //16 bytes + 2 bytes per char for table
            int freeSpace = reader.ReadInt32();
            ulong recordSizeInBytes = reader.ReadUInt64();
            string table = reader.ReadString();
            int columnCount = reader.ReadInt32();

            ulong currentFreeSpace = (ulong)freeSpace;

            if (columnCount != columns.Count)
                throw new Exception("Column count mismatch");// <- catch the case when there is default value

            for (int i = 0; i < columnCount; i++)
            {
                string columnName = reader.ReadString();
                string columnType = reader.ReadString();
            }

            while (currentFreeSpace + DataPageManager.BufferOverflowPointer > recordSizeInBytes)
            {
                currentFreeSpace -= recordSizeInBytes;


            }
        }
    }
}
