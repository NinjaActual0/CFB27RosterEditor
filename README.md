# CFB27 Roster Editor

A standalone Windows tool (WPF / .NET 10) for editing **EA SPORTS College Football 27** roster
files — no FMT dependency. Main goal: edit player positions (and visual/gear attributes) in
community roster files.

> **Status: WORK IN PROGRESS.** Reading roster files works; reliable position editing and
> save-after-edit do not yet. Details in [Current Status](#current-status) below. Help wanted.

---

## Features

### Reading / UI
- Opens CFB27 roster files (detects the `FBCHUNKS` container, decompresses the outer deflate
  layer, and walks the inner gzip streams).
- **Browse** tab: players grouped by team in a tree.
- **Search** tab: filter by name with live player count.
- **Detail panel** per player:
  - First / last name, jersey number
  - Position, class year, body type
  - Height, weight
  - Face ID, skin tone, hair color, eye color
  - Team
  - Equipment grid (helmets, pads, sleeves, socks, towels, etc.)
- **Dark mode** toggle.

### Editing (in-memory)
- Position, class year, body type, height, weight, face ID, skin/hair/eye color, team, equipment
  values.
- **Assign Random Face** (with configurable tone maps).
- **Bulk Swap Equipment** across selected players.
- **Roster Summary** counts.
- Auto-save to `<file>.autosave` every 3 minutes while modified.

### Export / Generate
- **Export as JSON** / **CSV**.
- **Generate from NCAA website**: scrapers for NCAA official stats, On3, and Sidearm Sports
  (used by the roster-generation flow).

### Debug tools
- **Round-Trip Test**: saves a file with zero edits and diffs it against the original, byte by
  byte, to validate the writer.
- **Raw Strings dump**: hex + extracted strings for the selected player's record.

---

## Current Status

### What works
- Full decompression + gzip-stream split of roster files.
- Player record parsing from embedded ASCII strings (name, team, body type, gear, height byte).
- In-memory edits and the full editing UI.
- Round-trip (no-edit save) is byte-identical.

### What does NOT work yet
1. **Position detection / editing is unreliable.**
   Community roster files (e.g. `ROSTER-TEST2007`) **do not use the FrTk table format**
   (no `SPBF`/`ASTO`/`SPEX` magic found in a full 152 MB decompressed scan). Positions are stored
   as **binary tag–value pairs** in the ~5,170,562-byte C2 trailing data — there are **no ASCII
   position strings** to key off.
   - The `DaiyronW/tool` Python parser is for **Dynasty/RTG saves only**, not roster files.
   - Likely candidate format: **TDB2** (used by Madden 21+ roster files).
2. **Save-after-edit breaks official rosters.** Editing changes the compressed stream sizes; the
   `FBCHUNKS` header offsets/checksums need to be updated on write (currently only the no-edit
   path round-trips byte-identically).

### Next steps (from PROGRESS.md)
1. Diff raw binary C2 data between a **generated roster** (has positions) vs a **community roster**
   to find where positions live.
2. Research the **TDB2** format spec.
3. Check the `cfb27-dynasty-modding` repo for roster format docs.

---

## File Format Notes (so far)

- Container magic: `FBCHUNKS`.
- Outer layer: zlib deflate starting at `0x4A`, compressed to a fixed length (padded), followed by
  the deflate stream.
- Inner layer: a 23-byte container header, then a series of **gzip streams** (`1F 8B 08`), one per
  player record / stats record (records < 100 bytes are treated as stats, larger ones as players).
- Player records embed ASCII strings: `FullId`, first/last name, `Unique_*` / `Generic_*` IDs,
  body type, position, class year, team name, gear tokens.
- Height is stored as a single byte right after the `A2 9B A3 00` marker (`heightByte = inches*2 - 12`).
- Trailing **C2 data** (binary tag–value pairs, ~5 MB) is currently unparsed — believed to hold the
  authoritative per-player fields including positions.

---

## Building from Source

Requires the **.NET 10 SDK**.

```powershell
dotnet build -c Release
```

Publish a self-contained exe:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o .\publish
```

> Note: the `DotNetZip` 1.16.0 package triggers a NU1903 security advisory warning. It's used for
> gzip compression control; replacing it is on the roadmap.

---

## Project layout

```
Roster/
  Views/RosterEditorWindow.xaml(.cs)   # main WPF window + editing logic
  CFB27RosterReader.cs                 # FBCHUNKS deflate + gzip stream extraction + record parsing
  CFB27RosterWriter.cs                 # payload rebuild + gzip-to-size + deflate re-compression
  FranchiseTable.cs / RecordCodec.cs   # FrTk table parser (Dynasty/RTG ONLY — port of DaiyronW/tool)
  PlayerVisualRecipe.cs                # player model (visual + gear fields)
  RosterData.cs                        # parsed roster container
  TeamMap.cs / On3TeamMap.cs / SidearmTeamMap.cs
  NCAAStatsScraper.cs / On3Scraper.cs / SidearmSportsScraper.cs
  ComplexionPresetMapper.cs / FaceTemplateMatcher.cs / HairColorMapper.cs
PROGRESS.md                            # running log of what's done / stuck on
```

> The C# root namespace is `Madden26Plugin` (legacy from the original fork); don't be confused —
> this project targets College Football 27.

---

## How to help

The two blockers above are the best places to contribute:

- **Find the position encoding** in the C2 trailing data (compare generated vs community rosters,
  or decode TDB2).
- **Fix save-after-edit** so the `FBCHUNKS` headers are rebuilt for changed stream sizes.

See [Current Status](#current-status) and `PROGRESS.md` for the full picture. Issues and PRs
welcome.
