using DMS.Constants;
using DMS.DataPages;
using System.Text;

namespace DMS.DataRecovery
{
    //The hash for data corruption will be the first thing in every 8KB sector 
    public class FileIntegrityChecker
    {
        private const int SectionSize = DataPageManager.DataPageSize; // 8KB
        private const int HashSize = 8;

        public static bool CheckForCorruptionOnStart()
        {
            (FileStream fs, BinaryReader reader) = OpenFileAndRead();

            fs.Seek(DataPageManager.CounterSection, SeekOrigin.Begin);

            for (int i = 0; i < DataPageManager.AllDataPagesCount; i++)
            {
                long numberOfBytesToRead = fs.Position + SectionSize - HashSize;

                ulong hash = reader.ReadUInt64();

                byte[] buffer = reader.ReadBytes((int)numberOfBytesToRead);

                ulong currentHash = Hash.ComputeHash(buffer);

                bool compareHashes = CompareHashes(hash, currentHash);
                if (!compareHashes)
                    return true;
            }

            return false;
        }

        public static void RecalculateHash(long startingPosition, byte[] data)
        {
            ulong hash = Hash.ComputeHash(data);

            (FileStream fs, BinaryWriter writer) = OpenFileAndWrite();

            fs.Seek(startingPosition, SeekOrigin.Begin);

            writer.Write(hash);
        }

        private static (FileStream, BinaryWriter) OpenFileAndWrite()
        {
            FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryWriter writer = new(fileStream, Encoding.UTF8);
            return (fileStream, writer);
        }

        private static (FileStream, BinaryReader) OpenFileAndRead()
        {
            FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open);
            BinaryReader reader = new(fileStream, Encoding.UTF8);
            return (fileStream, reader);
        }

        private static bool CompareHashes(ulong hash1, ulong hash2) => hash1 == hash2;
    }

}
