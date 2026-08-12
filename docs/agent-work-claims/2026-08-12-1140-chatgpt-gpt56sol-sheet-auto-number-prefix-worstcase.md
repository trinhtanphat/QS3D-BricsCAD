# Work claim — Semantic Sheet Auto Layout worst-case number prefix

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-sheet-auto-number-prefix-worstcase`
- Registered: `2026-08-12T11:40:00+07:00`
- Last Updated: `2026-08-12T11:40:00+07:00`
- Baseline main SHA: `28fb849be9248c8532e908d6baebabd0b069f83d`
- Priority: follow-up correctness defect found while auditing the completed auto-layout number-prefix fix
- Task Key: `DOCUMENTATION-SHEET-AUTO-NUMBER-PREFIX-WORSTCASE`

## Confirmed defect

`SemanticSheetAutoLayoutPlanner` currently limits `SheetNumberPrefix` to 62 characters so generated sheet `01` fits the downstream 64-character `SemanticSheetPlanner` number contract. That bound only accounts for the first two-digit ordinal. The same planner supports up to 10,000 requested views, and in the worst case each view can require its own sheet. `ordinal.ToString("D2")` therefore reaches `10000` (five characters). A 60..62-character prefix can pass option validation yet later fail inside `SemanticSheetPlanner.Build(...)` when a sufficiently high sheet ordinal is generated.

## Reserved scope

Make the auto-layout prefix bound reserve enough characters for the maximum sheet ordinal permitted by the existing `MaxItems = 10000` contract. Reuse the downstream sheet-number length contract instead of weakening it. Preserve all packing, pagination, ordering, title-block reservation, immutable result and view identity behavior.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- `src/QS3D.Core/Documentation/SemanticSheetPlanner.cs` only if needed to share the existing number-length constant without duplicating it
- `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutSmoke.cs`
- this claim file

## Explicit exclusions / coordination

- Do not change `MaxItems`, `MaxPlacements`, sheet-number max length, packing or pagination semantics.
- Do not change Semantic View, Schedule, Tag, Catalog Store/Editor, UI/native or release surfaces.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- A 59-character prefix remains accepted; combining it with maximum ordinal `10000` is exactly 64 characters.
- A 60-character prefix is rejected at option validation before item/view enumeration or layout work.
- Existing two-digit numbering and deterministic packing tests remain unchanged except for the corrected prefix boundary.
- Re-fetch moving `main` blobs and review the exact PR diff before integration.

## Completion condition

Current `main` reserves the full worst-case generated ordinal inside the 64-character sheet-number contract, focused regression coverage is merged, and this claim is closed `COMPLETED` with exact evidence.
