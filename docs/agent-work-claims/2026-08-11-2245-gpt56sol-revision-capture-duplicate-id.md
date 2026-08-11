# Work claim — Revision capture duplicate Element ID integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-capture-duplicate-id-20260811-2245`
- Registered: `2026-08-11T22:45:00+07:00`
- Completed: `2026-08-11T22:48:00+07:00`
- Baseline main SHA: `5bccb132a11babd4d5b69ca13ecf6f34d9a374f0`
- Claim commit: `da6985222ffa6cbc3043bd61bf9d4740f9ef8774`
- Source fix commit: `958324f2b4d0ef8276e1afeb5fb0e0d9eb717ee8`
- Regression commit: `f2a3b9e94f02da70add7dae4a94dc01ae62b747a`
- Priority: P2 source-proven regression hardening

## Reserved scope

Fix `RevisionService.Capture` so it cannot return a revision snapshot that violates the duplicate semantic Element ID invariant already enforced by revision compare/report and `RevisionSnapshotStore.Save`. `ProjectState.Elements` is externally mutable, so a project can contain two case-insensitive duplicate IDs; previous capture only checked non-blank IDs and emitted both, deferring failure until a later save/compare boundary.

## Implemented surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file

## Implemented fix

- `RevisionService.Capture` now maintains a case-insensitive semantic Element ID set while capturing.
- A duplicate ID fails closed before a second conflicting snapshot element is emitted.
- Existing capture behavior for finite quantities, canonical source handles and canonicalized dependencies is unchanged.
- Existing `RevisionRegressionSmoke.Run()` now invokes a focused `CaptureRejectsDuplicateElementIds()` regression using `E1`/`e1`.

## Explicit exclusions honored

- No Revision UI/code-behind changes.
- No revision persistence schema/version changes.
- No changes to general ProjectState collection ownership or mutation architecture.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation actually performed

- Verified the claim commit was reachable from current `main` before substantive implementation.
- Re-read exact current `RevisionService.cs` and `RevisionRegressionSmoke.cs` before writes.
- Used current blob SHA checks for source and regression writes; no force push/reset was used.
- Re-fetched current `main` after implementation and verified the capture duplicate guard plus registered regression are present.
- No local checkout/.NET build/Core smoke execution was available in this connector-only lane; executable PASS is not claimed.
- No BricsCAD V25 runtime or GitHub Actions execution is claimed.

## Coordination

The preceding canonical revision Element ID claim is completed. No newer Revision Core claim appeared before implementation, and the completed Revision luxury UI lane explicitly excluded Core revision snapshot logic.

## Completion condition

Completed. Duplicate project Element IDs can no longer escape through `RevisionService.Capture`, regression coverage is committed in the already-registered Revision smoke suite, current source was re-read, and this claim records exact SHAs and the actual validation boundary.
