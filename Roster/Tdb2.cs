using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Madden26Plugin.Roster;

public static class SixBit
{
    public static string Decode(byte[] data)
    {
        var sb = new StringBuilder();
        int totalBits = data.Length * 8;
        for (int off = 0; off < totalBits; off += 6)
            sb.Append((char)(GetBits(data, off, 6) + 32));
        return sb.ToString();
    }

    static int GetBits(byte[] data, int offset, int length)
    {
        int value = 0;
        for (int i = 0; i < length; i++)
        {
            int bitIndex = offset + i;
            int byteIndex = bitIndex >> 3;
            int bitInByte = 7 - (bitIndex & 7);
            value = (value << 1) | ((data[byteIndex] >> bitInByte) & 1);
        }
        return value;
    }

    public static byte[] Compress(string name)
    {
        var bits = name.Select(c =>
        {
            if (c >= 'A' && c <= 'Z') return c - 32;
            if (c >= '0' && c <= '9') return c - 32;
            throw new Exception("unsupported char " + c);
        }).ToArray();
        byte b1 = (byte)((bits[0] << 2) | (bits[1] >> 4));
        byte b2 = (byte)(((bits[1] & 0xF) << 4) | (bits[2] >> 2));
        byte b3 = (byte)(((bits[2] & 0x3) << 6) | bits[3]);
        return new byte[] { b1, b2, b3 };
    }
}

public static class Leb
{
    public static long ReadModifiedLeb(byte[] buf)
    {
        long value = 0;
        bool isNegative = false;
        for (int i = buf.Length - 1; i >= 0; i--)
        {
            long currentByte = buf[i];
            if (i != buf.Length - 1) currentByte ^= 0x80;
            if (i == 0 && (currentByte & 0x40) == 0x40)
            {
                currentByte ^= 0x40;
                isNegative = true;
            }
            long factor = 1L << (i * 6);
            if (i > 1) factor <<= 1;
            value += currentByte * factor;
        }
        return isNegative ? -value : value;
    }
}

public class Reader
{
    public byte[] B;
    public int Pos;
    public Reader(byte[] b, int pos = 0) { B = b; Pos = pos; }

    public byte ReadByte() { if (Pos >= B.Length) throw new Exception($"EOF at {Pos}"); return B[Pos++]; }
    public byte Peek() { return B[Pos]; }

    public byte[] Read(int n)
    {
        if (Pos + n > B.Length) throw new Exception($"EOF reading {n} at {Pos}");
        var r = new byte[n];
        Array.Copy(B, Pos, r, 0, n);
        Pos += n;
        return r;
    }

    public long ReadLeb()
    {
        var list = new List<byte>();
        byte cur;
        do { cur = ReadByte(); list.Add(cur); } while ((cur & 0x80) != 0);
        return Leb.ReadModifiedLeb(list.ToArray());
    }

    public long ReadLeb(byte first)
    {
        var list = new List<byte> { first };
        byte cur = first;
        while ((cur & 0x80) != 0) { cur = ReadByte(); list.Add(cur); }
        return Leb.ReadModifiedLeb(list.ToArray());
    }
}

public class FieldDef
{
    public string Name;
    public int Type;
    public int Count;
}

public class TableInfo
{
    public string Name;
    public int Type;
    public int Unknown1;
    public int Unknown2 = -1;
    public int Offset;
    public long NumEntries;
    public bool IsSubTable;
    public Dictionary<string, FieldDef> Fields = new Dictionary<string, FieldDef>();
}

public class Tdb2ParseResult
{
    public List<TableInfo> Tables = new List<TableInfo>();
    public int TablesParsed;
    public long RecordsParsed;
    public int CompressedErrors;
    public int ErrorOffset = -1;
    public string Error;
}

public class Tdb2Parser
{
    readonly Reader _p;
    readonly Tdb2ParseResult _res;
    readonly List<string> _dumpTableNames;
    readonly int _dumpLimit = 60;
    readonly Dictionary<string, int> _dumpCounts = new Dictionary<string, int>();
    readonly List<Dictionary<string, string>> _playRecords = new List<Dictionary<string, string>>();

    public Tdb2Parser(Reader p, Tdb2ParseResult res, List<string> dumpTableNames)
    {
        _p = p;
        _res = res;
        _dumpTableNames = dumpTableNames;
    }

