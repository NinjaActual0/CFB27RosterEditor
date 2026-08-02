using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Madden26Plugin.Roster;

/// <summary>
/// High-level access to the TDB2 sections of a CFB27 roster file that contain the
/// authoritative per-player attribute records (the "PLAY" subtable in the trailing
/// binary C2 data). This is where player positions (PPOS) live.
///
/// Layout (reverse-engineered, see README "File Format Notes"):
///   outer zlib/deflate at 0x4A  -> inner buffer
///   inner: BLOB table (type 3) -> BLBM subtable (type 5, gzip per-player profile
///   records) -> ... -> flat section -> PLAY subtable (type 4, numEntries = player count).
///   The PLAY subtable is located by its 6-bit key "PLAY" + type byte 0x04
///   (bytes C2 C8 79 04), followed by 1 unknown byte + a modified-LEB entry count.
/// </summary>
public static class Tdb2RosterExtractor
{
    // Six-bit packed key for "PLAY" with type 0x04.
    static readonly byte[] PlayKey = { 0xC2, 0xC8, 0x79, 0x04 };

    /// <summary>PPOS value -> game position abbreviation (CFB27 21-position set).</summary>
    public static readonly IReadOnlyDictionary<string, string> PositionMap = new Dictionary<string, string>
    {
        ["1"] = "HB",  ["2"] = "FB",  ["3"] = "WR",  ["4"] = "TE",  ["5"] = "LT",
        ["6"] = "LG",  ["7"] = "C",   ["8"] = "RG",  ["9"] = "RT",  ["10"] = "LEDG",
        ["11"] = "REDG", ["12"] = "DT", ["13"] = "SAM", ["14"] = "MIKE", ["15"] = "WILL",
        ["16"] = "CB", ["17"] = "FS", ["18"] = "SS", ["19"] = "K", ["20"] = "P"
    };

    /// <summary>Decompresses the outer deflate layer (starts at offset 0x4A, after a 2-byte zlib header).</summary>
    public static byte[] DecompressInner(byte[] fileBytes)
    {
        using var ms = new MemoryStream();
        using var input = new MemoryStream(fileBytes, 0x4A + 2, fileBytes.Length - 0x4A - 2);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        deflate.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>Locates the PLAY subtable header in the inner buffer, or -1 if not found.</summary>
    public static int FindPlaySubTable(byte[] inner)
    {
        for (int i = 0; i <= inner.Length - PlayKey.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < PlayKey.Length; j++)
                if (inner[i + j] != PlayKey[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }

    /// <summary>
    /// Parses every PLAY subtable record starting at <paramref name="playOffset"/> (the offset of the
    /// "PLAY" key bytes). Returns one dictionary per player; each maps field key -> string value
    /// (e.g. PPOS, PGID, PEPS, PFNA, PHTN, POVR, PSPD, PSTR, ...). <paramref name="fieldErrors"/>
    /// reports how many records failed to parse.
    /// </summary>
    public static List<Dictionary<string, string>> ExtractPlayRecords(byte[] inner, int playOffset, out int fieldErrors)
    {
        fieldErrors = 0;
        if (playOffset < 0) return new List<Dictionary<string, string>>();

        var r = new Reader(inner, playOffset);
        r.Read(5);                 // packed "PLAY" key (3) + type byte 0x04 (1) + unknown1 (1)
        long numEntries = r.ReadLeb();

        var res = new Tdb2ParseResult();
        var parser = new Tdb2Parser(r, res, new List<string>());
        var records = parser.ParseSubTableRecords(numEntries, "PLAY");
        fieldErrors = res.CompressedErrors;
        return records;
    }

    /// <summary>Maps a raw PPOS field value to the game position abbreviation; blank/0 means QB.</summary>
    public static string PositionFromPpos(string ppos)
    {
        if (string.IsNullOrEmpty(ppos)) return "QB";
        return PositionMap.TryGetValue(ppos, out var pos) ? pos : "?";
    }

    /// <summary>
    /// Full pipeline: raw roster file -> inner buffer -> PLAY records -> player position rows.
    /// </summary>
    public static List<PlayerPosition> ExtractPositions(byte[] fileBytes)
    {
        var inner = DecompressInner(fileBytes);
        int playOffset = FindPlaySubTable(inner);
        var records = ExtractPlayRecords(inner, playOffset, out _);

        var list = new List<PlayerPosition>(records.Count);
        foreach (var rec in records)
        {
            string G(params string[] keys) => keys.Select(k => rec.TryGetValue(k, out var v) ? v : "").FirstOrDefault() ?? "";
            var ppos = G("PPOS");
            list.Add(new PlayerPosition
            {
                AssetName = G("PEPS"),
                Ppos = ppos,
                Position = PositionFromPpos(ppos),
                FirstName = G("PFNA"),
                Hometown = G("PHTN"),
                PlayerId = ParseLong(G("PGID")),
                Height = G("PHGT"),
                Weight = G("PWGT"),
                Overall = G("POVR"),
                Speed = G("PSPD"),
                Strength = G("PSTR")
            });
        }
        return list;
    }

    static long ParseLong(string s) => long.TryParse(s, out var v) ? v : 0;
}

/// <summary>A single player's position/attribute summary extracted from the PLAY subtable.</summary>
public sealed class PlayerPosition
{
    public string AssetName;
    public string Ppos;
    public string Position;
    public string FirstName;
    public string Hometown;
    public long PlayerId;
    public string Height;
    public string Weight;
    public string Overall;
    public string Speed;
    public string Strength;

    public string Name => string.IsNullOrEmpty(AssetName)
        ? FirstName
        : AssetName.Split('_')[0];
}
