# Work claim — Semantic Sheet Auto Layout number-prefix length

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-sheet-auto-number-prefix-length`
- Registered: `2026-08-12T10:45:00+07:00`
- Last Updated: `2026-08-12T10:49:00+07:00`
- Baseline main SHA: `fb9e2bf36c84c8ffb340a8f94d7118cea3c42fae`
- Claim commit: `9826397496fa32097f0463f3b26142d2eba01976`
- Implementation PR: `#780`
- Main implementation commit: `0b30447bf2680904f75e5f84bdad17e7493d1c1c`
- Priority: evidence-driven Documentation validation-contract defect found during owner-requested `continue all`
- Task Key: `DOCUMENTATION-SHEET-AUTO-NUMBER-PREFIX-LENGTH`

## Confirmed defect

`SemanticSheetAutoLayoutPlanner.ValidateOptions(...)` previously validated all generated-sheet prefixes through one generic 120-character `Required(...)` limit. The generated sheet number appends at least a two-character ordinal (`01`) and is then validated by `SemanticSheetPlanner`, whose sheet-number contract is 64 characters maximum. A 63-character `SheetNumberPrefix` therefore passed the auto-layout option boundary but every non-empty layout request necessarily generated a first sheet number of at least 65 characters and failed later inside `SemanticSheetPlanner.Build(...)` after packing had already run.

The ID and name prefixes do not have this guaranteed-first-sheet mismatch at their current 120-character bound: generated IDs allow 128 characters and generated names allow 160 characters.

## Implemented

- Added a dedicated 62-character maximum for `SheetNumberPrefix`, reserving the two characters required by the first generated `01` suffix.
- Generalized the local `Required(...)` helper to accept a caller-specific maximum while preserving the existing 120-character default everywhere else.
- Added focused smoke coverage proving a 62-character prefix produces an exact 64-character generated number and a 63-character prefix is rejected at the auto-layout validation boundary.
- Preserved deterministic packing, pagination, per-sheet placement cap, ordering, readonly result and title-block reservation behavior.

## Validation evidence

- Source branch commit: `51eb1da85ec6f7b74a64b9894305d05b18d0baef`.
- Regression branch commit: `2339d332fbe6dcc18c412d88df5d142b82c69f01`.
- Exact PR #780 diff reviewed before merge: 2 files, +23/-3.
- Squash merge to `main`: `0b30447bf2680904f75e5f84bdad17e7493d1c1c`.
- No GitHub Actions/build/release dispatch and no executable smoke or BricsCAD V25/V26 runtime PASS claimed.

## Completion

Current `main` rejects guaranteed-invalid auto-layout sheet-number prefixes at the option boundary while preserving all previously completed auto-layout contracts. This claim is closed `COMPLETED`.
