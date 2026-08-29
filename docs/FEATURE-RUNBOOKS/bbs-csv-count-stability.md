# BBS CSV known-Count stability

## Scope

This lane protects `RebarCsvExporter.ToCsv` when a BBS row enumerable also exposes deterministic collection Count evidence. It is deterministic Core behavior; no licensed BricsCAD runtime is required.

## Integrity contract

At admission the exporter binds all available `ICollection<RebarScheduleRow>`, `IReadOnlyCollection<RebarScheduleRow>`, and non-generic `ICollection` Counts. Negative or conflicting evidence fails closed, and a known Count above the 10,000-row bound is rejected before enumeration.

During traversal the exporter validates Count around `MoveNext`, refuses to read `Current` after the admitted cardinality, requires exact known-count yield, and validates Count again after traversal. Growth, shrink, interface disagreement, under-yield, and post-traversal drift therefore cannot silently serialize a different collection generation.

Sources without Count evidence retain bounded streaming behavior.

## Deterministic evidence

`BbsCsvCountStabilitySmoke` covers growth and shrink after consumption, under-yield, conflicting interfaces, oversized admission, stable known-count output parity, and pure streaming compatibility. `scripts/preflight-bbs-csv-count-stability.py` pins the production ordering and forbids regression to outer `foreach` traversal.

## Acceptance

The exact candidate requires fresh Shared CI protected `preflight` and `core` success, including discovered feature guards, Core build/smoke, trusted V25 compile references, V25 plugin build, and final build. No remote `LOCAL_PASS` applies.
