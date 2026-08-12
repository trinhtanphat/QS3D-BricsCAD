# Work claim — QSDB audit action canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `3134625a1ea1b8bb3bde47d6a90ac2db8f526091`
- Priority: evidence-driven persistence/audit integrity

## Confirmed defect

`AuditTrail.Record(...)` now requires a non-blank canonical action name, but the persisted `.qsdb` boundary does not enforce the same invariant. `QsdbProjectXmlSchemaValidator.ValidateAudit(...)` checks only the event shape, while `QsdbProjectStore.ValidateProject(...)` checks audit timestamps but not action identity. Because `ProjectState.AuditEvents` is an exposed mutable list, callers can bypass `AuditTrail.Record`, insert a blank or padded action, and `Save(...)` can serialize it. A hand-edited/current-schema `.qsdb` can likewise load a non-canonical audit action.

This creates two authorities for the same provenance identity: runtime-recorded events are canonical, persisted events are not necessarily canonical.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- focused `QS3D.Core.SmokeTests` coverage for save/load rejection
- `docs/plans/2026-08-12-qsdb-audit-action-canonicality.md`
- this claim file

## Non-overlap

- Do not modify `AuditTrail.cs`; the just-completed AuditTrail action lane explicitly excluded persistence schema changes.
- Do not introduce an action enum/vocabulary, length policy, or normalization for detail/actor/correlation fields.
- Do not modify native BricsCAD callers, updater/release code, or unrelated persistence locking/recovery behavior.
- No GitHub Actions dispatch or release publication.

## Intended contract

1. Current-schema `.qsdb` audit events require a present, non-blank action attribute with no leading/trailing whitespace.
2. `Save(...)` rejects an in-memory audit event whose action is blank or padded before publishing the destination file.
3. `Load(...)` rejects a malformed persisted audit action rather than silently accepting a provenance alias.
4. Canonical audit actions round-trip unchanged.

## Validation / closure

- Commit claim before source edits.
- Commit planning document before implementation.
- Re-fetch exact current source immediately before each write.
- Add isolated Core smoke regression without touching shared `Program.cs` when possible.
- Verify source/test commits remain ancestors of latest `main` with `behind_by: 0` before closing.
- Do not claim CI/native runtime PASS unless actually executed.
