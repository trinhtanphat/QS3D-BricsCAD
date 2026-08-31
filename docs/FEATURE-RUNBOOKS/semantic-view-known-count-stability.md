# Semantic View known-Count stability

## Scope

`SemanticViewDefinition` category/include/exclude snapshots and `SemanticViewPlanner.BuildCatalog` materialization accept arbitrary `IEnumerable<T>` callers. For sources exposing a supported Count surface, that Count is structural integrity evidence and must remain stable for the entire snapshot traversal.

## Contract

For `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` sources, all exposed Count values must agree, be non-negative, and remain equal to the admitted Count. Count is rebound before and after every caller-controlled `MoveNext`, immediately after `Current` and before retaining the returned item, and again after traversal. Over-yield is rejected before `Current`; under-yield is rejected after traversal.

The existing bounds remain unchanged: category/filter snapshots support at most 100,000 entries and catalog materialization supports at most 10,000 definitions. Pure streaming enumerables remain bounded by observed yield count without manufacturing Count evidence.

## Failure precedence

A hostile source that changes Count from `Current` fails with the canonical Count-stability error before the returned semantic value can enter the detached snapshot. This prevents transient structural drift from being hidden by later semantic validation or by restoring Count at the next loop edge.

## Validation

`SemanticViewDefinitionBoundedSnapshotSmoke` covers the historical first-over-bound behavior, stable defensive snapshots, and a hostile `Current`-induced Count drift with exactly one `Current` read. `preflight-semantic-view-definition-bounds.py` pins the explicit `MoveNext -> Count -> Current -> Count -> retain` ordering and the supported Count surfaces.

Hosted Core/static CI is authoritative for this package. No licensed BricsCAD runtime evidence is required or claimed.
