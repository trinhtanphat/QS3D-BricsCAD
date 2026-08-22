# Work claim — ProjectStateSnapshot relation fidelity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-snapshot-relation-fidelity`
- Registered: `2026-08-11T23:19:00+07:00`
- Completed: `2026-08-11T23:27:00+07:00`
- Reservation commit: `f9e24367c48ea18179b4b988c128418c434d9f68`
- Priority: P1 — preserve exact reachable semantic state across rollback snapshots.

## Defect fixed

`ProjectStateSnapshot.CopyInto(...)` reconstructed each `ProjectElement` by passing `FamilyId`, `FloorId`, and `ZoneId` through the `ProjectElement` constructor. Those three relation properties are public mutable strings, while the constructor trims them. A reachable pre-operation relation such as `"  FAM  "` was therefore silently canonicalized while the snapshot was captured and could not be restored byte-for-byte after a failed mutation.

Snapshot copying now constructs the element with only immutable identity/category and then assigns the three mutable relation fields directly from the captured source. New-element constructor policy is unchanged, while rollback fidelity now matches the reachable in-memory state.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/ProjectSemanticMutationExecutorSmoke.cs`
- this claim file

## Published commits

- `f3ab6727e8f5fbefc9596d312ae9193f31346875` — preserve exact mutable Family/Floor/Zone relation strings in detached snapshot copies.
- `53f9e9c42aad75a59a7dc3c713e3928f989d5e15` — add fault-injection regression proving padded relation strings return exactly after semantic rollback.

## Validation notes

- Exact source/test diffs were fetched after publication and are limited to the reserved surfaces.
- The test is in the already auto-registered `ProjectSemanticMutationExecutorSmoke` suite and fails the prior snapshot implementation because constructor trimming changes the captured relation values.
- A stale test write was rejected with HTTP 409 during concurrent main movement; the current blob was re-read and no force-push/overwrite was used.
- This remote execution environment does not provide a usable .NET/BricsCAD V25 toolchain, so executable smoke/native runtime PASS is not claimed.
- GitHub Actions were not dispatched.

## Excluded scope

- No changes to `ProjectElement` setters or constructor policy.
- No changes to `ProjectFloorService` / `ProjectZoneService`; those remain under their separate canonical-reference lane.
- No persistence migration/schema behavior changes.
- No native DWG/UI/runtime changes.

## Completion condition

Satisfied for the source/static rollback contract. Exact executable/native qualification remains separate from this remote-safe fix.
