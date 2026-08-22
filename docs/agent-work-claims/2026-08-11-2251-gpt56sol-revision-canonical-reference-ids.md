# Work claim — Revision canonical semantic reference IDs

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-reference-id-20260811-2251`
- Registered: `2026-08-11T22:51:00+07:00`
- Completed: `2026-08-11T22:54:00+07:00`
- Baseline main SHA: `7ad2cc751da584ed74122180bae62d2f421db68c`
- Claim commit: `480553e3f2bd5bed006d40193a4077ba07651cec`
- Source fix commit: `5ae0fa7eac6bbf7018a6eb31eba5f9d17a7699c8`
- Regression commit: `37ae2736c56352c7cf8d666fe372660764121f56`
- Priority: P2 source-proven regression hardening

## Reserved scope

Align the low-level Revision capture/compare boundary with the canonical optional identity contract already enforced by `RevisionSnapshotStore.Save`. `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` are publicly settable after constructor normalization, so a padded value could escape through `RevisionService.Capture`; manually supplied public `RevisionSnapshot` instances with padded semantic reference IDs could also be compared as ordinary identity changes even though the persistence boundary rejects them.

## Implemented surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file

## Implemented fix

- `RevisionService.Capture` now validates non-empty Family/Floor/Zone references as canonical, non-padded identity values before copying them into a snapshot.
- `RevisionService.Compare` now applies the same reference validation while indexing manually supplied before/after snapshots.
- Empty optional references remain accepted and existing case-insensitive identity comparison for canonical values remains unchanged.
- Regression coverage mutates each public `ProjectElement` reference setter to a padded value and proves capture fails closed, and separately proves padded Family/Floor/Zone references in public `RevisionSnapshot` instances are rejected by compare.

## Explicit exclusions honored

- No changes to `ProjectElement` setters or general project mutation architecture.
- No revision XML schema/version or `RevisionSnapshotStore` changes.
- No Revision UI/code-behind changes.
- No Family/Floor/Zone manager behavior changes.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation actually performed

- Verified the claim commit was reachable from current `main` before substantive writes.
- Re-fetched exact current source/test blobs immediately before implementation and used their blob SHAs for conflict-safe writes.
- Re-read current `main` after implementation and verified the capture guards, compare index guards, and both focused regression methods are present in the already-registered `RevisionRegressionSmoke.Run()` suite.
- No force push/reset was used.
- No local checkout/.NET build/Core smoke execution was available in this connector-only lane; executable PASS is not claimed.
- No BricsCAD V25 runtime or GitHub Actions execution is claimed.

## Coordination

The two preceding Revision Core claims were completed before this batch. Recent Revision UI/read-only claims were also completed and explicitly excluded Core snapshot schema/comparison semantics, so this implementation remained disjoint.

## Completion condition

Completed. Revision capture/compare now rejects semantic Family/Floor/Zone reference IDs that revision persistence rejects, focused regression coverage is committed on `main`, current source was re-read, and this claim records exact SHAs and the actual validation boundary.
