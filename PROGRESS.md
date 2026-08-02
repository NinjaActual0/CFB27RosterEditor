# CFB27RosterEditor - Progress

## Goal
Standalone Windows tool (no FMT dependency) to edit CFB27 roster files, especially player positions.

## What's Done
- WPF project `CFB27RosterEditor.csproj` targeting `net10.0-windows`, DotNetZip only dependency
- UI: dark mode, team dropdown, body type assignment, debug round-trip test
- Name parsing: filters binary markers `"Hn"` and `"Np"`, extracts team names from strings
- `FranchiseTable.cs` + `RecordCodec.cs`: C# port of `DaiyronW/tool` Python code for reading FrTk table format (SPBF/ASTO/SPEX magic) — **only works for Dynasty/RTG saves, NOT roster files**
- `BinaryRecordHelper.cs`: `ReplacePosition()` method (requires existing position string)

## The Problem
Community roster files (like `ROSTER-TEST2007`) don't use FrTk table format. Full 152MB decompressed scan found zero SPBF/ASTO/SPEX magic. The 5,170,562-byte C2 trailing data is binary tag-value pairs with no ASCII position strings.

The Python tool from `DaiyronW/tool` is for Dynasty saves only.

## What's Needed Next
Figure out how to read position data from community roster files. Likely TDB2 format (used by Madden 21+ roster files). Options:
1. Compare raw binary C2 data between a generated roster (has positions) vs community roster
2. Research TDB2 format spec
3. Check `cfb27-dynasty-modding` repo for roster format docs

## Also Pending
- Save-after-edit breaks official rosters (compressed size changes, FBCHUNKS headers need updating)
- GitHub push: need `gh auth login --web -h github.com` in user's PowerShell
- ComboBox dark-mode popup styling unreliable