    public void Run(int maxTables)
    {
        try
        {
            int lastPos = -1;
            while (_res.TablesParsed < maxTables)
            {
                if (_p.Pos == lastPos) break;
                lastPos = _p.Pos;
                ParseTableHeader();
            }
        }
        catch (Exception ex)
        {
            _res.Error = ex.Message;
            _res.ErrorOffset = _p.Pos;
        }
    }

    void AddField(TableInfo t, string name, int type)
    {
        if (!t.Fields.TryGetValue(name, out var fd))
        {
            fd = new FieldDef { Name = name, Type = type };
            t.Fields[name] = fd;
        }
        fd.Count++;
    }

    bool ShouldDump(string name)
    {
        if (_dumpTableNames == null || !_dumpTableNames.Contains(name)) return false;
        if (!_dumpCounts.ContainsKey(name)) _dumpCounts[name] = 0;
        return _dumpCounts[name] < _dumpLimit;
    }

    void BumpDump(string name)
    {
        if (_dumpCounts.ContainsKey(name)) _dumpCounts[name]++;
    }

    void ParseTableHeader()
    {
        int offset = _p.Pos;
        var rawKey = _p.Read(5);
        var t = new TableInfo
        {
            Name = SixBit.Decode(rawKey[0..3]),
            Type = rawKey[3],
            Unknown1 = rawKey[4],
            Offset = offset
        };

        var b0 = _p.ReadByte();
        bool keyed = false;

        if (t.Type == 5)
        {
            t.Unknown2 = b0;
            t.NumEntries = _p.ReadLeb();
            keyed = true;
        }
        else if (t.Type == 3)
        {
            _p.Read(3);
            var b1 = _p.ReadByte();
            t.NumEntries = _p.ReadLeb(b1);
        }
        else
        {
            t.NumEntries = _p.ReadLeb(b0);
        }

        _res.Tables.Add(t);
        _res.TablesParsed++;

        bool dump = ShouldDump(t.Name);

        for (long i = 0; i < t.NumEntries; i++)
        {
            var fields = new Dictionary<string, string>();
            if (keyed)
            {
                long index = _p.ReadLeb();
                if (t.Unknown2 == 2)
                {
                    long byteCount = _p.ReadLeb();
                    var gz = _p.Read((int)byteCount);
                    fields = ParseCompressedRecord(gz, t, index);
                }
                else
                {
                    ParseRecordFields(t, fields, null);
                }
            }
            else
            {
                ParseRecordFields(t, fields, null);
            }
            _res.RecordsParsed++;
            if (dump && _dumpCounts.TryGetValue(t.Name, out int cnt) && cnt < _dumpLimit)
            {
                Console.WriteLine($"    [{i}] {string.Join("  ", fields.Select(kv => $"{kv.Key}={kv.Value}"))}");
                BumpDump(t.Name);
            }
        }

        if (_playRecords.Count > 0)
        {
            Console.WriteLine($"\n== PLAY subtable records ({_playRecords.Count} captured) ==");
            foreach (var rec in _playRecords)
            {
                var parts = new List<string>();
                foreach (var keep in new[] { "PPOS", "TGID", "PGID", "PEPS", "PFNA", "PHTN" })
                    if (rec.TryGetValue(keep, out var v)) parts.Add($"{keep}={v}");
                Console.WriteLine("  " + string.Join("  ", parts));
            }
        }
    }

    public List<Dictionary<string, string>> ParseSubTableRecords(long count, string tableName)
    {
        var t = new TableInfo { Name = tableName, Type = 4, IsSubTable = true };
        var list = new List<Dictionary<string, string>>();
        for (long i = 0; i < count && _p.Pos < _p.B.Length; i++)
        {
            var sub = new Dictionary<string, string>();
            try { ParseRecordFields(t, sub, null); }
            catch (Exception ex)
            {
                _res.CompressedErrors++;
                if (_res.CompressedErrors <= 5)
                    Console.WriteLine($"[err] PLAY record {i}: {ex.Message}");
                break;
            }
            list.Add(sub);
        }
        return list;
    }

    void CheckRecordEnd(TableInfo t, Dictionary<string, string> fields)
    {
        byte b = _p.ReadByte();
        if (b != 0x00)
            ParseRecordFields(t, fields, new byte[] { b });
    }

