# Work claim — Semantic Sheet Index Core P0

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-sheet-index-core`
- Registered: `2026-08-11T22:14:00+07:00`
- Baseline main SHA: `841b462765c6fa4621f08d8cf587309e0a9ebf3b`
- Issue: `#77`
- Priority: P2

## Reserved scope

Add a pure-Core, handle-free and deterministic Sheet Index model/builder from already validated `SemanticSheetPlan` data. This is a source-safe documentation parity slice only; it does not create or mutate BricsCAD Layout/Table/Viewport objects.

## Reserved files

- `src/QS3D.Core/Documentation/SemanticSheetIndexBuilder.cs` (new)
- `tests/QS3D.Core.SmokeTests/SemanticViewSheetPlannerSmoke.cs`
- `scripts/preflight-semantic-sheet-index.py` (new)
- `docs/DOCUMENTATION-LAYER.md` only if a minimal status note is needed after source lands
- this claim file for close-out

## Contract

- consume validated `SemanticSheetPlan` objects and preserve stable semantic `SheetId` separately from display number/name;
- produce deterministic rows ordered case-insensitively by sheet number then stable sheet ID;
- reject null rows, case-insensitive duplicate sheet IDs and duplicate sheet numbers, and over-bounded catalogs;
- defensively copy the returned rows so callers cannot mutate a previously built index;
- keep the P0 row handle-free and derived only from semantic sheet data: sheet ID, number, name, optional title-block name, and placed-view count;
- do not invent Layout/PaperSpace/Viewport/Table APIs or native IDs;
- do not touch Revision, Floor/Level, Quantity, updater, installer, CI/release or any other active product lane;
- no GitHub Actions dispatch/re-run and no licensed V25 runtime claim.

## Completion condition

Core builder + deterministic/fail-closed smoke coverage + focused static gate are merged/read back on `main`, documentation truth remains accurate, and this claim closes with exact pushed SHA(s).