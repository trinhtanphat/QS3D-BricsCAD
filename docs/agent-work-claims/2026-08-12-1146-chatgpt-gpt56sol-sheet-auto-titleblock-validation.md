# Work claim — Semantic Sheet Auto Layout title-block validation

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-sheet-auto-titleblock-validation`
- Registered: `2026-08-12T11:46:00+07:00`
- Last Updated: `2026-08-12T11:46:00+07:00`
- Baseline main SHA: `acd030b9aebf75eb1fddaef9b3a48e954897b09f`
- Priority: evidence-driven Documentation validation-order defect found during owner-requested `continue all`
- Task Key: `DOCUMENTATION-SHEET-AUTO-TITLEBLOCK-VALIDATION`

## Confirmed defect

`SemanticSheetAutoLayoutPlanner.ValidateOptions(...)` validates required prefixes, paper dimensions, margins, gaps and reserved area, but does not validate optional `TitleBlockName`. The downstream `SemanticSheetPlanner` accepts an optional title-block name only up to 160 characters. For non-empty auto-layout requests, an overlong title-block name therefore fails only after view indexing/materialization/packing. For an empty request, `Build()` returns `Array.Empty<SemanticSheetPlan>()` before constructing any `SemanticSheetDefinition`, so the same invalid overlong option is silently accepted.

## Reserved scope

Validate the optional title-block name against the existing 160-character downstream contract at the auto-layout option boundary. Preserve current null/empty/whitespace normalization semantics by validating trimmed length only; do not require canonical whitespace or change the output value path.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- focused registered Core smoke for early title-block validation
- this claim file

## Explicit exclusions / coordination

- Do not change sheet/title-block downstream limits, auto-layout packing/pagination, generated identities or existing prefix bounds.
- Do not change Semantic View, Schedule, Tag, Catalog Store/Editor, UI/native or release surfaces.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- A 160-character non-whitespace title-block name remains valid.
- A 161-character title-block name fails with `ArgumentException` before items/views are enumerated.
- Empty/whitespace title-block names preserve downstream optional/null behavior.
- Exact PR diff and moving-main source blob are reviewed before integration.

## Completion condition

Current `main` validates `TitleBlockName` consistently before auto-layout work, focused regression coverage is merged, and this claim is closed `COMPLETED` with exact evidence.
