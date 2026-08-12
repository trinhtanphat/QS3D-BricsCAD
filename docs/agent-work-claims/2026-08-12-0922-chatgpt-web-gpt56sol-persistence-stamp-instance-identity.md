# Work claim — ProjectPersistenceStamp instance identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-persistence-stamp-instance-identity-20260812-0922`
- Registered: `2026-08-12T09:22:00+07:00`
- Completed: `2026-08-12T09:24:00+07:00`
- Baseline main SHA: `cb55b30fd16e4d613ac5a105badb99376a149884`
- Priority: P1 persistence false-clean / in-memory project ownership integrity

## Confirmed defect

`ProjectPersistenceStamp` recorded only `ProjectState.ProjectId` and therefore accepted another `ProjectState` instance with the same ID. If that detached/replacement instance had the same `ChangeVersion` as the stamped project, `RequiresSave(...)` could incorrectly report it clean; `MarkSaved(...)` could also advance the stamp from a non-owned instance.

The production coordinator establishes an instance-lifetime contract: it stores one stamp alongside each cached project, creates a new stamp whenever a project is loaded/reloaded, and removes both together on forget.

## Completed fix

`ProjectPersistenceStamp` now captures the exact owning `ProjectState` reference at construction and requires `ReferenceEquals(...)` before `RequiresSave(...)` or `MarkSaved(...)` can operate. The existing ownership error text, saved-version tracking, and `QS3D.RecoveredFromBackup` pending-save behavior are preserved.

## Integration evidence

- Claim registration: `63c2371cba9fa4283e66c9b64b9cccb6b571a588`
- Source fix: `721677a7459a80a6da49d02fad141fd57c212262`
- Focused regression source: `705e03eef01db72a1af3281bac4bf1b9c8189fc1`
- Source read-back on moving `main` confirms exact-instance ownership guard remains present.
- Regression read-back confirms same-ID/same-version replacement rejection, same-instance clean/dirty/save behavior, and backup-recovery pending-save coverage.

## Validation boundary

No GitHub Actions were dispatched. The focused smoke source was committed and inspected but not executed in this web session. No build PASS or BricsCAD V25/V26 runtime PASS is claimed.
