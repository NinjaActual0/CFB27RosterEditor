namespace Madden26Plugin.Roster;

public class RosterData
{
    public List<PlayerVisualRecipe> Players { get; set; } = new();
    public List<RosterPlayerStats> StatsRecords { get; set; } = new();
    public List<byte[]> AllDecompressedStreams { get; set; } = new();
    public List<byte[]> AllCompressedStreams { get; set; } = new();
    public byte[] RawDeflatedPayload { get; set; }
    public byte[] ContainerHeader { get; set; }
    public byte[] FbChunksHeader { get; set; }
    public byte[] C2TrailingData { get; set; }

    public FranchiseTable PlayerTable { get; set; }
    public int[] PlayerFieldOffsets { get; set; }

    public string GetPositionForPlayer(int streamIndex)
    {
        if (PlayerTable == null || PlayerFieldOffsets == null)
            return null;
        if (streamIndex < 0 || streamIndex >= PlayerTable.Header.NextRecordToUse)
            return null;
        var record = PlayerTable.GetRecordBytes(streamIndex);
        var posBitOffset = PlayerFieldOffsets[119];
        var posValue = RecordCodec.ReadBits(record, posBitOffset, 6);
        return RecordCodec.DecodePosition(posValue);
    }
}
