# Audit history known-Count traversal integrity

Lane-Key: `issue-4619`

## Defect boundary

`AuditTrail` reads and validates the project-owned `IList<AuditEvent>`. A supported list Count is an admitted cardinality contract. A successful `MoveNext()` beyond that contract must be rejected before dereferencing caller/backing-list `IEnumerator.Current`.

The same rule applies to the public `Events` snapshot and to mutation preflight used by `Record` and `Clear`. A malformed or drifting history must fail closed before cloning, validating the unexpected event, touching the project, adding an event, or clearing history.

## Deterministic acceptance

- Count=N / enumerator yields N+1: N+1 `MoveNext` is observable, but `Current` is read only N times.
- Count=N / enumerator yields N: post-traversal Count is rebound and must still equal N.
- under-yield and Count drift fail closed.
- histories above 10,000 remain rejected before unsupported event processing.
- stable histories preserve read, Record, Clear, canonical validation, and aggregate text-budget behavior.

## Validation

Run the auto-discovered feature preflight and the Core smoke suite. This is Core-only; licensed BricsCAD/private-DWG runtime evidence is not required and must not be claimed.
