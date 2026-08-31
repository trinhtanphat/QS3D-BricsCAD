# Progress snapshot Current-induced Count integrity

`ProgressDomainContract.Snapshot<T>` materializes caller-controlled progress collections for immutable snapshot construction. When the source exposes a supported known `Count`, that cardinality is evidence and must remain stable at every caller-controlled traversal boundary.

The required per-item ordering is:

`MoveNext -> Count rebound -> overrun/limit checks -> Current -> Count rebound -> null validation -> retention`.

The post-`Current` rebound is intentional. A hostile or stateful enumerator can mutate an `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection` count while evaluating `Current`. Cardinality-integrity failure must therefore win before ordinary null-item validation or retention of the returned item.

The existing protections remain part of the contract: supported Count surfaces must agree, negative and over-limit counts fail closed, Count is checked before and after `MoveNext`, overrun is rejected before reading `Current`, under-yield is rejected, post-traversal drift is rejected, the 10,000-entry ceiling remains enforced, and pure streaming `IEnumerable<T>` sources remain supported without inventing Count evidence.

Deterministic coverage lives in `ProgressSnapshotCurrentCountIntegritySmoke.cs`. Its hostile source admits Count 1, mutates Count to 2 from `Current`, and returns a null payload. The expected result is the known-count-changed `ArgumentException`; the null-entry diagnostic must not win. Stable counted and streaming controls prove the valid paths remain accepted.

`preflight-progress-snapshot-current-count-integrity.py` pins the production ordering and smoke/runbook evidence. Runtime classification is `NOT_APPLICABLE`: this is deterministic Core behavior and does not require licensed BricsCAD execution.
