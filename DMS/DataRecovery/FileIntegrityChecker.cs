using DMS.DataPages;

namespace DMS.DataRecovery
{
    public class FileIntegrityChecker
    {
        private const int SectionSize = DataPageManager.DataPageSize; // 8KB
        private const int HashSize = 8; // Size of the hash in bytes (depends on your hash function)

        private static bool CompareHashes(ulong hash1, ulong hash2) => hash1 == hash2;
    }

}
