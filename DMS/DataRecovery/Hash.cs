namespace DMS.DataRecovery;

public abstract class Hash
{
    // << left shift
    // ^ XOR (0101 ^ 1000 = 1101)
    // >> right shift
    public static ulong ComputeHash(byte[] data)
    {
        ulong hash = 5381;
        int round = 1;

        foreach (byte b in data)
        {
            hash = ((hash << 5) + hash) ^ RotateLeft(b, round);
            hash ^= ReverseBits(b);
            round = (round + 1) % 8;
        }

        return hash;
    }

    private static byte RotateLeft(byte value, int count) => (byte)((value << count) | (value >> (8 - count)));

    private static byte ReverseBits(byte value)
    {
        int reversed = 0;
        for (int i = 0; i < 8; i++)
        {
            reversed = (reversed << 1) | (value & 1);
            value >>= 1;
        }
        return (byte)reversed;
    }
}