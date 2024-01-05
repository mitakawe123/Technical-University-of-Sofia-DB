using DMS.Constants;

namespace DMS.Shared
{
    public static class TestCorruption
    {
        public static void Change27thByte()
        {
            using FileStream fileStream = new(Files.MDF_FILE_NAME, FileMode.Open, FileAccess.ReadWrite);
            
            fileStream.Position = 26;

            Random random = new ();
            char randomChar = (char)random.Next(32, 126); // ASCII range for printable characters

            fileStream.WriteByte((byte)randomChar);
        }
    }
}