    void ParseRecordFields(TableInfo t, Dictionary<string, string> fields, byte[] startBuf)
    {
        int need = 5 - (startBuf?.Length ?? 0);
        var buf = (startBuf ?? Array.Empty<byte>()).Concat(_p.Read(need)).ToArray();

        var rawKey = buf[0..4];
        string key = SixBit.Decode(rawKey[0..3]);
        byte type = rawKey[3];
        AddField(t, key, type);

        switch (type)
        {
            case 0:
                {
                    long v = _p.ReadLeb(buf[4]);
                    fields[key] = v.ToString();
                    if (key == "UNWI" || key == "TREF")
                        _p.ReadByte();
                    CheckRecordEnd(t, fields);
                    break;
                }
            case 1:
                {
                    long len = buf[4] >= 0x80 ? _p.ReadLeb(buf[4]) : buf[4];
                    var raw = _p.Read((int)len);
                    fields[key] = Encoding.UTF8.GetString(raw).TrimEnd('\0');
                    CheckRecordEnd(t, fields);
                    break;
                }
            case 3:
                {
                    var b = _p.ReadByte();
                    ParseRecordFields(t, fields, buf[4..].Concat(new byte[] { b }).ToArray());
                    break;
                }
            case 4:
                {
                    var b1 = _p.ReadByte();
                    long numEntries = _p.ReadLeb(b1);
                    for (long i = 0; i < numEntries; i++)
                    {
                        var sub = new Dictionary<string, string>();
                        ParseRecordFields(t, sub, null);
                        if (key == "PLAY" && _playRecords.Count < _dumpLimit)
                            _playRecords.Add(sub);
                    }
                    fields[key] = $"(subtable {numEntries})";
                    CheckRecordEnd(t, fields);
                    break;
                }
            case 5:
                {
                    var b1 = _p.ReadByte();
                    int unknown2 = b1;
                    var b2 = _p.ReadByte();
                    long numEntries = _p.ReadLeb(b2);
                    var st = new TableInfo { Name = key, Type = 5, Unknown1 = buf[4], Unknown2 = unknown2, Offset = _p.Pos, NumEntries = numEntries, IsSubTable = true };
                    if (key == "BLBM")
                        Console.WriteLine($"[dbg] BLBM subtable: unk1={buf[4]} unk2={unknown2} numEntries={numEntries} at pos={st.Offset}");
                    for (long i = 0; i < numEntries; i++)
                    {
                        long index = _p.ReadLeb();
                        if (unknown2 == 2)
                        {
                            long byteCount = _p.ReadLeb();
                            var gz = _p.Read((int)byteCount);
                            try
                            {
                                var recFields = ParseCompressedRecord(gz, st, index);
                                if (ShouldDump(st.Name))
                                {
                                    Console.WriteLine($"    [{i}] idx={index} {string.Join("  ", recFields.Select(kv => $"{kv.Key}={kv.Value}"))}");
                                    BumpDump(st.Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                _res.CompressedErrors++;
                                if (_res.CompressedErrors <= 10)
                                {
                                    Console.WriteLine($"[err] BLBM record idx={index} size={byteCount}: {ex.Message}");
                                    using (var ms = new MemoryStream(gz))
                                    using (var gs = new GZipStream(ms, CompressionMode.Decompress))
                                    using (var outMs = new MemoryStream())
                                    {
                                        gs.CopyTo(outMs);
                                        var d = outMs.ToArray();
                                        Console.WriteLine("      dec hex: " + string.Join(" ", d[0..Math.Min(64, d.Length)].Select(x => x.ToString("X2"))));
                                    }
                                }
                            }
                        }
                        else
                        {
                            var sub = new Dictionary<string, string>();
                            ParseRecordFields(st, sub, null);
                        }
                    }
                    fields[key] = $"(subtable-c {numEntries})";
                    CheckRecordEnd(t, fields);
                    break;
                }
            case 10:
                {
                    var raw = buf[4..].Concat(_p.Read(3)).ToArray();
                    fields[key] = BitConverter.ToSingle(raw, 0).ToString("R");
                    CheckRecordEnd(t, fields);
                    break;
                }
            default:
                throw new Exception($"Unsupported field type 0x{type:X2} for key '{key}' at {_p.Pos}");
        }
    }

    Dictionary<string, string> ParseCompressedRecord(byte[] gz, TableInfo t, long index)
    {
        byte[] rec;
        using (var ms = new MemoryStream(gz))
        using (var gs = new GZipStream(ms, CompressionMode.Decompress))
        using (var outMs = new MemoryStream())
        {
            gs.CopyTo(outMs);
            rec = outMs.ToArray();
        }

        var p = new Reader(rec, 0);
        p.Read(4);
        p.Read(4);
        var result = new Dictionary<string, string>();

        if (t.Name == "BLBM" || t.Name == "BLOB")
        {
            if (p.Peek() != 0x00 && p.Peek() != 0x8E)
            {
                var sub = new Dictionary<string, string>();
                ParseDecompressedFields(p, t, sub, isSubRecord: true);
                foreach (var kv in sub)
                    result["SUBR_" + kv.Key] = kv.Value;
                if (p.Peek() == 0x00)
                    p.ReadByte();
                else if (p.Peek() != 0x8E && p.Peek() != 0x00)
                    ParseDecompressedFields(p, t, result, isSubRecord: false);
            }
            else
            {
                p.Read(2);
                p.Read(4);
                ParseDecompressedFields(p, t, result, isSubRecord: false);
            }
        }
        else
        {
            ParseDecompressedFields(p, t, result, isSubRecord: false);
        }
        return result;
    }

    void ParseDecompressedFields(Reader p, TableInfo t, Dictionary<string, string> result, bool isSubRecord)
    {
        while (true)
        {
            var rawKey = p.Read(4);
            string key = SixBit.Decode(rawKey[0..3]);
            byte type = rawKey[3];
            AddField(t, key, type);

            switch (type)
            {
                case 0:
                    result[key] = p.ReadLeb().ToString();
                    if ((key == "UNWI" || key == "WRST") && p.Peek() == 0x00)
                        p.ReadByte();
                    break;
                case 1:
                    {
                        long len = p.ReadLeb();
                        var raw = p.Read((int)len);
                        result[key] = Encoding.UTF8.GetString(raw).TrimEnd('\0');
                        break;
                    }
                case 3:
                    if (p.Peek() == 0x00) p.ReadByte();
                    break;
                case 4:
                    {
                        p.ReadByte();
                        long numEntries = p.ReadLeb();
                        var st = new TableInfo { Name = key, Type = 4, Offset = p.Pos, NumEntries = numEntries, IsSubTable = true };
                        for (long i = 0; i < numEntries; i++)
                        {
                            while (p.Peek() != 0x00)
                                ParseCompressedSubTableField(p, st);
                            p.ReadByte();
                        }
                        result[key] = $"(subtable {numEntries})";
                        break;
                    }
                case 10:
                    result[key] = BitConverter.ToSingle(p.Read(4), 0).ToString("R");
                    break;
                default:
                    throw new Exception($"Unsupported decompressed field type 0x{type:X2} key '{key}'");
            }

            if (p.Peek() == 0x00)
            {
                p.ReadByte();
                if (isSubRecord)
                {
                    p.Read(4); // CHVI header
                    return;
                }
                return;
            }
        }
    }

    void ParseCompressedSubTableField(Reader p, TableInfo t)
    {
        var rawKey = p.Read(4);
        string key = SixBit.Decode(rawKey[0..3]);
        byte type = rawKey[3];
        AddField(t, key, type);

        switch (type)
        {
            case 0: p.ReadLeb(); break;
            case 1:
                {
                    long len = p.ReadLeb();
                    p.Read((int)len);
                    break;
                }
            case 3: break;
            case 4:
                {
                    p.ReadByte();
                    long numEntries = p.ReadLeb();
                    var st = new TableInfo { Name = key, Type = 4, Offset = p.Pos, NumEntries = numEntries, IsSubTable = true };
                    for (long i = 0; i < numEntries; i++)
                    {
                        while (p.Peek() != 0x00)
                            ParseCompressedSubTableField(p, st);
                        p.ReadByte();
                    }
                    break;
                }
            case 10: p.Read(4); break;
            default:
                throw new Exception($"Unsupported subtable field type 0x{type:X2} key '{key}'");
        }
    }
}
