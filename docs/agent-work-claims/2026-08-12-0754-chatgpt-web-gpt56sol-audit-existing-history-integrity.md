# Work claim — AuditTrail existing-history persistability

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-audit-existing-history-integrity-20260812-0754`
- Registered: `2026-08-12T07:54:00+07:00`
- Baseline main SHA: `4253d55dc6019a2dedbda4b96228091ada484237`
- Priority: P2 — prevent a new audit mutation from advancing a project whose existing audit history already violates QSDB persistence invariants.

## Reserved scope

`AuditEvent` entries are publicly mutable through `ProjectState.AuditEvents`. QSDB save requires every audit timestamp to have `DateTimeKind.Utc` and every action to be non-empty/canonical (no surrounding whitespace). `AuditTrail.Record(...)` already validates the new action and now rejects null existing entries, but it does not preflight these two persistence invariants on existing non-null events. It can therefore `Touch()` the project and append another event to history that is already unsaveable.

## Reserved surfaces

- `src/QS3D.Core/Audit/AuditTrail.cs`
- `tests/QS3D.Core.SmokeTests/AuditExistingHistoryIntegritySmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- Before `Record(...)` mutates revision/history, require every existing non-null audit event to have UTC `Utc` and canonical non-empty `Action` matching QSDB persistence.
- Keep new-action trimming behavior, null-history fail-closed behavior, cloning/read behavior, and `Clear()` as a repair path unchanged.
- Focused smoke proves malformed existing action/timestamp fail without changing `ChangeVersion` or event count, while canonical history still accepts a new record.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no BricsCAD V25 runtime PASS claimed.
