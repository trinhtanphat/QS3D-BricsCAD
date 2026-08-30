# Grid intersection marker known Count stability

`GridIntersectionMarkerPlanner.Plan` accepts counted collections and pure-streaming intersection sources. Supported Count surfaces are treated as input-integrity evidence rather than a capacity hint.

Counted input is admitted before traversal and rebound before every `MoveNext()`, at the terminal edge, after every successful `MoveNext()` before caller-controlled `Current`, and after traversal. Advertised overrun and the independent 100,000-marker ceiling reject before the affected semantic intersection read; under-yield and final Count drift remain fail-closed.

The contract covers generic `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection`, including negative values and conflicting Count surfaces. Pure-streaming sources remain supported to the same hard cap.

Pair-owned identity delegation, occurrence ordering, pair/owner tokens, points, and read-only result semantics remain unchanged. This is deterministic Core integrity work; no licensed BricsCAD/private-DWG `LOCAL_PASS` is required or claimed.
