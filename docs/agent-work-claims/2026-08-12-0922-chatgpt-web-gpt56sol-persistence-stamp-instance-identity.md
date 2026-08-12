# Work claim — ProjectPersistenceStamp instance identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-persistence-stamp-instance-identity-20260812-0922`
- Registered: `2026-08-12T09:22:00+07:00`
- Baseline main SHA: `cb55b30fd16e4d613ac5a105badb99376a149884`
- Priority: P1 persistence false-clean / in-memory project ownership integrity

## Confirmed defect

`ProjectPersistenceStamp` records only `ProjectState.ProjectId` and therefore accepts any other `ProjectState` instance with the same ID. If that detached/replacement instance happens to have the same `ChangeVersion` as the stamped project, `RequiresSave(...)` can incorrectly return `false`; `MarkSaved(...)` can also advance the stamp from a non-owned instance.

The production coordinator establishes the opposite lifetime contract: it stores one stamp alongside each cached project, creates a new stamp whenever a project is loaded/reloaded, and removes both together on forget. This makes exact in-memory `ProjectState` identity part of the persistence-stamp ownership boundary.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs`
- one focused CAD-independent Core smoke source
- this claim file

## Contract

Bind a persistence stamp to the exact `ProjectState` instance supplied at construction. Reject another instance even when `ProjectId` and `ChangeVersion` match. Preserve same-instance saved-version tracking and backup-recovery pending-save semantics. No changes to sidecar revision stamps, project loading, native document lifecycle, or QSDB serialization.

## Validation plan

Add deterministic smoke coverage proving: same-instance clean/dirty tracking remains unchanged; a different instance with the same project ID is rejected by both `RequiresSave(...)` and `MarkSaved(...)`; recovery metadata still forces pending save. Re-fetch moving `main` before every write and never force-push.

No GitHub Actions dispatch, executable smoke PASS, build PASS, or BricsCAD runtime qualification is claimed by this lane.
