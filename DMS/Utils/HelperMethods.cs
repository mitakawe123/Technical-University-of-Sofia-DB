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

        public static T[] CustomEmpty<T>() => new T[0];
    }
}
