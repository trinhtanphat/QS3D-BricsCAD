# AuditTrail transient known-Count stability

## Purpose

`AuditTrail` treats the persisted `IList<AuditEvent>.Count` as an admitted integrity surface. Read snapshots and mutation prevalidation must reject a custom/corrupt history whose Count changes transiently while enumeration is in progress, even when the source later restores the original value.

## Contract

For both `Events` and `ValidateExistingHistory(...)`:

1. validate the initial stored Count and the 10,000-event ceiling;
2. rebind that same admitted Count immediately before each caller-controlled `MoveNext()`;
3. after a successful `MoveNext()`, rebind again before the cardinality gate and before reading `Current`;
4. on a terminating `MoveNext()`, rebind Count before leaving the enumeration;
5. preserve observed-cardinality equality and a final Count rebound before publication/mutation;
6. continue to validate null/canonical/XML-safe events and the aggregate text budget before exposing, adding, clearing, or touching project state.

A Count mismatch must therefore reject before an event is consumed and before `Record`/`Clear` mutate storage. Stable ordinary `List<AuditEvent>` histories remain readable and mutable.

## Deterministic validation

Run:

```text
python scripts/preflight-audit-transient-count-stability.py
python scripts/preflight-audit-history-count-integrity.py
python scripts/preflight-audit-snapshot-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

`AuditTrailTransientCountStabilitySmoke` uses a hostile `IList<AuditEvent>` whose enumerator changes the exposed Count during its first `MoveNext()` and would restore it from `Current`. The read path must reject transient growth before `Current`; `Record` must reject transient shrink before `Current`/Add; `Clear` must reject negative Count before `Current`/Clear. A normal list remains a positive control.

## Runtime boundary

This is deterministic Core audit/data-integrity behavior. Licensed BricsCAD runtime and private DWG evidence are not required and must not be claimed for this contract.
