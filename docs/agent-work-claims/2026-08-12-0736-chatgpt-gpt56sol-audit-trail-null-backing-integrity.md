# Work claim — AuditTrail null backing integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-audit-trail-null-backing-integrity`
- Registered: `2026-08-12T07:36:00+07:00`
- Last Updated: `2026-08-12T07:43:00+07:00`
- Baseline main SHA: `4266743037326397e9977b250875f66ac7dd06fa`
- Priority: deterministic Core mutation-integrity defect found during owner-requested evidence-driven audit
- Task Key: `CORE-AUDIT-TRAIL-NULL-BACKING-INTEGRITY`

## Confirmed defect

`AuditTrail.Events` already treated a `null` existing audit entry as corrupt and failed visible. `QsdbProjectStore.ValidateProject(...)` likewise rejected an in-memory project whose `AuditEvents` collection contained a null event before save. However, `AuditTrail.Record(...)` validated only the new action, then called `ProjectState.Touch()` and appended the new event without validating the existing backing collection.

Therefore a malformed project with `project.AuditEvents.Add(null)` could advance `ChangeVersion`/`UpdatedUtc` and append new authoritative audit history through `Record(...)`, even though the same aggregate was invalid for read/persistence.

## Implemented scope

`AuditTrail.Record(...)` now checks the existing backing audit collection for null entries after validating/canonicalizing the requested action but before constructing a committed record, calling `ProjectState.Touch()`, or appending. A null backing entry causes `InvalidOperationException` and leaves repair explicit.

The prior action-canonicalization behavior remains unchanged: blank actions still fail before mutation and valid actions remain trimmed. `AuditTrail.Clear()` was deliberately not changed.

## Committed evidence

- Claim registration: `7e0e4403173f6aef378732b5d45c350c19b81496` — `chore(agent): claim audit trail null backing integrity`
- Core fix: `d48d791feb9e200267bb497e99123695b2565995` — `fix(audit): reject null backing events before record`
- Focused smoke: `0e4299ccc8a2148a799a2c9227946b83909201ec` — `test(audit): guard null backing record atomicity`
- Isolated smoke registration: `1a7d35180deb05e05790f2ba872c3832681ac7f0` — `test(audit): register null backing integrity smoke`
- Moving-main read-back on `a3595336c2426adcd88a58fd301cb67dd89fce7e` confirmed source, smoke and isolated registration remained present after concurrent commits.

The focused smoke creates one valid audit event plus one null backing event, verifies a valid `Record(...)` request is rejected without changing `ChangeVersion`, `UpdatedUtc`, authoritative count/order or the corruption itself, then explicitly removes the null entry and proves a valid record appends once and advances `ChangeVersion` exactly once.

## Preserved behavior / exclusions

- `QsdbProjectStore.cs` and `QsdbProjectXmlSchemaValidator.cs` were not modified; the concurrent QSDB audit-action canonicality lane retained those surfaces.
- Action vocabulary/canonicalization, detail/actor/correlation semantics, timestamp policy and max-version behavior were not changed.
- `AuditTrail.Clear()` remains unchanged and outside this Record-integrity lane.
- No BricsCAD callers or command-specific audit ownership were modified.
- No unrelated ACTIVE claim was overwritten; no force-push or GitHub Actions/build/release dispatch was used.
- No local smoke/.NET execution or BricsCAD runtime qualification is claimed.

## Completion condition

Satisfied: project-bound `AuditTrail.Record(...)` cannot mutate an audit aggregate already known to be structurally invalid because of a null backing event, with focused regression source committed and registered.