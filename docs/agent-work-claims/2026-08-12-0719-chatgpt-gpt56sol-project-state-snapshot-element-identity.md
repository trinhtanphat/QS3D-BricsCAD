# Work claim — ProjectStateSnapshot element rollback identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-project-state-snapshot-element-identity`
- Registered: `2026-08-12T07:19:00+07:00`
- Last Updated: `2026-08-12T07:19:00+07:00`
- Baseline main SHA: `9e1057b1bc9a8d2786ecc6bdeb7d3e210d4aa4dd`
- Priority: deterministic Core rollback identity defect found during owner-requested evidence-driven continue-all audit
- Task Key: `CORE-PROJECT-STATE-SNAPSHOT-ELEMENT-IDENTITY`

## Confirmed defect

`ProjectStateSnapshot.Restore(...)` restores semantic values by calling `CopyInto(_snapshot, project)`. The current element branch clears `target.Elements` and constructs a new `ProjectElement` for every snapshot element. A `ProjectElement` reference that was canonical and valid before a transaction therefore becomes stale after rollback even when that element existed before the transaction and was restored by id.

This violates rollback identity expectations for callers that resolve a canonical element before a rollback-protected operation and continue using that reference after rollback. It is especially observable for remove/fail/retry flows: the semantic id returns to the project, but the pre-transaction object reference is no longer the canonical object.

## Reserved scope

Preserve canonical `ProjectElement` object identity across `ProjectStateSnapshot.Capture(...).Restore(...)` for every element that existed at capture time. Capture stable references separately from the detached value snapshot; on restore, copy snapshot values back into those original objects and reassemble `ProjectState.Elements` in snapshot order. Elements created after capture must disappear; elements removed after capture must be reinserted using their original captured object reference.

Keep `CreateDetachedCopy(...)` fully detached: it must continue cloning elements and must never alias canonical source elements.

## Expected surfaces

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- one focused Core smoke/regression for mutate + remove + add + rollback identity/value restoration and detached-copy non-aliasing
- smoke registration only if required by the existing test harness
- this claim file

## Coordination / exclusions

- Do not broaden this batch to Zone/Floor/Family/QuantityRule/AuditEvent object identity; their restore value semantics remain unchanged.
- Do not modify BricsCAD adapter/UI, project session/store persistence, command-specific rollback flows, or native DWG transactions.
- Do not change project identity, `UpdatedUtc`, `ChangeVersion`, element dirty/timestamp restoration, or collection ordering semantics except as necessary to restore the captured element order with original objects.
- Do not touch any unrelated `ACTIVE` claim scope, including current Semantic Schedule, Formula Reference, release-manifest or validation-checkpoint lanes.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime qualification claim.

## Validation plan

- Capture a project with at least two canonical elements and retain their object references.
- Mutate fields/collections/dirty state on one captured element, remove another captured element, and add a post-capture element.
- Restore and prove `ReferenceEquals(project.FindElement(id), capturedReference)` for both captured elements, including the removed-then-restored element.
- Prove all captured semantic values, dirty flags/timestamps, project persistence state and element order are restored; the post-capture element is absent.
- Prove `CreateDetachedCopy(project)` still returns different element objects so read-only preview regeneration cannot mutate canonical state.
- Re-fetch `main` and claim collision immediately before every source/test write; read back committed source/test afterward. Do not claim local smoke execution unless actually run.

## Completion condition

Rollback restores both semantic values and canonical `ProjectElement` identity for all elements that existed at capture time while detached copies remain non-aliasing, with focused regression evidence committed.