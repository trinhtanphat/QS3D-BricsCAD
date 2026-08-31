# Quantity revision summary known-Count stability

Lane-Key: `issue-5087`

`QuantityRevisionReport.Summarize` consumes caller-supplied revision rows before grouping and compensated aggregation. When a source exposes deterministic `Count` metadata through `ICollection<QuantityRevisionRow>`, `IReadOnlyCollection<QuantityRevisionRow>`, or non-generic `ICollection`, that metadata is an admitted cardinality contract rather than a hint.

The contract is traversal-wide and fail-closed. All supported Count surfaces must agree at admission. The admitted Count is rebound immediately before and after every caller-controlled `MoveNext`, immediately after detached `Current` capture, and after traversal. The first traversed row beyond an admitted Count is rejected before observing unexpected `Current` data. A Count change caused transiently by `MoveNext` or `Current` must therefore fail before null/key validation or retention even if the source would restore its original Count at the next iterator boundary.

Negative or conflicting Count evidence is rejected whenever it is observed. Under-yield and post-traversal drift remain rejected before grouping, numeric aggregation, or publication. Pure streaming inputs with no supported Count surface remain valid and preserve the same bounded semantic validation path.

Historical #3178 established the initial Count snapshot and observed traversal-length equality. #4446 added early-overrun precedence and final Count rebinding. #5087 closes the remaining transient traversal gap while preserving canonical quantity-key validation, case-insensitive grouping/order, compensated finite totals, stable multi-interface counted input, and streaming behavior.

Deterministic coverage is self-registering in `tests/QS3D.Core.SmokeTests/QuantityRevisionSummaryCountStabilitySmoke.cs`. Historical source coverage remains in `scripts/preflight-quantity-revision-summary-count-stability.py`; the focused traversal-order guard is `scripts/preflight-quantity-revision-summary-transient-count-integrity.py`.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core quantity/revision integrity and does not require licensed BricsCAD execution; hosted compilation is not a licensed runtime PASS.
