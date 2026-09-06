# Mapping catalog known-Count generation binding

## Scope

REMOTE_SAFE managed Core validation for `MeasurementWorkItemMappingCatalog`. No licensed BricsCAD runtime evidence is required or implied.

## Invariant

A mapping source that exposes a supported collection `Count` is admitted as one cardinality generation. The catalog must not accept a mapping returned while that admitted Count has changed, even if the source restores its original Count before traversal completes.

The constructor therefore uses explicit enumerator boundaries and revalidates the exact admitted Count:

1. immediately after each `MoveNext`, before reading `Current`;
2. immediately after each `Current` read, before validating or accepting that mapping;
3. after terminal traversal, before final cardinality agreement and publication.

Negative or conflicting Count-family observations fail at the same rebound boundaries. Pure streaming `IEnumerable<MeasurementWorkItemMapping>` inputs without a supported Count remain supported. The existing 10,000-entry limit, null/duplicate/ambiguous mapping checks and deterministic sorting remain unchanged.

## Regression coverage

`MeasurementWorkItemMappingCatalogKnownCountGenerationSmoke` uses hostile counted sequences whose Count changes inside `MoveNext` or `Current` and then restores on terminal traversal. These sequences were accepted by the old before/after-only contract; they now fail before an unstable mapping can be accepted. Stable counted and pure-streaming controls remain accepted.

`scripts/preflight-measurement-work-item-mapping-catalog-known-count-generation.py` pins explicit enumeration and the MoveNext → Count rebound → Current → Count rebound → mapping-acceptance ordering.

## Validation

Run the focused preflight and deterministic Core smoke suite, then require fresh exact-head protected `preflight` and `core` SUCCESS. Reconcile non-force with latest protected `main` when stale and merge only while review/collision/freshness gates remain clean.
