# Work claim — Audit XML payload safety

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-audit-xml-payload-safety`
- Registered: `2026-08-12T09:52:00+07:00`
- Baseline main SHA: `6d3bdd42b153198bda216e7692a555a06df5800f`
- Regression commit: `a31aba4fe367cde50fc05dc49c419385976aa7ae`
- Completed source commit: `e4689ee03e7462eed267c5feb0768ff8bdc6e6fc`
- Readback main SHA before close-out: `e8c5c8f179e56d9449f7afe26b2a702957fd6254`
- Priority: P1 persisted-audit atomicity / QSDB save safety found during owner-requested `continue all` audit.

## Confirmed defect

`AuditTrail.Record(...)` validated/normalized the audit action but accepted `elementId`, `detail`, `actor`, and `correlationId` as arbitrary .NET strings, then called `ProjectState.Touch()` and appended the event. `QsdbProjectStore.Serialize(...)` persists every one of those values through `XAttribute`; .NET XML serialization rejects XML-invalid characters, so an audit mutation could succeed in memory and only fail later during QSDB save.

The previous audit read-integrity lane was already complete and guarded null events, UTC timestamps, and action canonicality. This lane extends persistence safety without changing payload whitespace/redaction semantics.

## Implemented contract

1. `Record(...)` keeps the existing required/trimmed/control-character action policy.
2. Before any project `Touch()` or event append, `XmlConvert.VerifyXmlChars(...)` now validates action, elementId, detail, actor and correlationId.
3. Invalid new payload throws `ArgumentException` before project freshness or audit collection changes.
4. Existing in-memory audit history now fails visibly when any persisted payload field contains XML-invalid character content, and such history blocks a subsequent `Record(...)` before freshness mutation.
5. Valid payload strings are not trimmed/canonicalized; ordinary whitespace and XML-valid tab/newline/carriage-return content remain preserved.
6. Focused smoke coverage uses the real QSDB writer/loader for valid payload round-trip and covers all four non-action fields, an invalid action surrogate, and malformed-existing-history read/record rejection atomicity.

## Verification

- Current-main source readback confirmed XML validation occurs before `ValidateExistingHistoryForRecord()`, `Touch()` and `_events.Add(...)`, and stored history checks all persisted payload fields.
- Current-main smoke readback confirmed valid QSDB round-trip plus invalid-new/existing payload cases.
- `e4689ee03e7462eed267c5feb0768ff8bdc6e6fc...main` compared as `ahead` with the source commit as merge base; subsequent concurrent commits touched unrelated structural-wall/project-name/floor claim files.
- Smoke source was committed but not executed from this remote connector session. Full Core smoke execution/build and GitHub Actions were not run; no PASS is fabricated.
- This is Core audit/persistence work and makes no licensed BricsCAD runtime claim.

## Excluded

- No audit redaction/truncation/length policy changes.
- No QSDB schema/migration or ProjectSession changes.
- No BricsCAD adapter/UI, installer or release changes.
