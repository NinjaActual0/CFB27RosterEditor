# Session State (opencode)

Last updated: 2026-09-13

## Environment
- OS: Windows 11, user: Ninja
- **GITHUB ACCOUNT: `DarthNinja00`** (confirmed correct 2026-09-12; older notes referenced DarthNinja-LOL/DarthNinja0). gh CLI logged in via keyring; git credential helper wired through gh; git.config: name DarthNinja00 (email still DarthNinja-LOL@users.noreply.github.com — verify)
- .NET 10 SDK 10.0.400 survived reinstall
- MSVC: `C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.51.36231\bin\Hostx64\x64\cl.exe`
- Node.js NOT installed anymore (was v24.19.0 for M27 converter — reinstall with `winget install OpenJS.NodeJS.LTS` only if converter is needed again; converter backed up in FMT repo `m27converter\`)
- winget is the package manager (no chocolatey)

## Projects — `C:\Users\Ninja\Source\Repos`

### 1. FMT.Madden26Plugin (PAUSED — waiting on PaulV)
- **What**: FMT plugin for Madden 26/27/CFB27. Anti-cheat bypass, roster editor, face cloning, asset compiler.
- **Stack**: .NET 10, C#, WPF
- **Build**: `dotnet build Madden26Plugin.csproj -c FMT_PRO`
- **Deploy**: `Madden26Plugin.dll` + `ProDotNetZip.dll` → FMT Plugins folder
- **Remotes**: `origin`/`fork` = DarthNinja00/FMT.Madden26Plugin, `upstream` = FMTDev/FMT.Madden26Plugin
- **Branch**: `main`, pushed to new account 2026-08-24
- **Status**: Merged upstream, DotNetZip→ProDotNetZip swapped, pushed to fork. PaulV is reviewing/rewiring before we resume.
- **FMT installs**:
  - NEW (v27): `C:\Users\Ninja\Documents\Mods\FMT_PROFMT_PRO.27.0.9716.24243`
  - OLD (v26): `C:\Users\Ninja\Documents\Mods\FMT_PRO`
- **Game dirs**: `C:\Program Files\EA Games\Madden NFL 26\`, `C:\Program Files\EA Games\EA SPORTS College Football 27\`
- **Plugin deploy**: `C:\Users\Ninja\Documents\Mods\FMT_PRO\Plugins\Madden26Plugin.dll`
- **FMT logs**: `C:\Users\Ninja\Documents\Mods\FMT_PRO\Logging\`
- **Detailed notes**: `FMT.Madden26Plugin/OPCODE_NOTES.md`, `opencode_memory.md`, `opencode_memory_m26_roster_convert.md`
- **Next when resuming**: PaulV review feedback → finish roster display → eye color cloning → anti-cheat PostCompile reliability fix

### 2. CFB27RosterEditor (READY TO WORK)
- **What**: Standalone WPF tool to edit CFB27 roster files (no FMT dependency). Main goal: edit player positions.
- **Stack**: .NET 10, WPF, DotNetZip (swap to ProDotNetZip safe to do — standalone app)
- **Build**: `dotnet build -c Release`
- **Test tool**: `Tools/Tdb2Dump/` — console app to dump all player positions
- **Branch**: `master`, pushed to DarthNinja00 2026-08-24 (was 2 ahead of old origin + notes now committed)
- **Test roster**: `C:\Users\Ninja\Documents\EA SPORTS College Football 27\saves\ROSTER-test`
- **Status**: TDB2 parser complete, all 11,730 PLAY records parse with 0 errors. Position read side DONE. Round-trip (no-edit) is byte-identical.
- **Key blocker**: Position writing — PPOS byte is in TDB2 PLAY subtable (type 4), read side decoded, need to wire into writer + fix FBCHUNKS headers for save-after-edit
- **Detailed notes**: `CFB27RosterEditor/PROGRESS.md`, `CFB27RosterEditor/CFB_Roster_Editor_README.md`

### 3. PinkSlipsTool (COMPLETE)
- **What**: WPF tool for CFB27 dynasty leagues — star calculator, pink slips wheel, perk shop, dynasty save editor.
- **Stack**: .NET 10, WPF
- **Build**: `dotnet build PinkSlipsTool.csproj -c Release` (must target csproj explicitly — multiple project files in root)
- **Publish**: `dotnet publish PinkSlipsTool.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish`
- **Branch**: `master`, pushed to DarthNinja00 2026-08-24 (wheel popup views now committed)
- **Status**: Largely complete. Dev upgrades + player transfers confirmed in-game.

### 4. DuplicationPlugin (TESTING — texture reference issue)
- **What**: Modified MMC Editor DuplicationPlugin.dll — enables face duplication for CFB27 + M27
- **Stack**: .NET Framework 4.8, WPF, decompiled from original DLL via ilspycmd
- **Build**: `dotnet build DuplicationPlugin.csproj -c Release`
- **Deploy (2026-09-13)**: Copy `bin/Release/net48/DuplicationPlugin.dll` to `C:\Users\Ninja\Documents\Mods\M27 mods\MMC_Modding_Tools_v1.1.0.4\MMC_Editor_v1.1.0.4\Plugins\DuplicationPlugin.dll` (stock backed up as `DuplicationPlugin.dll.stock.bak`; `OG_DuplicationPlugin.dll` is the identical stock — MD5 17883BE0...)
- **RETARGETED to MMC 1.1.0.4 SDK (2026-09-13)**: csproj HintPaths now point to `...v1.1.0.4\MMC_Editor_v1.1.0.4\`. Built DLL = 95,744 B, 0 warnings. 1.1.0.4 stock plugin is byte-identical to 1.1.0.3 (gate, filter, AddToBRT, TextureExtension all unchanged) — the BRT fixes are in the SDK, which rebuilding against 1.1.0.4 picks up.
- **Branch**: `master`, 10 commits, **NO REMOTE — local only, NOT published** (user decision 2026-08-24: wait until MMC/Frosty fix the M27 texture duplication issue before sharing). Texture work committed 2026-08-24 (bundle preservation + catch-all shader preset fix).
- **Key file**: `DuplicationPlugin/DuplicationTool.cs` — all modifications
- **Docs**: `README.md` (user-facing), `NOTES.md` (developer details)
- **Status**: Testing in M27 with MMC 1.1.0.4. Mesh duplicates correctly. EBX-side remapping CLOSED (2026-09-13, self-test + import experiment: still old textures; face textures are bundle-bound, zero EBX refs). Next: in-place texture data swap / BRT Res-Chunk remap.
- **CURRENT (resume here, 2026-09-13 ~13:10)**: DECISIVE result in — M27 EBX-side remapping is a DEAD END. Round-2 build fully remaps the chain (MVDB→MeshMaterial/MeshMaterialVariation→Shader→st_preset→inline Textures) + duplicates all preset types, and the user's own in-game test at 13:05 STILL shows OLD TEXTURES. Root cause is confirmed bundle-only binding: `BurrowJoe_20948_color` has ZERO EBX references, bound only by BRT bundles (`win32/.../burrowjoe_20948_color_playerhead_brt`, `burrowjoe_20948_playerhead_brt`). At 13:06 the user imported `barberPeyton_17612_color.png` as a face texture and launched — STILL OLD TEXTURE (import likely went into the duplicated texture asset, which the game never reads). NEXT: implement IN-PLACE texture data swap on original-named assets (SESSION_STATE workaround #4): duplicate heads must stay bound to ORIGINAL texture assets so MMC texture imports land on bundle-loaded data; BRT Res/Chunk remap is the alternative proper fix. Project saved at `D:\Game stuff\mods\M27 mods\Cyber Faces\Boomer test\Boomer test v1.fbproject`.
- **DECISIVE IN-GAME RESULTS (2026-09-13)**: (a) 13:05 user self-test of round-2 build (friend-backed): duplicated head STILL shows old textures — proves EBX chain (MVDB/material/shaders/presets) is NOT how M27 binds face textures. (b) 13:06 experiment: `[Texture Editor] Texture ...barberPeyton_17612_color.png (PNG→TT_2d, BC7_SRGB, 1024x1024, mips=11) successfully imported` → project saved → launched → STILL OLD TEXTURE. Together with the "no references / bundle-only" check this closes the EBX route entirely.
- **M27 material-chain remap fix (2026-09-13, DLL now 126,976 B, 0 warn)**: All 4 ref-fix loops updated — (a) MVDB branch under `num2` re-points `Materials[].Material` → duplicated MeshMaterial and `Materials[].MaterialVariation` → duplicated MeshMaterialVariation when present (replaces bogus mesh-guid pointer + NRE on external variation), null-guards inline shader/texture remap, M26 path unchanged; (b) new `MeshMaterial`/`MeshMaterialVariation` branch remaps `rootObject.Shader.Shader` → duplicated st_preset and inline `TextureParameters` → duplicated textures. Pre-fix deployed DLL backed up as `DuplicationPlugin.dll.pre-*.bak` in Plugins.
- **LOG-DRIVEN FINDING (2026-09-13, Burrow dup + log)**: NO MeshMaterial / MeshMaterialVariation / SurfaceShaderPreset / AppearanceShaderPreset EBX was duplicated — only their per-player BUNDLES (`..._st_preset_playerhead_brt` etc.), meshes, textures, MVDBs. So the material/shader assets are filtered/shared; ref remaps can't fire on them (old textures persist). `AppearanceShaderPreset : SurfaceShaderPreset` carries `BundledTextureParameters[].TextureParam` (WEAK NAME ref `AssetName` CString, not GUID) + `ShaderPreset.TextureParameters[].Value` (GUID refs, catch-all remaps). The M26 `unwantedTypes` filter excluded `AppearanceShaderPreset`/`AppearanceShaderExpressionPreset`/`ShaderExpressionPreset` — likely hiding M27's st_preset EBX.
- **FIX SHIPPED (11:07)**: gated the `RemoveAll` filters so M27 (`num2`) does NOT filter the preset types (strand hair types still filtered M26-only now too). Re-run dup to confirm `Duped type=AppearanceShaderPreset/SurfaceShaderPreset name=..._st_preset` and `Remapped texture ... in ...` lines appear; then in-game check.
- **11:11 RE-RUN (filter gate took effect)**: `AppearanceShaderPreset` (`BurrowJoe_20948_ST_preset`), `AppearanceShaderExpressionPreset` (`..._ST_preset_ES_appearance`), `ShaderExpressionPreset` (`..._ST_preset_ES`), `StrandHairAsset`/`StrandHairBindAsset` ALL now dup correctly. BUT catch-all found ZERO texture refs to remap in the presets.
- **11:55 XML EXPORTS (orig + dup presets) — KEY FINDING**: `st_preset` (`AppearanceShaderPreset`): `ShaderPreset.TextureParameters` holds only SHARED skin textures (arm_skintone3 etc.), `BundledTextureParameters` EMPTY, dup is byte-identical to orig except Name. `..._ST_preset_ES_appearance`/`_ES` hold only shared params (VEC4/Single/PointerRef to common textures/parameters). None reference `BurrowJoe_20948_color`/`_normal`/`_hair_*`.
- **USER CHECK — "no references"**: `BurrowJoe_20948_color` has NO EBX pointer references at all — it's bound purely by **bundle assignment** (`win32/.../burrowjoe_20948_color_playerhead_brt`, `burrowjoe_20948_playerhead_brt`). M27 binds face textures via per-player BRT/bundles, NOT via EBX refs → EBX remapping can never fix M27 textures; the fix must come from the BRT/Res-Chunk bundle side (or in-place data swap on original-named assets, SESSION_STATE #4).
- **NAME-FIX BUILD (2026-09-13 13:51, DLL 128,000 B, 0 warn/0 err)**: All 4 dup loops now (a) preserve source asset case instead of force-`ToLower()`, (b) rewrite embedded old-asset GUID fragments in generated names to the new GUIDs (`RewriteEmbeddedGuids`, pair map from `oldToNewAssetPairs`; parents-before-children assumed), (c) guarantee unique names (`MakeUniqueName`: if name already exists in EBX/RES, append `_1/_2...`). Helpers: `CiReplace`/`RewriteEmbeddedGuids`/`MakeUniqueName`/`AssetNameExists` (DuplicationTool.cs ~503-551). Deployed 13:51, backed up as `DuplicationPlugin.dll.pre-namefix-20260913_135123.bak`.
- **13:55 RESULT — FIX WAS NOT EXERCISED**: `Boomer test v1_13092026_135525.fbproject` autosave is only **3 bytes** different from the 13:05 one → user retested with the OLD duplicate data (name fix never ran; no re-dup). "Still original texture" result is NOT a test of the name fix.
- **CRASH ON OPENING DUP MESH — ROOT CAUSE FOUND (OUR ROUND-2 MVDB REMAP)**: `crashlog.txt` stack = `MeshSetPlugin.FrostyMeshSetEditor.LoadVariations()` NRE. Decompiled: `MeshVariationMaterial.cs:20` sets `MaterialGuid = Material.External.ClassGuid` (viewer matches MVDB material by the mesh's inline Material ClassGuid); `LoadVariations` (~1103-1105) does `item2.GetMaterial(instanceGuid)` then derefs `material.MaterialVariationAssetGuid` → NRE. Our round-2 num2-branch re-pointed MVDB `Materials[].Material` to the DUPLICATED MeshMaterial asset (new FileGuid + new RootInstanceGuid ≠ dup mesh inline material guid) → `GetMaterial()` null → NRE. FIXED 14:13 (DLL 124,416 B, 0 warn/0 err, backup `DuplicationPlugin.dll.pre-mvdb-remap-remove-*.bak`): both MVDB branches now re-point `Materials[].Material` to the DUP MESH partition guid (`guid2`) keeping the ORIGINAL `ClassGuid` (= mesh inline material instance guid, matches viewer + game) and the M27 MaterialVariation remap was REMOVED (nothing to remap, variation is an inline MeshMaterialVariation in the dup mesh partition anyway).
- **MESH XML PROOF (from `burrowjoe_20948_mesh.xml`)**: SkinnedMeshAsset has NO texture refs — `Materials[]` are 3 `MeshMaterial`s (blue_light_eye / player_head2020_ST2_preset / eyelash_long_black), all `TextureParameters` EMPTY, `Shader` refs point to shared presets. `ComputeDeformer` ref embeds the mesh's own guid. So per-player `_color` texture binds at RUNTIME only (name/identity via bundles), never through mesh/MVDB EBX.
- **Current working changes (uncommitted)**:
  1. BRT blueprint reference fix (commit 19bdc4f) — `BundleRefTableBlueprintBundle` handler remaps `rootObject.Blueprint.External.FileGuid` via `oldToNewAssetPairs`
  2. Strand/shader filter restored (commit 70c8eee) — `unwantedTypes` list filters out 6 types from duplication
  3. **Bundle fix**: After `DuplicateAsset`, force `AddedBundles` back to original via `assetEntry.EnumerateBundles()`. This prevents extensions (like `BlueprintBundleExtension`) from creating new bundles that the game can't find. Mesh now works in-game.
  4. **Catch-all `else` handler**: Changed `else if (item.Type == "ShaderTexturePreset")` to `else` in all 4 reference-fixing loops. Tries `rootObject.ShaderPreset.TextureParameters` on any unhandled type. Includes diagnostic logging.
   5. **MVDB material pointer fix (14:13)**: in BOTH MVDB branches, `Materials[].Material` re-points to dup mesh partition (`guid2`) + ORIGINAL ClassGuid; removed the M27 `num2` remap to duplicated MeshMaterial asset/±MaterialVariation (this was crashing the MMC mesh viewer). Name-fix (13:51) unchanged.
- **CONFIRMED WORKING**: Version gate removed, BRT fix (fallback characters resolved), bundle fix (mesh shows correctly), recipe duplication, tools dropdown menus
- **ACTIVE BUG — textures show old player**: 
  - Duplicated head shows correct mesh/shape but old player's face textures
  - **CRITICAL FINDING (Aug 19)**: Even manual standalone texture duplication (right-click → Duplicate on a single color file) doesn't show in game. The issue is NOT in our face duplication code — it's a fundamental Frosty/MMC texture duplication problem in M27. Textures are loaded through a mechanism that breaks when new assets are created, regardless of how.
  - Mesh works because it's loaded through explicit EBX references (ObjectBlueprint → Mesh). Textures must use a different loading path (Res chunk binding, path convention, or MVDB-driven) that doesn't survive duplication.
  - TextureAsset entries like `burrowjoe_20948b_color_playerhead_brt` are duplicated (Res + Chunk created) but the game ignores them.
  - M27 MVDB `TextureParameters` are empty (EA changed storage between M26→M27), so the existing MVDB texture remapping code does nothing in M27.
  - **Possible workaround**: Instead of duplicating textures as new assets, modify the existing duplicated TextureAsset entries to point to the new Res/Chunk data. Or swap texture data in-place on the original entries.
- **M26 vs M27 texture difference**: M26 duplication works perfectly with identical code — MVDB TextureParameters are populated and the existing remapping code works. In M27, textures are stored/referenced via `st_preset` instead.
- **FMT beta exports** (reference data): `E:\Game stuff\mods\M27 mods\beta mods\Cyber Faces from M27\Players\` — per-player `.bin`, `.json`, `.png` showing full EBX structure
- **St_preset structure** (from FMT JSON): `ShaderPreset.TextureParameters[]` entries like `colorTexture`, `normalClampTexture`, `R_S_SSR_T_Texture`, etc. with `Value.External.FileGuid` pointers to TextureAsset EBX entries
- **Frosty texture duplication summary**: Shared with user to pass to Frosty/MMC devs. Summary covers M27 TextureBaseAsset duplication failure, M26 vs M27 MVDB TextureParameters difference, manual reproduction steps, and suspected root cause (texture Res/Chunk binding mechanism changed in M27).
- **Next session priorities**:
  1. **Confirm 13:06 import target** (original `BurrowJoe_20948_color` vs duplicated `boomerburrowjoe_20948_color`) — determines if in-place swap on original-named assets works (dup head reads shared bundles)
  2. **Implement in-place texture workaround (2026-09-13 decision)**: in M27, keep duplicated heads bound to ORIGINAL texture assets instead of new ones — modded texture imports then land on bundle-loaded data and show in-game. Alternative: BRT Res/Chunk remap so new assets can load.
  3. **Follow up with Frosty devs** on the M27 texture duplication issue — if they identify the new texture binding mechanism, we can adapt our duplication code
  4. Once textures work: commit all changes, create GitHub repo, share with MMC community

## Paused Work (from earlier sessions)

### MaddenSaveConverter — "Legion of Loom Madden Save Converter (MSC)" (ACTIVE — core engine DONE)
- **What**: Standalone portable WPF tool. Pure C# port of the JS TDB2/FBCHUNKS pipeline (no Node dependency). Roadmap: 1) roster converter ✅ 2) draft class transfer 3) retro schedule tool 4) franchise transfer.
- **Repo**: `C:\Users\Ninja\Source\Repos\MaddenSaveConverter` — **local only, NOT published** (user decision). Commit d7cc923 (2026-08-25).
- **Stack**: .NET 10, WPF, net10.0-windows; LOL branding (black #000000 / green #00A040/#00C048), logo in Assets/
- **Build**: `dotnet build -c Release`; headless validation: `MaddenSaveConverter.exe --selftest` → **EXIT=0**
- **Self-test**: `--selftest` (forward M26→M27) and `--selftest --reverse` (M27→M26), both **EXIT=0**. Forward: converts ROSTER-2019, verifies size 6291530 + len + CRC-32B, reloads cleanly, asserts SEMANTIC equality vs JS reference. Reverse: converts the verified output back to M26 format and spot-checks PGID 112 contract (PCON=4 PSA0=615 PSB0=75). Byte-identity NOT the bar (different deflate impls).
- **Status**: BOTH directions pass. Portable single-file publish done: `publish\MaddenSaveConverter.exe` (133 MB self-contained) + `Templates\ROSTER-Official` + `Templates\ROSTER-M26-sample`. Published exe passes both self-tests.
- **Caveat**: reverse test's source is the forward output which still carries salary keys in PLAY (game tolerated this in the verified run), so PLAY salary *injection* path ran 0 times there — worth one manual GUI test converting an official M27 roster (106-field PLAY) to M26.
- **Format gotchas learned while porting** (all fixed & encoded in Tdb2Reader.cs):
  - Top-level type-3 table: entries LEB starts at FRESH byte after 9-byte key; type-4: LEB starts FROM b0 (byte 5); type-5: [unk1][unk2][fresh LEB]
  - Type-4 FIELD subtable inside uncompressed records: LEB starts FROM chunk[4]; inside decompressed records: unk1 byte consumed THEN fresh LEB
  - Record-end check: non-zero byte = first byte of next field chunk (forward it!)
  - Compressed records only when unknown2==2; otherwise plain fields
  - BLBM/BLOB gz records: CHAN(4) → subheader(4) → if next byte not 00/8E parse subrecord fields, then terminator implies parent CHVI(4) must be skipped before parent fields
  - Subtables INSIDE decompressed records use different record parsing (_readCompressedRecordSubTable): 4-byte keys, plain UNK, no UNWI trick
  - Modified LEB = six-bit groups w/ XOR 0x80 + sign bit — exact port in SixBit.cs (Leb.Read/Write)
  - Reader MUST add each parsed record to table.Records (writer serializes from there)
- **Save naming rules (user-confirmed, in-game enforced)**: rosters `ROSTER-(NAME)`, franchise `CAREER-(NAME)`, draft classes `CAREERDRAFT-(NAME)` — exactly ONE dash, no other punctuation allowed in-game. Bundled templates follow this: `Templates\ROSTER-TemplateM26` (= official M26 RL11 roster) and `Templates\ROSTER-TemplateM27` (= official M27 RL1 roster), so they can be copied into a saves folder if ever needed.
- **Persona randomization (user request, 2026-08-25)**: forward conversion by default samples WHOLE persona rows from the template's real PRSN records (shuffled, near-uniform coverage — 2854 distinct variants across 3133 players) so converted players don't all share zeroed DNA data. `randomizePersona:false` reproduces the zeroed JS-parity output (used by selftest comparison). GUI uses randomized mode.
- **Next**: manual GUI smoke-test (load/convert/save via window), one reverse run with official M27 roster to exercise salary injection, then roadmap item 2: draft class transfer.
- **Reference material**: JS lib clone @ bep713/madden-file-tools 6c7eb93 + trace scripts in `C:\Users\Ninja\AppData\Local\Temp\opencode\m26to27\` (`trace_m26.js` prints full parser decision trace — invaluable for debugging port drift)

### M20 Player Head Port (PAUSED — user needs to prepare exports)
- Goal: Port M27 player heads/bodies into M20 via FrostyEditor
- Blocker: Head bone mapping differs M20↔M27, need exported head sets to diff
- M20 FrostyEditor: `C:\Users\Ninja\Documents\Mods\M20 mods\FrostyEditor\`
- Needs: M20 `.ast` portrait file + M27 portrait exports to build ID allocator

### CFB27 Roster Hash Reversal (PAUSED)
- Checksums at header offsets 26-29 are validated, not standard CRC32/CRC16/Adler
- Different from M26/M27 roster format (which uses standard CRC-32)
- Next: try MurmurHash3, Jenkins lookup3, xxHash on inner payload
