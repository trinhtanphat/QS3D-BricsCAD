# Work claim — ProjectStateSnapshot family rollback identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-project-state-snapshot-family-identity`
- Registered: `2026-08-12T09:10:00+07:00`
- Last Updated: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `e7a2576e0e0bdaad3b3483534e02d210f1e9159e`
- Implementation merge SHA: `9e0f512e19bc1cf6ae89660c6d10759b1fd5e64e`
- Pull Request: `#683`
- Priority: deterministic Core rollback identity defect found during owner-requested evidence-driven continue-all audit
- Task Key: `CORE-PROJECT-STATE-SNAPSHOT-FAMILY-IDENTITY`

## Confirmed defect

`ProjectStateSnapshot.Restore(...)` preserved captured `ProjectElement` identity on same-project rollback but cleared `target.Families` and created new `ProjectFamily` instances. `ProjectFamily` implements `INotifyPropertyChanged`, so a canonical Family reference and any event subscription established before a rollback-protected mutation became detached after rollback even though the same semantic family id was restored.

## Completed implementation

- Capture canonical `ProjectFamily` references by case-insensitive family id alongside the detached semantic snapshot.
- Reuse those captured Family instances only when restoring into the exact `ProjectState` instance that was captured.
- Restore Name, Category and Properties into the original Family object, preserving reference identity and existing `PropertyChanged` subscriptions.
- Reassemble captured family order, remove post-capture families, and reinsert captured families that were removed after capture.
- Keep `CreateDetachedCopy(...)` fully detached and keep restore into a foreign same-id `ProjectState` non-aliasing.
- Preserve the previously completed `ProjectElement` rollback identity behavior without broadening to Zone/Floor/QuantityRule/AuditEvent identity.

## Regression evidence committed

`tests/QS3D.Core.SmokeTests/ProjectStateSnapshotFamilyIdentitySmoke.cs` covers:

- mutate/remove/add + same-project rollback identity/value restoration;
- continued `PropertyChanged` subscription behavior on the canonical Family after rollback;
- captured order restoration and removal of post-capture families;
- detached-copy Family non-aliasing;
- foreign same-project-id restore non-aliasing.

The smoke is registered in `SmokeTestRegistration`. Source and regression files were read back directly from `main` after squash merge, and `9e0f512e19bc1cf6ae89660c6d10759b1fd5e64e` was verified as an ancestor of the then-current `main`.

## Validation boundary

No GitHub Actions were dispatched for this lane. No local/full build, executable smoke, or licensed BricsCAD V25 runtime PASS is claimed.

## Completion condition

Completed: same-project rollback restores Family semantic values and canonical `ProjectFamily` identity without changing detached-copy or foreign-target isolation semantics, with focused regression evidence committed on `main`.
