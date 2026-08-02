using System.Text;

namespace Madden26Plugin.Roster;

public static class RecordCodec
{
    public static int ReadBits(byte[] record, int bitOffset, int length)
    {
        var value = 0;
        for (var b = 0; b < length; b++)
        {
            var byteIdx = (bitOffset + b) / 8;
            var bitIdx = 7 - ((bitOffset + b) % 8);
            var bit = (record[byteIdx] >> bitIdx) & 1;
            value = (value << 1) | bit;
        }
        return value;
    }

    public static string ReadCStringAt(byte[] buf, int offset)
    {
        var end = offset;
        while (end < buf.Length && buf[end] != 0) end++;
        return Encoding.Latin1.GetString(buf, offset, end - offset);
    }

    public static readonly Dictionary<int, string> PositionE = new()
    {
        { 0, "QB" }, { 1, "HB" }, { 2, "FB" }, { 3, "WR" }, { 4, "TE" },
        { 5, "LT" }, { 6, "LG" }, { 7, "C" }, { 8, "RG" }, { 9, "RT" },
        { 10, "LE" }, { 11, "RE" }, { 12, "DT" },
        { 13, "LOLB" }, { 14, "MLB" }, { 15, "ROLB" },
        { 16, "CB" }, { 17, "FS" }, { 18, "SS" },
        { 19, "K" }, { 20, "P" }, { 21, "LS" },
        { 22, "KR" }, { 23, "PR" }, { 24, "KOS" },
        { 25, "3DRB" }, { 26, "GAD" }, { 27, "PWHB" }, { 28, "SLWR" },
        { 29, "RLE" }, { 30, "RRE" }, { 31, "RDT" }, { 32, "NT" },
        { 33, "SUBLB" }, { 34, "SLCB" },
        { 35, "HC" }, { 36, "OC" }, { 37, "DC" }, { 38, "OWN" },
    };

    public static string DecodePosition(int value)
    {
        return PositionE.TryGetValue(value, out var name) ? name : value.ToString();
    }
}
