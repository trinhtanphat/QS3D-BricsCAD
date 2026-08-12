# Work claim — Audit XML payload safety

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-audit-xml-payload-safety`
- Registered: `2026-08-12T09:52:00+07:00`
- Baseline main SHA: `6d3bdd42b153198bda216e7692a555a06df5800f`
- Priority: P1 persisted-audit atomicity / QSDB save safety found during owner-requested `continue all` audit.

## Confirmed defect

`AuditTrail.Record(...)` validates/normalizes the audit action but currently accepts `elementId`, `detail`, `actor`, and `correlationId` as arbitrary .NET strings, calls `ProjectState.Touch()`, and appends the event. `QsdbProjectStore.Serialize(...)` later persists every one of those values through `XAttribute`. .NET XML serialization rejects XML-invalid characters (including invalid control characters and malformed surrogate content), so a caller can successfully mutate project/audit state and only discover the invalid payload later when QSDB save fails.

The existing audit read-integrity lane is already completed and currently guards null events, UTC timestamps, and action canonicality. This lane extends the persistence-safety boundary without changing audit payload whitespace/redaction semantics.

## Reserved scope

- `src/QS3D.Core/Audit/AuditTrail.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current AuditTrail source and this claim before writes.
2. Validate XML character legality for action, elementId, detail, actor and correlationId before any `Touch()` or event append; preserve existing stricter action control-character rule.
3. Extend stored-history validation so malformed in-memory audit payloads fail visibly through `Events` and block further `Record(...)` calls before project freshness changes.
4. Preserve exact valid payload text, including ordinary whitespace and XML-valid tab/newline/carriage-return characters.
5. Add focused smoke coverage for valid payload round-trip, invalid new payload atomicity across all four non-action fields, invalid action surrogate/control cases, and malformed existing-history read/record rejection without freshness mutation.
6. Read back source/test on current `main`; do not dispatch GitHub Actions or claim BricsCAD runtime PASS.
7. Close claim only after source/regression commits remain visible on current `main`.

## Excluded

- No audit redaction/truncation/length policy changes.
- No QSDB schema/migration or ProjectSession changes.
- No BricsCAD adapter/UI, installer or release changes.
