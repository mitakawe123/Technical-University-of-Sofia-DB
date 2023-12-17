using DMS.Constants;
using DMS.DataPages;
using System.Text;

namespace DMS.DataRecovery
{
    //The hash for data corruption will be the first thing in every 8KB sector 
    public static class FileIntegrityChecker
    {
        private const int SectionSize = DataPageManager.DataPageSize; // 8KB

        public static ulong DefaultHashValue => 0;
        public static int HashSize => 8;

        public static bool CheckForCorruptionOnStart()
        {
            if (DataPageManager.AllDataPagesCount == 0)
                return false;

            (FileStream fs, BinaryReader reader) = OpenFileAndRead();

            fs.Seek(DataPageManager.CounterSection, SeekOrigin.Begin);

            for (int i = 0; i < DataPageManager.AllDataPagesCount; i++)
            {
                long end = fs.Position + SectionSize - HashSize;
                long bytesToRead = end - fs.Position;
                byte[] buffer = new byte[bytesToRead];

                ulong hash = reader.ReadUInt64();

                int bytesRead = fs.Read(buffer, 0, (int)bytesToRead);

                ulong currentHash = Hash.ComputeHash(buffer);

                bool compareHashes = CompareHashes(hash, currentHash);
                if (!compareHashes)
                {
                    CloseFileAndRead(fs,reader);
                    return true;
                }
            }

            CloseFileAndRead(fs, reader);
            return false;
        }

        public static void RecalculateHash(FileStream fs, BinaryWriter writer, long startingPosition)
        {
            fs.Seek(startingPosition + HashSize, SeekOrigin.Begin);

            long end = startingPosition + SectionSize - HashSize;
            long bytesToRead = end - startingPosition;
            byte[] buffer = new byte[bytesToRead];

            int bytesRead = fs.Read(buffer, 0, (int)bytesToRead);

            ulong hash = Hash.ComputeHash(buffer);

            fs.Seek(startingPosition, SeekOrigin.Begin);

            writer.Write(hash);
        }

        private static bool CompareHashes(ulong hash1, ulong hash2) => hash1 == hash2;
        
        private static (FileStream, BinaryReader) OpenFileAndRead()
        {
            FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryReader reader = new(fileStream, Encoding.UTF8);
            return (fileStream, reader);
        }

        private static void CloseFileAndRead(FileStream fs, BinaryReader reader)
        {
            fs.Close();
            reader.Close();
        }
    }
}
