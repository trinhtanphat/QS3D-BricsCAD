# Polygon source-loop known-Count integrity

Issue: #5009
Lane-Key: `issue-5009`

## Scope

Core-only deterministic validation for `PolygonSourceLoopRegionAssembler.Assemble(IEnumerable<PolygonSourceLoop2>)`. No licensed BricsCAD runtime evidence is required or implied.

## Contract

When source loops expose `ICollection<T>.Count`, `IReadOnlyCollection<T>.Count`, or non-generic `ICollection.Count`, that Count is integrity evidence. Assembly rejects negative, conflicting, oversized (>1024), transiently changing, over-yielding, and under-yielding known Count contracts before geometry normalization can accept an unexpected loop.

Traversal is fail-closed: admission Count -> pre-MoveNext rebound -> MoveNext -> post-MoveNext rebound -> declared-count/1024 capacity guard -> Current -> post-Current rebound -> retain -> terminal equality -> final rebound -> source identity/geometry normalization.

Known Count overrun is rejected before unexpected `Current`. Pure streaming enumerable input remains supported and single-pass; its 1024 capacity guard also runs before reading Current for item 1025.

## Regression

The registered `PolygonSourceLoopRegionAssemblerSmoke` covers Count=0 over-yield with zero unexpected Current reads, transient MoveNext Count drift, transient Current Count drift, under-yield, stable counted input, and pure streaming input, in addition to the existing topology/identity cases.

## Validation

Run the auto-discovered focused preflight and the deterministic Core smoke suite through Shared CI. Merge requires exact-current-candidate protected `preflight` and `core` success, collision cleanliness, and strict latest-main freshness. Hosted/static evidence must not be called licensed BricsCAD `LOCAL_PASS`.
