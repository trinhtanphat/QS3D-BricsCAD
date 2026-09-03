# Rate reference graph generation stability

Issue: #5510

`RateReferenceGraph` accepts arbitrary `IEnumerable<RateReferenceEdge>` input. Sources that expose an authoritative known `Count` are treated as caller-owned replayable collections and must prove that the ordered semantic edge generation is unchanged before the graph publishes its immutable snapshot.

The admission traversal retains all existing null, duplicate, maximum-entry, Count-conflict, Count-drift, under-run and over-run checks. After admission, known-Count sources are replayed exactly once and compared positionally by `SourceRateCode`, `TargetKind`, and `TargetId`. Same-count replacement, reorder, or semantic edge drift fails closed with `Rate reference edge source content changed during traversal.`

Sources without an authoritative known Count remain streaming-compatible and are consumed exactly once.

Deterministic verification is provided by `RateReferenceGraphGenerationStabilitySmoke` and `scripts/preflight-rate-reference-graph-generation-stability.py`. Hosted Core/static CI is authoritative for this source-only package; licensed BricsCAD runtime evidence is not applicable.
