# Work claim — Revision canonical element ID fail-closed integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-canonical-id-20260811-2239`
- Registered: `2026-08-11T22:39:45+07:00`
- Completed: `2026-08-11T22:43:00+07:00`
- Baseline main SHA: `fbb7fae24e2fb2086715aba071aa8946e88e47ad`
- Claim commit: `2d0febbd42b9589e1f6103b57aa505c93fca4a89`
- Quantity diff fix commit: `c6a522afdf004877fc3f154d19596cf730625d57`
- Revision compare fix commit: `60eeb617312553710bf5755b3422b42a7e017145`
- Regression commit: `b3a7bf0dd54d477116d31fb1ddee65f707e8faca`
- Priority: P2 source-proven regression hardening

## Reserved scope

Fix the Core Revision identity-validation mismatch where persisted revision snapshots and `SemanticChangeReviewBuilder` reject leading/trailing whitespace in semantic Element IDs, while `RevisionService.Compare` and `QuantityRevisionReport.Build` previously indexed raw non-blank IDs. A malformed in-memory/public `RevisionSnapshot` could therefore be interpreted as different Added/Removed identities instead of failing closed at the comparison boundary.

## Implemented surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file

## Implemented fix

- `RevisionService.Compare` now rejects leading/trailing whitespace on every snapshot Element ID before indexing.
- `QuantityRevisionReport.Build` now enforces the same canonical Element ID rule.
- Existing case-insensitive duplicate detection remains unchanged for canonical IDs.
- Regression coverage proves both APIs reject a padded single identity (`" E1 "`) and reject a snapshot containing canonical `"E1"` plus padded `" E1 "` rather than reporting misleading Added/Removed quantity rows.

## Explicit exclusions honored

- No Revision WPF/UI/code-behind changes.
- No revision persistence schema/version or XML store changes.
- No quantity calculation/rule semantics changes.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation actually performed

- Re-read current `main` before implementation and confirmed both low-level revision indexes still accepted padded non-blank IDs while `SemanticChangeReviewBuilder` and `RevisionSnapshotStore` already rejected them.
- Verified the separate claim commit was reachable from current `main` before substantive writes.
- Used current blob SHA checks for every source/test write; no force push/reset was used.
- Re-fetched current `main` after implementation and verified both low-level indexes contain the canonical whitespace guard and `RevisionRegressionSmoke` contains and registers `PaddedElementIdsAreRejected()` through its existing `Run()` method.
- No local checkout/.NET build/Core smoke execution was available in this connector-only lane; executable PASS is not claimed.
- No BricsCAD V25 runtime or GitHub Actions execution is claimed.

## Coordination

The recently completed Revision luxury UI claim explicitly excluded Core revision arithmetic/snapshots. This batch remained entirely inside the disjoint Core Revision identity-validation lane.

## Completion condition

Completed. The canonical-ID comparison gap is fixed on `main`, focused regression coverage is committed in the already-registered Revision smoke suite, current source was re-read after the writes, and this claim records the exact implementation commits and actual validation boundary.
