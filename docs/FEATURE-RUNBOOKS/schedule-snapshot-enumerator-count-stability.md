# Schedule snapshot enumerator-acquisition Count stability

`ScheduleSnapshot` accepts counted collections and pure streaming enumerables for activities, dependencies, and quantity links. Count evidence from `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` is an integrity contract, not only a capacity hint.

For counted sources, QS3D reads and validates the admitted Count interfaces before enumeration. Because `GetEnumerator()` is caller-controlled and may mutate the collection, QS3D must reacquire all known Count evidence immediately after `GetEnumerator()` returns and reject growth, shrink, negative values, or interface disagreement **before first MoveNext**. A rejected acquisition-time drift must therefore perform zero traversal reads.

The existing 10,000-entry ceiling remains authoritative. The implementation must also preserve null-entry rejection, overrun/under-yield detection, a post-traversal Count rebound, deterministic sorting/duplicate validation, and acceptance of pure streaming sources that expose no Count contract.

Deterministic regression coverage uses a hostile counted activity source whose `GetEnumerator()` changes all admitted Counts. The expected result is `ArgumentException` with `MoveNextCalls == 0`. Stable counted activities and a pure streaming source are positive controls.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core scheduling collection-integrity behavior and does not require licensed BricsCAD execution.
