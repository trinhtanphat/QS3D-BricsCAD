# Work claim — QSDB audit action canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `3134625a1ea1b8bb3bde47d6a90ac2db8f526091`
- Priority: evidence-driven persistence/audit integrity

## Confirmed defect

`AuditTrail.Record(...)` now requires a non-blank canonical action name, but the persisted `.qsdb` boundary did not enforce the same invariant. `QsdbProjectXmlSchemaValidator.ValidateAudit(...)` checked only the event shape, while `QsdbProjectStore.ValidateProject(...)` checked audit timestamps but not action identity. Because `ProjectState.AuditEvents` is an exposed mutable list, callers could bypass `AuditTrail.Record`, insert a blank or padded action, and `Save(...)` could serialize it. A hand-edited/current-schema `.qsdb` could likewise load a non-canonical audit action.

## Completed contract

1. Current-schema `.qsdb` audit events now require a present, non-blank action attribute with no leading/trailing whitespace.
2. `Save(...)` now rejects in-memory audit events whose action is blank or padded before publication.
3. `Load(...)` rejects missing/blank/padded persisted audit actions through current-schema validation.
4. Canonical audit actions remain unchanged on round-trip.

## Commits

- Claim registration: `61f3a4aa959cfcda68d2698aa3a4c71d12645417`
- Planning: `f93a2a088ac180ab64528f9ff3cf1e5a30dee306`
- XML persistence validator: `3699ba21766f8b556769ec85467186cd73b3fb78`
- In-memory save validation: `282dfef4abe80623bc54a49a4ea46c8078f1ce90`
- Focused smoke regression source: `ac57fa72b257aa4e44069755df9dd8d6fa241959`

## Validation evidence

- Exact source diffs were read back: the XML validator adds one required-canonical action check; the project-store change adds only the audit-action validation beside the existing UTC check.
- Source and smoke commits were verified as ancestors of observed `main` `86282192d967066258748e455bc226e4dc0ca775` with `behind_by: 0`.
- Concurrent commits observed after the source changes did not modify the persistence source files in this claim.
- Smoke regression source is committed but was not executed through GitHub Actions in this remote session.
- No licensed BricsCAD runtime PASS, CI PASS, build PASS, or release publication is claimed.

## Released scope

This claim is complete and the persistence files above are released for other agents. `AuditTrail.cs` was intentionally not modified by this lane.
