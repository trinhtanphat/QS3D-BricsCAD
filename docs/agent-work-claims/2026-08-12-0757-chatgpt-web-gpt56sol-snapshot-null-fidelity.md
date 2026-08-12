# Work claim — ProjectStateSnapshot null backing fidelity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-snapshot-null-fidelity`
- Registered: `2026-08-12T07:57:00+07:00`
- Baseline main SHA: `f2531014d3b7a4429e087942ddebb4e209e8c538`
- Priority: P1 — exact rollback state fidelity at a remote-safe Core boundary.

## Confirmed defect

`ProjectStateSnapshot` is the rollback boundary used by semantic mutation flows, and the repository already established that snapshot capture/restore must preserve exact reachable mutable relation state. The current copy path still rewrites reachable runtime `null` backing values to `string.Empty` for project mutable strings, family properties, element relation/source/dependency/property state, audit fields, and project metadata. A failed mutation can therefore return a project that differs from the pre-operation state even when rollback reports success.

This lane does not make null backing valid for persistence or authoring. It only prevents rollback infrastructure from silently canonicalizing pre-existing mutable backing state.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/ProjectStateSnapshotNullFidelitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectStateSnapshotNullFidelityRegistration.cs`
- this claim file

## Intended contract

- Detached snapshot capture and rollback restore preserve nullable runtime backing values exactly where the public mutable containers/properties can already hold them.
- Existing constructor validation, persistence validation, audit authoring normalization, category/quantity semantics, and project identity rules remain unchanged.
- Existing element object identity preservation during rollback remains unchanged.

## Excluded scope

- No changes to `ProjectState`, `ProjectElement`, `ProjectFamily`, `AuditTrail`, QSDB schema/migration, native BricsCAD code, UI, or installer behavior.
- No new acceptance policy for null values at save/load boundaries.
- No GitHub Actions dispatch and no BricsCAD runtime qualification claim.

## Validation plan

- Add isolated Core smoke coverage that seeds pre-existing null backing values, captures a detached copy and a rollback snapshot, mutates the same fields, restores, and verifies the original nulls are retained while element object identity is preserved.
- Re-fetch the reserved source blob immediately before writing and use its exact blob SHA guard.
- Review exact pushed diffs and verify implementation/test/close commits remain reachable from current `main` without force-push.
- No .NET/BricsCAD PASS will be claimed unless actually executed.

## Completion condition

Snapshot capture/restore no longer rewrites pre-existing null backing values to empty strings, focused regression source is on `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs and truthful validation notes.
