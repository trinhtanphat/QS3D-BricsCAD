# Opening host matcher known Count stability

`OpeningHostMatcher.Match` accepts counted collections and pure-streaming host-segment sources. Supported collection Count surfaces are an input-integrity contract, not a capacity hint.

Counted input is admitted before traversal and rebound immediately before every `MoveNext()`, at the terminal edge, and after every successful `MoveNext()` before observing caller-controlled `Current`. Advertised overrun and the independent 20,000-segment cap are rejected before the affected semantic host read; under-yield and final Count drift remain fail-closed.

The contract covers generic `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection`, including negative values and conflicts between surfaces. Pure streaming sources remain supported up to the same hard cap.

The change preserves null rejection, numeric/geometry overflow checks, per-host best-candidate reduction, deterministic ordering, and ambiguity semantics. This is deterministic Core integrity work; no licensed BricsCAD/private-DWG `LOCAL_PASS` is required or claimed.
