using System.Text;

namespace Madden26Plugin.Roster;

public class FranchiseTableHeader
{
    public string Name { get; init; }
    public bool IsArray { get; init; }
    public int TableId { get; init; }
    public int UniqueId { get; init; }
    public int RecordCount { get; init; }
    public int Table1Length { get; init; }
    public int Table2Length { get; init; }
    public int Table3Length { get; init; }
    public int RecordSize { get; init; }
    public int RecordCapacity { get; init; }
    public int NumMembers { get; init; }
    public int NextRecordToUse { get; init; }
    public int OffsetStart { get; init; }
    public int Table1Start { get; init; }
    public int Table2Start { get; init; }
    public int Table3Start { get; init; }
    public int HeaderSize { get; init; }
}

public class FranchiseTable
{
    public int AbsoluteStart { get; init; }
    public int AbsoluteEnd { get; init; }
    public FranchiseTableHeader Header { get; init; }
    public byte[] Data { get; init; }

    public byte[] GetRecordBytes(int index)
    {
        if (index < 0 || index >= Header.RecordCount)
            throw new IndexOutOfRangeException($"Record index {index} out of range (0..{Header.RecordCount})");
        var off = Header.Table1Start + index * Header.RecordSize;
        var result = new byte[Header.RecordSize];
        Array.Copy(Data, off, result, 0, Header.RecordSize);
        return result;
    }

    public int[] ReadRawOffsetTable()
    {
        var result = new int[Header.NumMembers];
        for (var i = 0; i < Header.NumMembers; i++)
            result[i] = ReadU32BE(Data, Header.OffsetStart + i * 4);
        return result;
    }

    private static int ReadU32BE(byte[] buf, int offset)
    {
        return (buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3];
    }
}

public static class FranchiseTableParser
{
    private static readonly byte[] MagicSPBF = "SPBF"u8.ToArray();
    private static readonly byte[] MagicASTO = "ASTO"u8.ToArray();
    private static readonly byte[] MagicSPEX = "SPEX"u8.ToArray();

    private const int TableStartOffset = 0x94;
    private const int HeaderStart = 0x80;

    public static List<FranchiseTable> ScanTables(byte[] payload)
    {
        var starts = FindTableStarts(payload);
        starts.Sort();
        var tables = new List<FranchiseTable>();
        for (var i = 0; i < starts.Count; i++)
        {
            var start = starts[i];
            var end = i + 1 < starts.Count ? starts[i + 1] : payload.Length - 8;
            var tableBuf = new byte[end - start];
            Array.Copy(payload, start, tableBuf, 0, end - start);
            try
            {
                var header = ParseTableHeader(tableBuf);
                tables.Add(new FranchiseTable
                {
                    AbsoluteStart = start,
                    AbsoluteEnd = end,
                    Header = header,
                    Data = tableBuf
                });
            }
            catch
            {
            }
        }
        return tables;
    }

    public static FranchiseTable FindTableByName(List<FranchiseTable> tables, string name)
    {
        return tables.Find(t => t.Header.Name == name);
    }

    private static List<int> FindTableStarts(byte[] payload)
    {
        var starts = new List<int>();
        for (var i = 0; i < payload.Length - 3; i++)
        {
            if (MatchesMagic(payload, i, MagicSPBF) ||
                MatchesMagic(payload, i, MagicASTO) ||
                MatchesMagic(payload, i, MagicSPEX))
            {
                var start = i - TableStartOffset;
                if (start >= 0)
                    starts.Add(start);
            }
        }
        return starts;
    }

    private static bool MatchesMagic(byte[] data, int offset, byte[] magic)
    {
        if (offset + magic.Length > data.Length) return false;
        for (var i = 0; i < magic.Length; i++)
            if (data[offset + i] != magic[i]) return false;
        return true;
    }

    private static string ReadCString(byte[] buf, int offset, int limit = 0)
    {
        var end = offset;
        var hi = limit > 0 ? Math.Min(buf.Length, offset + limit) : buf.Length;
        while (end < hi && buf[end] != 0) end++;
        return Encoding.Latin1.GetString(buf, offset, end - offset);
    }

    private static int ReadU32BE(byte[] buf, int offset)
    {
        return (buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3];
    }

    private static FranchiseTableHeader ParseTableHeader(byte[] tableBuf)
    {
        var hs = HeaderStart;
        var name = ReadCString(tableBuf, 0);
        var isArray = name.Contains("[]");

        var tableId = ReadU32BE(tableBuf, hs);
        var uniqueId = ReadU32BE(tableBuf, hs + 4);
        var tableStoreLength = ReadU32BE(tableBuf, hs + 36);

        var headerOffset = hs + 40;
        if (tableStoreLength > 0)
            headerOffset += tableStoreLength;

        var recordCount = ReadU32BE(tableBuf, headerOffset + 8);
        var table1Length = ReadU32BE(tableBuf, headerOffset + 16);
        var table2Length = ReadU32BE(tableBuf, headerOffset + 20);
        var table3Length = ReadU32BE(tableBuf, headerOffset + 24);
        var tableTotalLength = ReadU32BE(tableBuf, headerOffset + 40);
        var recordWords = ReadU32BE(tableBuf, headerOffset + 44);
        var recordCapacity = ReadU32BE(tableBuf, headerOffset + 48);
        var numMembers = ReadU32BE(tableBuf, headerOffset + 52);
        var nextRecordToUse = ReadU32BE(tableBuf, headerOffset + 60);

        var offsetStart = 0xE8 + tableStoreLength;
        var hasSecondTable = tableTotalLength > table1Length;
        var recordSize = recordWords * 4;
        var headerSize = offsetStart;

        int table1Start, table2Start;
        if (!isArray)
        {
            headerSize += numMembers * 4;
            table1Start = headerSize;
            table2Start = headerSize + recordCount * recordSize;
        }
        else
        {
            table1Start = headerSize + recordCount * 4;
            table2Start = table1Start + recordCount * recordSize;
        }

        var hasThirdTable = table3Length > 0;
        var table3Start = table2Start + table2Length;

        return new FranchiseTableHeader
        {
            Name = name,
            IsArray = isArray,
            TableId = tableId,
            UniqueId = uniqueId,
            RecordCount = recordCount,
            Table1Length = table1Length,
            Table2Length = table2Length,
            Table3Length = table3Length,
            RecordSize = recordSize,
            RecordCapacity = recordCapacity,
            NumMembers = numMembers,
            NextRecordToUse = nextRecordToUse,
            OffsetStart = offsetStart,
            Table1Start = table1Start,
            Table2Start = table2Start,
            Table3Start = table3Start,
            HeaderSize = headerSize,
        };
    }
}
