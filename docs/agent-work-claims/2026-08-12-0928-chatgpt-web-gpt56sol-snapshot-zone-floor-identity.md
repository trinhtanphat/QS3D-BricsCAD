# Work claim — Snapshot Zone/Floor rollback identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-snapshot-zone-floor-identity`
- Registered: `2026-08-12T09:28:00+07:00`
- Baseline main SHA: `640b41b4178a290b06f45aae1b166a007cd8243b`
- Priority: P1 transaction rollback identity integrity found during owner-requested `continue all` audit.

## Confirmed defect

`ProjectStateSnapshot` now preserves captured `ProjectElement` and `ProjectFamily` instances during rollback into the exact `ProjectState` that was captured, but `CopyInto(...)` still clears `Zones`/`Floors` and constructs new `ZoneDefinition` / `FloorDefinition` objects for every restore. A caller holding the canonical object returned by `FindZone(...)` or `FindFloor(...)` before a mutation therefore keeps a stale detached reference after rollback, even when the same id existed at snapshot capture.

The already-merged Family identity lane established the intended contract: same-project rollback restores captured canonical objects in place, while detached copies and restores into a different same-id `ProjectState` must remain non-aliasing. Zone/Floor should follow the same contract.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, snapshot source and this claim before writes.
2. Capture canonical Zone and Floor references by case-insensitive id alongside existing Family/Element references, rejecting null/empty/duplicate entries consistently.
3. During restore into the exact captured ProjectState, reuse captured Zone/Floor objects and restore mutable values in place; reinsert captured objects removed after capture and remove post-capture additions.
4. Keep `CreateDetachedCopy(...)` and restore into a foreign same-id ProjectState fully non-aliasing by cloning Zone/Floor objects.
5. Add smoke coverage for same-project identity/value restoration plus detached/foreign isolation for both Zone and Floor.
6. Read back source/test on current `main`; no GitHub Actions and no BricsCAD runtime PASS.
7. Close claim only after source/regression remain visible on current `main`.

## Excluded

- No further Element/Family snapshot changes beyond preserving their existing contract.
- No ProjectSession, QSDB schema, persistence-token, adapter/UI, installer or release changes.
