# Schedule snapshot traversal Count stability

Count evidence exposed by `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection` remains authoritative throughout schedule snapshot traversal. The acquisition rebound from #5331 protects `GetEnumerator()`; this contract protects the two subsequent caller-controlled enumeration operations.

After every `MoveNext()` return, QS3D must re-read the admitted Count interfaces before reading `Current` or accepting end-of-stream. If `MoveNext()` changes Count, the snapshot fails closed with zero `Current` reads. After `Current` returns, QS3D must re-read Count again **before item acceptance**, including before adding that item to the detached list.

These rebounds preserve the existing 10,000-entry ceiling, acquisition rebound, null rejection, overrun/under-yield detection, final Count rebound, deterministic sorting/duplicate rules, and pure streaming behavior for sources that expose no known Count.

Deterministic regression uses one hostile collection that can mutate Count from either `MoveNext()` or `Current`. The `MoveNext()` case requires zero `Current` reads; the `Current` case requires rejection immediately after the one caller-controlled read and before materialization. Stable counted and pure streaming controls remain accepted.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core scheduling collection-integrity behavior; licensed BricsCAD evidence is neither required nor claimed.
