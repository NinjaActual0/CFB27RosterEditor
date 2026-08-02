using Madden26Plugin.Roster;

var rosterPath = args.Length > 0 && File.Exists(args[0])
    ? args[0]
    : @"C:\Users\Ninja\Documents\EA SPORTS College Football 27\saves\ROSTER-test";

if (!File.Exists(rosterPath))
{
    Console.WriteLine($"Usage: Tdb2Dump <path-to-roster-file>");
    Console.WriteLine($"File not found: {rosterPath}");
    return 1;
}

var fileBytes = File.ReadAllBytes(rosterPath);
Console.WriteLine($"file: {rosterPath}");
Console.WriteLine($"len : {fileBytes.Length}");

byte[] inner;
try
{
    inner = Tdb2RosterExtractor.DecompressInner(fileBytes);
    Console.WriteLine($"inner len: {inner.Length}");
}
catch (Exception ex)
{
    Console.WriteLine($"outer deflate failed: {ex.Message}");
    return 1;
}

int playOffset = Tdb2RosterExtractor.FindPlaySubTable(inner);
Console.WriteLine($"PLAY subtable at inner[{playOffset}]");

var players = Tdb2RosterExtractor.ExtractPositions(fileBytes);
Console.WriteLine($"players parsed: {players.Count}");

Console.WriteLine("\n== position distribution ==");
foreach (var g in players.GroupBy(p => p.Position).OrderBy(g => g.Key))
    Console.WriteLine($"  {g.Key,-5} {g.Count()}");

Console.WriteLine("\n== sample players ==");
foreach (var p in players.Take(12))
    Console.WriteLine($"  {p.Name,-28} {p.Position,-5} PGID={p.PlayerId,-8} OVR={p.Overall,-3} SPD={p.Speed,-3} STR={p.Strength,-3} {p.Hometown}");

if (args.Length > 1)
{
    var outPath = args[1];
    using var sw = new StreamWriter(outPath);
    foreach (var p in players)
        sw.WriteLine($"{p.AssetName}\t{p.Ppos}\t{p.Position}\t{p.FirstName}\t{p.Hometown}\t{p.PlayerId}\t{p.Height}\t{p.Weight}\t{p.Overall}\t{p.Speed}\t{p.Strength}");
    Console.WriteLine($"\nwrote {players.Count} rows -> {outPath}");
}

return 0;
