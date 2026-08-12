# Work claim — ProjectStateSnapshot family rollback identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-project-state-snapshot-family-identity`
- Registered: `2026-08-12T09:10:00+07:00`
- Last Updated: `2026-08-12T09:10:00+07:00`
- Baseline main SHA: `e7a2576e0e0bdaad3b3483534e02d210f1e9159e`
- Priority: deterministic Core rollback identity defect found during owner-requested evidence-driven continue-all audit
- Task Key: `CORE-PROJECT-STATE-SNAPSHOT-FAMILY-IDENTITY`

## Confirmed defect

`ProjectStateSnapshot.Restore(...)` preserves captured `ProjectElement` identity on same-project rollback but clears `target.Families` and creates new `ProjectFamily` instances. `ProjectFamily` implements `INotifyPropertyChanged`, so a canonical Family reference and any event subscription established before a rollback-protected mutation becomes detached after rollback even though the same semantic family id is restored.

## Reserved scope

Preserve canonical `ProjectFamily` object identity only when restoring into the exact project instance captured by `ProjectStateSnapshot.Capture(...)`. Restore captured Name, Category and Properties into original Family objects and reassemble the captured order. Families added after capture disappear; captured families removed after capture return using their original object references. Keep `CreateDetachedCopy(...)` fully detached.

## Expected surfaces

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- focused Core smoke/regression covering mutate/remove/add + rollback identity/value restoration and detached-copy non-aliasing
- smoke registration only if required by the existing harness
- this claim file

## Coordination / exclusions

- Do not broaden to Zone/Floor/QuantityRule/AuditEvent identity.
- Do not alter element identity semantics from the completed sibling lane.
- Do not touch BricsCAD adapter/UI, persistence store/session, native transactions, or unrelated ACTIVE claims.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime qualification claim.

## Validation plan

Capture a project with two families and retain references/subscriptions; mutate one, remove one, add one, then restore. Prove `ReferenceEquals(project.FindFamily(id), capturedReference)` for both captured families, values/properties/order are restored, the post-capture family is absent, and `CreateDetachedCopy(...)` still does not alias family instances.

## Completion condition

Same-project rollback restores Family semantic values and canonical `ProjectFamily` identity without changing detached-copy semantics, with focused regression evidence committed.
