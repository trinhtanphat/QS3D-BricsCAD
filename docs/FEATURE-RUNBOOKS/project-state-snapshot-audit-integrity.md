# Project State Snapshot Audit Integrity

## Scope

This Core-only contract keeps `ProjectStateSnapshot` rollback/detached state compatible with canonical audit integrity while preserving the established snapshot null-backing fidelity contract. `ProjectState.AuditEvents` is a mutable list, so callers can bypass `AuditTrail.Record` and inject invalid event state directly; snapshot creation must reject malformed non-null state before retaining it.

## Invariant

Before snapshot audit entries are copied, `ProjectStateSnapshot.ValidateCollectionEntries` calls `AuditTrail.ValidateSnapshotHistory(source)`. The internal snapshot path reuses the same bounded traversal, aggregate text accounting, UTC checks, canonical identity checks and XML checks used by `AuditTrail`; it does not copy those rules into snapshot code.

Snapshot compatibility has one explicit exception: historical snapshots are allowed to retain an `AuditEvent.Action == null` backing value. `ProjectStateSnapshotNullFidelitySmoke` already establishes this rollback/detached-copy behavior. The compatibility mode does **not** permit blank/padded/control-bearing/XML-invalid non-null actions, and it does not relax public `AuditTrail.Events`, `Record` or `Clear` validation.

The centralized contract therefore rejects, among other invalid snapshot histories:

- more than 10,000 stored events, unstable traversal/count behavior, or aggregate audit text beyond the supported 8 MiB character budget;
- non-UTC timestamps;
- blank, padded, control-bearing or XML-invalid **non-null** actions;
- non-canonical optional element/correlation identities;
- XML-invalid detail, actor, element-id or correlation-id text.

Valid event order and text are copied exactly. A detached snapshot owns distinct mutable `AuditEvent` objects and does not alias source events.

## CI remediation boundary

The first implementation called strict `AuditTrail.Events` directly. Protected Core smoke correctly exposed that as an overconstraint because `ProjectStateSnapshotNullFidelitySmoke` intentionally preserves null audit backing. The remediation keeps that historical smoke unchanged and moves the compatibility decision into a narrow internal AuditTrail validation mode. This preserves one policy implementation while keeping ordinary AuditTrail consumers strict.

## Deterministic validation

Run:

```text
python scripts/preflight-project-state-snapshot-audit-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused smoke injects invalid audit state directly into `ProjectState.AuditEvents` and proves both `Capture` and `CreateDetachedCopy` fail without mutating source state. It also proves canonical Unicode audit history is copied exactly into distinct event objects. The existing null-fidelity smoke proves a legacy null action backing survives detached copy and rollback unchanged.

## Runtime boundary

No licensed BricsCAD execution is required. This is deterministic Core model-lifecycle/persistence integrity; remote/static/Core evidence must not be reported as `LOCAL_PASS`.
