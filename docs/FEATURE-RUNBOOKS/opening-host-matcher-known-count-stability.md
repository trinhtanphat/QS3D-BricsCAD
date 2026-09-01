# Opening host matcher known Count stability

`OpeningHostMatcher.Match` accepts counted collections and pure-streaming host-segment sources. Supported collection Count surfaces are an input-integrity contract, not a capacity hint.

Counted input is admitted before traversal and rebound immediately before every `MoveNext()`, at the terminal edge, and after every successful `MoveNext()` before observing caller-controlled `Current`. Advertised overrun and the independent 20,000-segment cap are rejected before the affected semantic host read. After every successful `Current`, all supported Count surfaces are rebound again before retaining the host segment. Under-yield and final Count drift remain fail-closed.

The contract covers generic `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection`, including transient growth, shrink, negative values, and conflicts between surfaces activated by either `MoveNext` or `Current`. A hostile source cannot restore its Count after `Current` and escape the immediate post-Current rebound. Pure streaming sources remain supported up to the same hard cap.

Deterministic smoke proves advertised overrun rejects before a second `Current`, MoveNext-time drift rejects with zero `Current` reads, Current-time drift rejects after exactly one `Current` and one immediate post-Current Count rebound, and stable counted/streaming controls still match. The auto-discovered preflight pins `MoveNext -> Count rebound -> advertised/cap guards -> Current -> Count rebound -> retention -> final rebound`.

The change preserves null rejection, numeric/geometry overflow checks, per-host best-candidate reduction, deterministic ordering, and ambiguity semantics. This is deterministic Core integrity work; no licensed BricsCAD/private-DWG `LOCAL_PASS` is required or claimed.
