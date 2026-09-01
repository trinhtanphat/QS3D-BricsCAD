# Semantic schedule Save known-Count integrity

## Scope

Core-only deterministic validation for `SemanticScheduleCatalog.Save(ProjectState, IEnumerable<SemanticScheduleDefinition>)`. No licensed BricsCAD runtime evidence is required or implied.

## Contract

When the Save input exposes `ICollection<T>.Count`, `IReadOnlyCollection<T>.Count`, or non-generic `ICollection.Count`, all supported Count views are integrity evidence. Save must reject negative, conflicting, oversized, transiently changing, over-yielding, and under-yielding known Count contracts before any metadata mutation.

Traversal ordering is fail-closed:

`admission Count -> pre-MoveNext rebound -> MoveNext -> post-MoveNext rebound -> known-count/capacity guard -> Current -> post-Current rebound -> retain -> terminal count equality -> final rebound -> ValidateCatalog -> serialize/persist`

The 128-definition bound remains authoritative. A known-count overrun is rejected before reading unexpected `Current`. Pure streaming `IEnumerable<T>` inputs remain supported and single-pass.

## Deterministic regression

`SemanticScheduleCatalogSaveKnownCountSmoke` covers:

- known Count zero yielding one definition, rejected before unexpected Current;
- transient Count drift caused by `MoveNext`, rejected before Current;
- transient Count drift caused by `Current`, rejected before retention/persistence;
- known Count under-yield;
- stable counted two-definition Save;
- pure streaming Save.

Every rejected hostile case verifies that project `ChangeVersion` and the semantic-schedule metadata key remain unchanged.

## Validation

Run the auto-discovered feature guard and Core smoke suite through Shared CI. Merge qualification requires protected current-candidate `preflight` and `core` SUCCESS plus strict latest-main freshness and collision cleanliness. Hosted validation must not be called licensed BricsCAD `LOCAL_PASS`.
