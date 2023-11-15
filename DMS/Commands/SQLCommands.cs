using DataStructures;
using DMS.Constants;
using DMS.DataPages;
using DMS.Shared;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DMS.Commands
{
    public static class SQLCommands
    {
        //createtable test(id int primary key, name string(max) null)
        //insert into test (id, name) values (1, 'hellot123'), (2, 'test2main'), (3, 'test3')
        public static void InsertIntoTable(IReadOnlyList<IReadOnlyList<char[]>> columnsValues, ReadOnlySpan<char> tableName)
        {
            Dictionary<char[], long> offset = DataPageManager.TableOffsets;

            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            using BinaryReader reader = new(fileStream, Encoding.UTF8);

            char[]? matchingKey = null;

            foreach (KeyValuePair<char[], long> item in offset)
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
            ulong recordSizeInBytes = reader.ReadUInt64();//<- validation purposes only
            int tableLength = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(tableLength);
            string table = Encoding.UTF8.GetString(bytes);
            int columnCount = reader.ReadInt32();

            DKList<Column> columnNameAndType = new();

            for (int i = 0; i < columnCount; i++)
            {
                string columnName = reader.ReadString();
                string columnType = reader.ReadString();
                columnNameAndType.Add(new Column(columnName, columnType));
            }

            long currentPosition = fileStream.Position;

            fileStream.Close();
            reader.Close();

            using FileStream fs = new(Files.MDF_FILE_NAME, FileMode.Open);// <- initiate new file stream because the old one is not writable even through I gave full permissions for the stream
            using BinaryWriter writer = new(fs, Encoding.UTF8);

            fs.Seek(currentPosition, SeekOrigin.Begin);

            byte[] allRecords = GetAllData(columnsValues);
            InsertIntoFreeSpace(fs, writer, allRecords, freeSpace);
        }

        private static void InsertIntoFreeSpace(FileStream fs, BinaryWriter writer, byte[] allRecords, int freeSpaceInCurrentPage)
        {
            if (allRecords.Length <= freeSpaceInCurrentPage - DataPageManager.BufferOverflowPointer)
            {
                writer.Write(allRecords, 0, (int)(freeSpaceInCurrentPage - DataPageManager.BufferOverflowPointer));
                return;
            }

            int totalLength = allRecords.Length;
            int writtenBytes = 0;
            int initialFreeSpaceInNotMainDP = 8180;
            while (writtenBytes < totalLength)
            {
                //first page to write
                writer.Write(allRecords, 0, (int)(freeSpaceInCurrentPage - DataPageManager.BufferOverflowPointer));
                writtenBytes += (int)(freeSpaceInCurrentPage - DataPageManager.BufferOverflowPointer);

                fs.Seek(DataPageManager.AllDataPagesCount * DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                long pointer = fs.Read(new byte[8], 0, 8);//<- pointer to next page

                if (pointer == DataPageManager.DefaultBufferForDP)
                {
                    fs.Seek(DataPageManager.AllDataPagesCount * DataPageManager.DataPageSize - DataPageManager.BufferOverflowPointer, SeekOrigin.Begin);
                    writer.Write((DataPageManager.AllDataPagesCount + 1) * DataPageManager.DataPageSize);

                    DataPageManager.AllDataPagesCount++;
                    DataPageManager.DataPageCounter++;

                    fs.Seek(DataPageManager.AllDataPagesCount * DataPageManager.DataPageSize, SeekOrigin.Begin);

                    writer.Write(initialFreeSpaceInNotMainDP);
                }
                else
                {
                    fs.Seek(pointer, SeekOrigin.Begin);
                    int freeSpace = fs.Read(new byte[4], 0, 4);//<- free space in data page

                    //Here whats need to happend
                    //While there is bytes left i need to find empty data pages / or create new ones to fill the bytes
                    /*if (freeSpace >= totalLength - writtenBytes)
                    {
                        writer.Write(allRecords, writtenBytes, totalLength - writtenBytes);
                        return;
                    }
                    else
                    {
                        writer.Write(allRecords, writtenBytes, freeSpace);
                        writtenBytes += freeSpace;
                    }*/
                }
            }
        }

        private static byte[] GetAllData(IReadOnlyList<IReadOnlyList<char[]>> columnsValues)
        {
            int totalSize = 0;
            foreach (IReadOnlyList<char[]> column in columnsValues)
            {
                foreach (char[] charArray in column)
                {
                    totalSize += sizeof(int); // Add 4 bytes for the length of char[]
                    totalSize += Encoding.UTF8.GetByteCount(charArray);
                }
            }

            byte[] allBytes = new byte[totalSize];

            int currentPosition = 0;
            foreach (IReadOnlyList<char[]> column in columnsValues)
            {
                foreach (char[] charArray in column)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(charArray);
                    byte[] lengthPrefix = BitConverter.GetBytes(byteArray.Length);

                    lengthPrefix.CopyTo(allBytes, currentPosition);
                    currentPosition += sizeof(int);

                    byteArray.CopyTo(allBytes, currentPosition);
                    currentPosition += byteArray.Length;
                }
            }

            return allBytes;
        }

        private static IReadOnlyList<IReadOnlyList<char[]>> ReadAllData(byte[] allBytes)//<- this will come in handy for the Select 
        {
            DKList<DKList<char[]>> columnsValues = new();
            int currentPosition = 0;

            while (currentPosition < allBytes.Length)
            {
                DKList<char[]> column = new();

                while (currentPosition < allBytes.Length)
                {
                    // Read the length of the next char[]
                    int charArrayLength = BitConverter.ToInt32(allBytes, currentPosition);
                    currentPosition += sizeof(int);

                    byte[] byteArray = new byte[charArrayLength];
                    Array.Copy(allBytes, currentPosition, byteArray, 0, charArrayLength);
                    currentPosition += charArrayLength;

                    char[] charArray = Encoding.UTF8.GetChars(byteArray);
                    column.Add(charArray);
                }

                columnsValues.Add(column);
            }

            return columnsValues.AsReadOnly();
        }
    }
}
