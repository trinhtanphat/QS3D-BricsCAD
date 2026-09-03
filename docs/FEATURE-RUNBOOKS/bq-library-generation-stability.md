# BQ library generation stability

## Scope

`BqLibraryCatalog` accepts both counted collections and raw streaming enumerables. Counted inputs are treated as authoritative enough to admit a bounded snapshot, so publication must fail closed when a second traversal with the same Count exposes a different ordered semantic generation.

The semantic identity of a BQ entry is the exact tuple of `ItemCode`, `Description`, `Unit`, `CategoryPath`, and `ReferenceUnitRate`. Replacement or reordering of same-count counted input is rejected before a catalog/import result is published.

Raw streaming inputs without an authoritative Count remain single-pass compatible and are not replayed.

## Deterministic evidence

`BqLibraryGenerationStabilitySmoke` covers constructor replacement drift, import reorder drift, stable counted replay, and raw streaming compatibility. `scripts/preflight-bq-library-generation-stability.py` is auto-discovered by the aggregate feature guard and pins the source + regression contract.

Runtime classification: `NOT_APPLICABLE`; this is pure Core behavior and does not claim licensed BricsCAD execution.
