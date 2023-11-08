using DMS.Constants;

namespace DMS.Offset
{
    public static class OffsetManager
    {
        public static bool LoadOffsetMapper()
        {
            try
            {
                if (!File.Exists(Files.OFFSET_FILE_NAME))
                {
                    File.Create(Files.OFFSET_FILE_NAME);
                    return true;
                }

                using FileStream fileStream = new(Files.OFFSET_FILE_NAME, FileMode.Open);
                using BinaryWriter binaryWriter = new(fileStream);

                binaryWriter.Seek(0, SeekOrigin.Begin);

                return true;
            }
            catch (Exception)
            {
                throw new Exception("Invalid operation");
            }
        }
    }
}
