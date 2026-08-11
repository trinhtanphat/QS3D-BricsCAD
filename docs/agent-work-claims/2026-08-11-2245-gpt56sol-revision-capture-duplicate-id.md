# Work claim — Revision capture duplicate Element ID integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-capture-duplicate-id-20260811-2245`
- Registered: `2026-08-11T22:45:00+07:00`
- Baseline main SHA: `5bccb132a11babd4d5b69ca13ecf6f34d9a374f0`
- Priority: P2 source-proven regression hardening

## Reserved scope

Fix `RevisionService.Capture` so it cannot return a revision snapshot that violates the duplicate semantic Element ID invariant already enforced by revision compare/report and `RevisionSnapshotStore.Save`. `ProjectState.Elements` is externally mutable, so a project can contain two case-insensitive duplicate IDs; current capture only checks non-blank IDs and emits both, deferring failure until a later save/compare boundary.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No Revision UI/code-behind changes.
- No revision persistence schema/version changes.
- No changes to general ProjectState collection ownership or mutation architecture.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation plan

- Re-read exact current source and test blob before implementation.
- Add case-insensitive duplicate Element ID validation inside `RevisionService.Capture` before emitting duplicate snapshot entries.
- Add deterministic smoke coverage constructing a ProjectState with `E1`/`e1` and proving Capture fails closed.
- Preserve current non-finite quantity, source-handle canonicalization and dependency behavior.
- Source/static readback plus committed smoke coverage only; no executable/local/V25 PASS claim in this connector lane.

## Coordination

The preceding canonical revision Element ID claim is completed. No newer Revision Core claim is present in recent claim history; the completed Revision luxury UI lane explicitly excluded Core revision snapshot logic.

## Completion condition

Duplicate project Element IDs cannot escape through `RevisionService.Capture`, regression coverage is committed on current `main`, current source is re-read, and this claim is closed with exact SHAs and actual validation scope.
