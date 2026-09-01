# Project State Snapshot Audit Integrity

## Scope

This Core-only contract keeps `ProjectStateSnapshot` rollback/detached state compatible with the canonical `AuditTrail` stored-history contract. `ProjectState.AuditEvents` is a mutable list, so callers can bypass `AuditTrail.Record` and inject invalid event state directly; snapshot creation must fail closed instead of retaining that state.

## Invariant

Before snapshot audit entries are copied, `ProjectStateSnapshot.ValidateCollectionEntries` validates the source history through `AuditTrail.ForProject(source).Events`. This deliberately reuses the existing audit-history policy rather than creating a second validator.

The reused contract rejects, among other invalid stored-history states:

- more than 10,000 stored events or unstable traversal/count behavior;
- aggregate audit text beyond the supported 8 MiB character budget;
- non-UTC timestamps;
- blank, padded, control-bearing or XML-invalid actions;
- non-canonical optional element/correlation identities;
- XML-invalid detail, actor, element-id or correlation-id text.

Valid event order and text are copied exactly. A detached snapshot owns distinct mutable `AuditEvent` objects and does not alias source events.

## Why this is required

QSDB persistence and the public `AuditTrail` API are fail-closed around malformed audit history. Before this fix, snapshot validation only bounded/null-checked `ProjectState.AuditEvents`; direct list mutation could therefore create a rollback snapshot that retained audit state which canonical audit reads or later persistence reject. That breaks the expectation that rollback/detached state is safe to retain and republish.

## Deterministic validation

Run:

```text
python scripts/preflight-project-state-snapshot-audit-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused smoke injects invalid audit state directly into `ProjectState.AuditEvents` and proves both `Capture` and `CreateDetachedCopy` fail without mutating source state. It also proves canonical Unicode audit history is copied exactly into distinct event objects.

## Runtime boundary

No licensed BricsCAD execution is required. This is deterministic Core model-lifecycle/persistence integrity; remote/static/Core evidence must not be reported as `LOCAL_PASS`.
