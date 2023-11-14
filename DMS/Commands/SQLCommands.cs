using DMS.DataPages;
using DMS.Shared;

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
        }
    }
}
