# Work claim — AuditTrail action control-character canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `764a6ee6078af5267e19be376ebe5d9acf936a76`
- Priority: owner-requested whole-repository audit; Core audit persistence integrity

## Verified defect

`AuditTrail.Record(...)` trims and rejects blank actions, but an otherwise nonblank action may still contain control characters such as `\u0001`. The standard audit API therefore admits an action token that is not a safe canonical persisted identifier; QSDB later serializes `AuditEvent.Action` into an XML attribute, so XML-forbidden control characters can turn an accepted semantic mutation into a later save failure.

## Reserved scope

- `src/QS3D.Core/Audit/AuditTrail.cs`
- `tests/QS3D.Core.SmokeTests/AuditTrailActionCanonicalitySmoke.cs`
- this claim file

## Intended contract

1. Reject action tokens containing control characters before touching project revision/history.
2. Preserve existing outer-whitespace trimming for otherwise valid actions.
3. Preserve audit payload semantics (`ElementId`, `Detail`, `Actor`, `CorrelationId`).
4. Extend the existing action-canonicality smoke with control-character atomicity coverage.

## Non-overlap / validation

- Do not modify QSDB serializer/schema, BricsCAD source, or unrelated audit consumers.
- Re-fetch exact current source/test after this claim lands and before implementation.
- No GitHub Actions dispatch, no force-push, no release publication, and no BricsCAD V25 runtime PASS claim.
