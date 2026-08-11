# Work claim — Title Block parameter mapping Core P0

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-title-block-parameter-map-core`
- Registered: `2026-08-11T22:20:00+07:00`
- Baseline main SHA: `d2e5c2e4d009193970e1a346da5dfd098e274d4d`
- Issue: `#77`
- Priority: P2

## Reserved scope

Add a pure-Core mapping contract that turns validated `SemanticSheetPlan` fields into deterministic title-block parameter values. Native BricsCAD block/attribute discovery and mutation remain outside this lane.

## Reserved files

- `src/QS3D.Core/Documentation/SemanticTitleBlockParameterMapBuilder.cs` (new)
- `tests/QS3D.Core.SmokeTests/SemanticViewSheetPlannerSmoke.cs`
- `scripts/preflight-semantic-title-block-map.py` (new)
- `docs/DOCUMENTATION-LAYER.md` for a minimal status update
- this claim file for close-out

## Contract

- treat the destination parameter/attribute tag as a bounded opaque key; do not invent BricsCAD tag syntax rules in Core;
- support only explicit semantic Sheet fields in P0: stable SheetId, SheetNumber, SheetName, optional TitleBlockName and PlacedViewCount;
- reject null definitions, blank/overlong tags, duplicate destination tags case-insensitively, over-bounded maps and unknown enum values;
- render numeric values invariant-culture and optional title-block name as empty when absent;
- sort output deterministically by destination tag and return a defensive read-only snapshot;
- remain handle-free and independent from BricsCAD/Teigha APIs;
- do not create BlockReference/AttributeReference entities or assume a customer-private title-block definition exists;
- no Revision, Floor/Level, Quantity, updater, installer, CI/release or other active lane changes;
- no GitHub Actions dispatch/re-run and no licensed V25 runtime claim.

## Completion condition

Core mapping builder + deterministic/fail-closed smoke coverage + focused static gate are merged/read back on `main`, documentation status is accurate, and this claim closes with exact pushed SHA(s).