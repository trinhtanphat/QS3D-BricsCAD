# Work claim — AuditTrail null backing integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-audit-trail-null-backing-integrity`
- Registered: `2026-08-12T07:36:00+07:00`
- Last Updated: `2026-08-12T07:36:00+07:00`
- Baseline main SHA: `4266743037326397e9977b250875f66ac7dd06fa`
- Priority: deterministic Core mutation-integrity defect found during owner-requested evidence-driven audit
- Task Key: `CORE-AUDIT-TRAIL-NULL-BACKING-INTEGRITY`

## Confirmed defect

`AuditTrail.Events` already treats a `null` existing audit entry as corrupt and fails visible. `QsdbProjectStore.ValidateProject(...)` likewise rejects an in-memory project whose `AuditEvents` collection contains a null event before save. However, `AuditTrail.Record(...)` validates only the new action, then calls `ProjectState.Touch()` and appends the new event without validating the existing backing collection.

Therefore a malformed project with `project.AuditEvents.Add(null)` can still advance `ChangeVersion`/`UpdatedUtc` and append new authoritative audit history through `Record(...)`, even though the same aggregate is invalid for read/persistence. This is a mutation-before-corruption-check inconsistency.

## Reserved scope

Make `AuditTrail.Record(...)` fail before `ProjectState.Touch()` and before append when the existing backing audit collection contains a null entry. Preserve the just-completed action canonicalization contract and all valid record payload semantics.

## Expected surfaces

- `src/QS3D.Core/Audit/AuditTrail.cs`
- dedicated focused Core smoke + isolated registration if needed
- this claim file

## Coordination / exclusions

- Do not modify `QsdbProjectStore.cs` or `QsdbProjectXmlSchemaValidator.cs`; the current QSDB audit-action canonicality lane owns persistence surfaces.
- Do not alter action vocabulary/canonicalization, detail/actor/correlation semantics, timestamp policy, or max-version behavior.
- Do not modify `AuditTrail.Clear()` in this lane; explicit clearing may be used as destructive repair and is outside this Record-integrity defect.
- Do not modify BricsCAD callers or command-specific audit ownership.
- Do not overwrite any other ACTIVE claim; no GitHub Actions/build/release dispatch and no runtime PASS claim.

## Validation plan

- Construct a project-bound trail with an existing `null` audit entry.
- Call `Record(...)` with an otherwise valid canonical action and require `InvalidOperationException`.
- Prove authoritative audit count/order remains unchanged and the null entry remains untouched for explicit repair.
- Prove `ProjectState.ChangeVersion` and `UpdatedUtc` remain unchanged on rejection.
- Preserve valid Record behavior and existing action canonicalization/max-version tests.
- Re-fetch `main`, source blob and claim collision before every write; read back committed source/test before closure.

## Completion condition

Project-bound `AuditTrail.Record(...)` cannot mutate an audit aggregate already known to be structurally invalid because of a null backing event, with focused regression evidence committed.