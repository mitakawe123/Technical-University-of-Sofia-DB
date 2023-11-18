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

        public static void QuickSort<T>(T[] array, Comparison<T> comparison) => QuickSort(array, 0, array.Length - 1, comparison);

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

        private static void QuickSort<T>(T[] array, int left, int right, Comparison<T> comparison)
        {
            if (left >= right) 
                return;
            
            int pivotIndex = Partition(array, left, right, comparison);
            QuickSort(array, left, pivotIndex - 1, comparison);
            QuickSort(array, pivotIndex + 1, right, comparison);
        }

        private static int Partition<T>(T[] array, int left, int right, Comparison<T> comparison)
        {
            T pivot = array[right];
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                if (comparison(array[j], pivot) <= 0)
                {
                    i++;
                    Swap(ref array[i], ref array[j]);
                }
            }
            Swap(ref array[i + 1], ref array[right]);
            return i + 1;
        }

        private static void Swap<T>(ref T x, ref T y) => (x, y) = (y, x);
    }
}
