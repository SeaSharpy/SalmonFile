namespace Salmon;
using System.Security.Cryptography;
/// <summary>Creates identifiers for SALMON data.</summary>
public static class Snowflake
{
    private const int SequenceBits = 12;
    private const int SequenceMask = (1 << SequenceBits) - 1;
    private static readonly object Sync = new();
    private static readonly long EpochMilliseconds =
        new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
    private static long LastTimestamp;
    private static int Sequence;
    /// <summary>Creates a numeric identifier suitable for an uploadable level.</summary>
    /// <returns>A string containing only decimal digits.</returns>
    public static string Create()
    {
        ulong value = CreateULong();
        string valueText = value.ToString();

        int prefixDigits = 32 - valueText.Length;
        if (prefixDigits <= 0)
            return valueText;

        Span<char> prefix = stackalloc char[prefixDigits];
        for (var i = 0; i < prefixDigits; i++)
            prefix[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));

        return $"{prefix.ToString()}{valueText}";
    }
    internal static ulong CreateULong()
    {
        lock (Sync)
        {
            var timestamp = GetTimestampMilliseconds();
            if (timestamp < LastTimestamp)
                timestamp = LastTimestamp;

            if (timestamp == LastTimestamp)
            {
                Sequence = (Sequence + 1) & SequenceMask;
                if (Sequence == 0)
                    timestamp = WaitForNextTimestamp(LastTimestamp);
            }
            else
            {
                Sequence = 0;
            }

            LastTimestamp = timestamp;

            var value = ((ulong)timestamp << SequenceBits) | (uint)Sequence;
            return value;
        }
    }
    private static long GetTimestampMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - EpochMilliseconds;

    private static long WaitForNextTimestamp(long previousTimestamp)
    {
        long timestamp;
        do
            timestamp = GetTimestampMilliseconds();
        while (timestamp <= previousTimestamp);

        return timestamp;
    }
}
