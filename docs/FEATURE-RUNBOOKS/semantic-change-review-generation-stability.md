# Semantic change review detached generation stability

## Scope

`SemanticChangeReviewBuilder.Build` publishes revision IDs, element category/grouping evidence, portable field deltas and summary counts. All of that evidence must belong to one detached generation per caller snapshot.

## Fail-closed boundary

The builder first captures a detached generation for both the before and after `RevisionSnapshot` inputs through `RevisionSnapshotDetacher`. Only those detached snapshots may be used for revision-id admission, element indexing/category lookup and `RevisionService.Compare`.

The builder must not retain a live reference from the caller-owned element graph and consult it after comparison traversal begins. A caller-controlled nested map/list traversal may mutate the original snapshot while detachment is in progress; such mutation may affect the caller object after its field was captured, but it cannot rewrite category/grouping evidence already admitted into the detached generation.

There is no retry loop. Existing detacher Count/cardinality integrity checks remain authoritative, and malformed or unstable caller collections fail closed.

## Compatibility

Portable-property filtering, source-handle omission, revision-id canonicality, semantic field classification/order, element ordering and summary calculations are unchanged. `RevisionService.Compare` remains the semantic-delta authority, now operating only on the already detached review generations.

## Deterministic regression

`SemanticChangeReviewSmoke.ReviewUsesOneDetachedCategoryGeneration` supplies an after-element whose hostile Properties enumeration changes the original Category from `StructuralWall` to `StructuralColumn` after the category generation has been captured. The original object proves the mutation occurred, while the published review must retain `StructuralWall` together with the property delta from the same detached generation.

Runtime classification: `NOT_APPLICABLE`; this is deterministic Core revision/review provenance integrity and requires no licensed BricsCAD execution.
