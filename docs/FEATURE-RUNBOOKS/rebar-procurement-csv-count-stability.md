# Rebar procurement CSV known-Count stability

## Scope

This lane protects `RebarProcurementCsvExporter.ToCsv` when callers provide an enumerable that also exposes collection Count evidence. It is deterministic Core behavior and does not require licensed BricsCAD runtime.

## Integrity contract

At serialization admission, the exporter binds every available `ICollection<RebarProcurementSummary>`, `IReadOnlyCollection<RebarProcurementSummary>`, and non-generic `ICollection` Count. Negative or conflicting evidence fails closed, and an admitted Count above the 10,000-row CSV bound is rejected before enumeration.

For a known-count source, the exporter revalidates Count around each `MoveNext`, refuses to read `Current` after the admitted cardinality, requires the enumerator to yield exactly the admitted Count, and revalidates Count after traversal. Growth, shrink, interface disagreement, under-yield, and post-traversal drift therefore cannot silently produce CSV for a different collection generation.

Sources without Count evidence remain supported as bounded streaming enumerables. Their existing 10,000-row limit remains enforced before reading `Current` for an over-bound row.

## Deterministic evidence

`RebarProcurementCsvCountStabilitySmoke` exercises growth and shrink after a consumed row, under-yield, conflicting Count interfaces, oversized known Count, stable known-count output parity, and pure streaming compatibility. `scripts/preflight-rebar-procurement-csv-count-stability.py` pins the production ordering and forbids reverting the serializer to outer `foreach` traversal.

## Acceptance

Run the repository Shared CI for the exact candidate. Required remote evidence is current protected `preflight` and `core` success, including discovered feature guards and deterministic Core smoke. No `LOCAL_PASS` or private DWG evidence applies to this lane.
