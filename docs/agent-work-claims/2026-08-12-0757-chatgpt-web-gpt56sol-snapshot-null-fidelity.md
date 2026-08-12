# Work claim — ProjectStateSnapshot null backing fidelity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-snapshot-null-fidelity`
- Registered: `2026-08-12T07:57:00+07:00`
- Completed: `2026-08-12T08:01:00+07:00`
- Baseline main SHA: `f2531014d3b7a4429e087942ddebb4e209e8c538`
- Claim commit: `70662c5c82b0a119ab0c3f61f4a7439912486ff7`
- Priority: P1 — exact rollback state fidelity at a remote-safe Core boundary.

## Confirmed defect

`ProjectStateSnapshot` is the rollback boundary used by semantic mutation flows, and the repository already established that snapshot capture/restore must preserve exact reachable mutable relation state. The copy path rewrote reachable runtime `null` backing values to `string.Empty` for project mutable strings, family properties, element relation/source/dependency/property state, audit fields, and project metadata. A failed mutation could therefore return a project that differed from the pre-operation state even when rollback reported success.

This lane does not make null backing valid for persistence or authoring. It only prevents rollback infrastructure from silently canonicalizing pre-existing mutable backing state.

## Completed scope

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/ProjectStateSnapshotNullFidelitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectStateSnapshotNullFidelityRegistration.cs`
- this claim file

## Product/test commits

- `f9d4b531b21b3d700ff382a955fa3d908b2cabc9` — `fix(persistence): preserve null snapshot backing state`
- `c8310c98af82a8f5b3655f7697bb4977b4bbb21f` — `test(persistence): cover snapshot null backing fidelity`
- `49ca9fb169d4e48aa0066732b57693fdecfd3719` — `test(persistence): register snapshot null fidelity smoke`

## Resulting contract

- Detached snapshot capture and rollback restore preserve nullable runtime backing values exactly where the public mutable containers/properties can already hold them.
- Existing constructor validation, persistence validation, audit authoring normalization, category/quantity semantics, and project identity rules remain unchanged.
- Existing element object identity preservation during rollback remains unchanged.

## Validation

- Re-fetched the reserved source blob immediately before implementation and wrote against exact blob SHA `678180e0a2920f38c874ce952cf87c645c35f5ca`.
- Reviewed the exact implementation diff; it only removes snapshot-local `null -> string.Empty` coercions on the reserved surface.
- Read back current `main` source after the write and confirmed the null-preserving copy remains present.
- Focused smoke source seeds reachable null backing values across project mutable strings, family properties, element relations/source/dependencies/properties, audit fields and project metadata; it covers both detached copy and rollback restore and asserts captured `ProjectElement` reference identity is preserved.
- Smoke registration uses a dedicated module initializer rather than a shared test registry.
- GitHub Actions were not dispatched.
- No .NET SDK or licensed BricsCAD runtime was available in this session, so no compile/test-runtime/V25/V26 PASS is claimed.

## Excluded scope

- No changes to `ProjectState`, `ProjectElement`, `ProjectFamily`, `AuditTrail`, QSDB schema/migration, native BricsCAD code, UI, or installer behavior.
- No new acceptance policy for null values at save/load boundaries.

## Completion

Snapshot capture/restore no longer rewrites pre-existing null backing values to empty strings on the reserved Core surface, focused regression source is on `main`, and the claim is released as completed.
