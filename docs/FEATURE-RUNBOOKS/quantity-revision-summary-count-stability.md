# Quantity revision summary known-Count stability

Lane-Key: `issue-4446`

`QuantityRevisionReport.Summarize` consumes caller-supplied revision rows before grouping and compensated aggregation. When a source exposes deterministic `Count` metadata through `ICollection<QuantityRevisionRow>`, `IReadOnlyCollection<QuantityRevisionRow>`, or non-generic `ICollection`, that metadata is an admitted cardinality contract rather than a hint.

The contract is fail-closed in two directions. The first traversed row beyond an admitted Count is rejected before null/key/quantity semantic processing, so an unexpected row or later throwing tail cannot outrank the declared cardinality violation. After an exactly sized traversal, all supported Count surfaces are read again; changed, negative, or conflicting post-traversal evidence is rejected before grouping, numeric aggregation, or publication.

Historical #3178 established the initial Count snapshot and observed traversal-length equality. #4446 extends that same boundary with early-overrun precedence and post-traversal Count rebinding while preserving under-yield refusal, canonical quantity-key validation, case-insensitive grouping/order, compensated finite totals, honest counted input, and pure streaming behavior.

Deterministic coverage is self-registering in `tests/QS3D.Core.SmokeTests/QuantityRevisionSummaryCountStabilitySmoke.cs`. The auto-discovered source guard is `scripts/preflight-quantity-revision-summary-count-stability.py`.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core quantity/revision integrity and does not require licensed BricsCAD execution; hosted compilation is not a licensed runtime PASS.
