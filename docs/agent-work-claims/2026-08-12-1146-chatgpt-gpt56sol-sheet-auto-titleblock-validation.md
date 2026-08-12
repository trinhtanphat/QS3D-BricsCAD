# Work claim — Semantic Sheet Auto Layout title-block validation

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-sheet-auto-titleblock-validation`
- Registered: `2026-08-12T11:46:00+07:00`
- Last Updated: `2026-08-12T11:53:00+07:00`
- Baseline main SHA: `acd030b9aebf75eb1fddaef9b3a48e954897b09f`
- Claim commit: `7dc08688c20c7ad1dc28d35a74c650d543270104`
- Implementation PR: `#838`
- Main source commit during moving-base race: `783bbfe37a696084ed15c65f19fc44b768857fe5`
- PR merge commit: `2b04b601d6a4e656add8a46feb53a7ee7e0343ca`
- Priority: evidence-driven Documentation validation-order defect found during owner-requested `continue all`
- Task Key: `DOCUMENTATION-SHEET-AUTO-TITLEBLOCK-VALIDATION`

## Confirmed defect

`SemanticSheetAutoLayoutPlanner.ValidateOptions(...)` validated required prefixes, paper dimensions, margins, gaps and reserved area, but did not validate optional `TitleBlockName`. The downstream `SemanticSheetPlanner` accepts an optional title-block name only up to 160 characters. For non-empty auto-layout requests, an overlong title-block name therefore failed only after view indexing/materialization/packing. For an empty request, `Build()` returned `Array.Empty<SemanticSheetPlan>()` before constructing any `SemanticSheetDefinition`, so the same invalid overlong option was silently accepted.

## Implemented

- Added the existing 160-character title-block limit to auto-layout option validation.
- Optional values are measured after trimming only for length validation; null/empty/whitespace remains optional and downstream normalization is unchanged.
- Overlong values now fail before available views/items are enumerated or layout work begins.
- Added focused registered Core smoke coverage for the 160-character boundary, 161-character early failure, and whitespace-only optional/null behavior.
- Preserved generated identity bounds, packing, pagination, ordering and title-block output semantics.

## Validation evidence

- Source branch commit: `7ddeacd521afcbd5c367904f59d8a20e35e5f46e`.
- Smoke branch commit: `efd6c340417b1a895d04f4d3981f55407c07bd1f`.
- Registration branch commit/head: `4b6a5b834c1554f119710bd34ed79ba409f857e8`.
- Exact PR #838 diff reviewed before integration: 3 files, +107/-0.
- `main` moved repeatedly during merge attempts while the exact target source blob stayed unchanged. The reviewed source content was applied safely with its exact blob precondition at `783bbfe37a696084ed15c65f19fc44b768857fe5`; PR #838 then merged the same reviewed branch content at `2b04b601d6a4e656add8a46feb53a7ee7e0343ca`.
- Final main source/smoke/registration blobs match the reviewed branch blobs.
- No force-push, no GitHub Actions/build/release dispatch, and no executable smoke or BricsCAD V25/V26 runtime PASS claimed.

## Completion

Current `main` validates `TitleBlockName` consistently before auto-layout work while preserving optional whitespace semantics. This claim is closed `COMPLETED`.
