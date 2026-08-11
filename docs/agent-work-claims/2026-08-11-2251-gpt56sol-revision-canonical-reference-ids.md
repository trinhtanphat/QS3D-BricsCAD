# Work claim — Revision canonical semantic reference IDs

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-reference-id-20260811-2251`
- Registered: `2026-08-11T22:51:00+07:00`
- Baseline main SHA: `7ad2cc751da584ed74122180bae62d2f421db68c`
- Priority: P2 source-proven regression hardening

## Reserved scope

Align the low-level Revision capture/compare boundary with the canonical optional identity contract already enforced by `RevisionSnapshotStore.Save`. `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` are publicly settable after constructor normalization, so a padded value can currently escape through `RevisionService.Capture`; manually supplied public `RevisionSnapshot` instances with padded semantic reference IDs can also be compared as ordinary identity changes even though the persistence boundary rejects them.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No changes to `ProjectElement` setters or general project mutation architecture.
- No revision XML schema/version or `RevisionSnapshotStore` changes.
- No Revision UI/code-behind changes.
- No Family/Floor/Zone manager behavior changes.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation plan

- Verify this claim is reachable from current `main` before substantive writes and re-fetch current source/test blobs.
- Reject non-empty `FamilyId`, `FloorId`, or `ZoneId` values containing leading/trailing whitespace during `RevisionService.Capture` and during low-level snapshot indexing for `Compare`.
- Preserve empty optional references and existing case-insensitive identity comparison semantics for canonical values.
- Add deterministic regression coverage for a padded project reference during capture and a padded manually supplied snapshot reference during compare.
- Validation is source/static readback plus committed smoke coverage only; no local .NET/BricsCAD/Actions PASS claim.

## Coordination

The two preceding Revision Core claims are completed. Recent Revision UI/read-only claims are also completed and explicitly excluded Core snapshot schema/comparison semantics. If a newer active claim reserves these exact Core Revision surfaces before implementation, stop and re-scope.

## Completion condition

Revision capture/compare cannot accept semantic reference IDs that its own snapshot persistence rejects, focused regression coverage is committed on `main`, current source is re-read, and this claim is marked `COMPLETED` with exact SHAs and actual validation scope.
