# Semantic title-block known Count stability

Canonical source carrier: Issue #4422 / Lane-Key `issue-4422`.

Runtime: `NOT_APPLICABLE`. This is a deterministic Core Documentation integrity contract and does not qualify licensed BricsCAD behavior.

## Boundary

`SemanticTitleBlockParameterMapBuilder.Build(...)` accepts caller-controlled `IEnumerable<SemanticTitleBlockParameterDefinition>`. When that enumerable also exposes `ICollection<T>.Count`, `IReadOnlyCollection<T>.Count`, or non-generic `ICollection.Count`, those surfaces are deterministic cardinality evidence rather than a capacity hint.

The builder must therefore bind the materialized definition generation to the Count evidence that admitted it:

- negative, conflicting, or greater-than-128 known Count fails before enumeration;
- item `knownCount + 1` fails immediately after `MoveNext()` and before `Current` is read/retained;
- pure streaming sources without known Count retain the independent 128-definition ceiling;
- under-yield remains a post-traversal cardinality mismatch;
- after an exactly-sized counted traversal, all supported Count surfaces are read again before semantic validation, ordering, or map publication;
- post-traversal negative, conflicting, or uniformly changed Count evidence fails closed;
- honest stable counted input and enumerable-only input retain existing deterministic destination-tag semantics.

## Deterministic evidence

`SemanticTitleBlockKnownCountIntegritySmoke` is module-initialized and covers:

1. pre-enumeration negative/oversized/conflicting Count rejection;
2. early overrun precedence against a later throwing tail and no read of the overrun element's `Current`;
3. under-yield mismatch;
4. uniform post-traversal Count drift;
5. independent generic/read-only/non-generic surface drift producing post-traversal conflict;
6. post-traversal negative Count;
7. stable exact-at-128 counted input with before/after Count reads;
8. enumerable-only 128-item streaming bound.

`scripts/preflight-semantic-title-block-map.py` additionally pins the production ordering: the known-Count overrun check must precede `result.Add(enumerator.Current)`, and post-traversal Count revalidation must precede publication of the materialized list. Existing pure-Core, explicit field, bounded tag, case-insensitive uniqueness, deterministic ordering, immutable snapshot and no-native-API guards remain authoritative.

## Exclusions

This package does not change title-block native CAD materialization, layouts/viewports, Sheet semantics, field rendering, duplicate-tag policy, release/signing paths, or LOCAL_ONLY qualification under parent #77. Hosted CI must not be represented as `LOCAL_PASS`.