# Work claim — Semantic Sheet Auto Layout number-prefix length

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-sheet-auto-number-prefix-length`
- Registered: `2026-08-12T10:45:00+07:00`
- Last Updated: `2026-08-12T10:45:00+07:00`
- Baseline main SHA: `fb9e2bf36c84c8ffb340a8f94d7118cea3c42fae`
- Priority: evidence-driven Documentation validation-contract defect found during owner-requested `continue all`
- Task Key: `DOCUMENTATION-SHEET-AUTO-NUMBER-PREFIX-LENGTH`

## Confirmed defect

`SemanticSheetAutoLayoutPlanner.ValidateOptions(...)` validates all generated-sheet prefixes through one generic 120-character `Required(...)` limit. The generated sheet number appends at least a two-character ordinal (`01`) and is then validated by `SemanticSheetPlanner`, whose sheet-number contract is 64 characters maximum. A 63-character `SheetNumberPrefix` therefore passes the auto-layout option boundary but every non-empty layout request necessarily generates a first sheet number of at least 65 characters and fails later inside `SemanticSheetPlanner.Build(...)` after packing has already run.

The ID and name prefixes do not have this guaranteed-first-sheet mismatch at their current 120-character bound: generated IDs allow 128 characters and generated names allow 160 characters.

## Reserved scope

Reject sheet-number prefixes that cannot represent even generated sheet `01` within the downstream 64-character sheet-number contract. Preserve current trimming, packing, per-sheet placement cap, ordering, read-only output, title-block reservation and all view identity behavior.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutSmoke.cs`
- this claim file

## Explicit exclusions / coordination

- Do not change `SemanticSheetPlanner` output limits.
- Do not change auto-layout packing/pagination or the completed per-sheet cap/readonly-result lanes.
- Do not alter Semantic Tag, Documentation Catalog, Schedule Placement, UI/native or release surfaces.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- A 62-character `SheetNumberPrefix` remains accepted for a one-sheet layout and produces a 64-character number ending in `01`.
- A 63-character prefix is rejected at the auto-layout option validation boundary rather than passing through packing and failing later in the generated sheet planner.
- Existing deterministic packing and pagination smoke remains unchanged.
- Re-fetch moving `main` source/test blobs and inspect exact PR diff before integration.

## Completion condition

Current `main` rejects guaranteed-invalid auto-layout sheet-number prefixes at the option boundary, focused smoke coverage is merged, and this claim is closed `COMPLETED` with exact evidence.
