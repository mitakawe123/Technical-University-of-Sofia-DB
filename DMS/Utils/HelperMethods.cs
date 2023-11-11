namespace DMS.Utils
{
    public static class HelperMethods
    {
        public static bool CustomExists<T>(T[] array, Predicate<T> match) => FindIndex(array, 0, array.Length, match) != -1;

        public static int FindIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
                if (match(array[i]))
                    return i;
            
            return -1;
        }

        public static int Get7BitEncodedIntSize(int value)
        {
            int size = 0;
            uint num = (uint)value;
            while (num >= 0x80)
            {
                num >>= 7;
                size++;
            }
            size++;
            return size;
        }
    }
}
