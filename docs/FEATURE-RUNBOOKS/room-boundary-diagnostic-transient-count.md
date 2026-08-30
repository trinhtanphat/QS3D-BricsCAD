# Room boundary diagnostic transient Count integrity

`RoomBoundaryDiagnosticService.Analyze` accepts both counted and pure-streaming segment sources. Count metadata from generic, read-only, and non-generic collection surfaces is an integrity contract, not merely a capacity hint.

For counted sources, diagnostic materialization must rebind the admitted Count before each `MoveNext()` and again after every successful `MoveNext()` before observing `Current`. Known-count overrun and the independent 5,000-segment cap are admitted before semantic segment reads; terminal and final rebounds preserve under-yield and final-stability detection.

This closes the gap where a hostile source could alter Count after `MoveNext()`, restore it from `Current`, and hide the transient violation from the old final-count comparison. Adversarial smoke separately instruments Count, `MoveNext`, and `Current` for growth, shrink, invalid negative metadata, cross-interface conflict, and advertised overrun.

The change does not alter topology discovery, tolerance/minimum-area policy, source-provenance UTF-16 checks, privacy-safe fingerprints, reason classification, accepted-boundary handoff, or V25 Room Auto runtime acceptance. Pure streaming inputs remain supported up to 5,000 traversed segments.

This is deterministic Core data-integrity work; no licensed BricsCAD or private DWG `LOCAL_PASS` is required or claimed.
