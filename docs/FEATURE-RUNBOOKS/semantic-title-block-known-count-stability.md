# Semantic title-block known Count stability

Canonical source carriers: Issue #4422 established the admission/post-traversal contract; Issue #4932 / Lane-Key `issue-4932` strengthens caller-controlled traversal stability.

Runtime: `NOT_APPLICABLE`. This is a deterministic Core Documentation integrity contract and does not qualify licensed BricsCAD behavior.

## Boundary

`SemanticTitleBlockParameterMapBuilder.Build(...)` accepts caller-controlled `IEnumerable<SemanticTitleBlockParameterDefinition>`. When that enumerable also exposes `ICollection<T>.Count`, `IReadOnlyCollection<T>.Count`, or non-generic `ICollection.Count`, those surfaces are deterministic cardinality evidence rather than a capacity hint.

The builder must therefore bind the materialized definition generation to the Count evidence that admitted it:

- negative, conflicting, or greater-than-128 known Count fails before enumeration;
- admitted Count is rebound before and immediately after each caller-controlled `MoveNext()`;
- item `knownCount + 1` fails after `MoveNext()` and before `Current` is read/retained;
- after caller-controlled `Current`, admitted Count is rebound again before the definition can be retained;
- pure streaming sources without known Count retain the independent 128-definition ceiling and one-pass behavior;
- under-yield remains a post-traversal cardinality mismatch;
- after traversal, all supported Count surfaces are read again before semantic validation, ordering, or map publication;
- transient or persistent negative, conflicting, unavailable, or changed Count evidence fails closed;
- honest stable counted input and enumerable-only input retain existing deterministic destination-tag semantics.

For a stable one-item source that exposes one supported Count surface, the strengthened traversal contract observes Count seven times: admission; pre/post first `MoveNext`; post-`Current`; pre/post terminal `MoveNext`; and final post-traversal rebound.

## Required ordering

For an admitted counted source, each traversal step is fail-closed in this order:

1. revalidate admitted Count;
2. execute caller-controlled `MoveNext()`;
3. revalidate Count immediately after `MoveNext()`;
4. if an item exists, reject known-count over-yield and the 128-entry ceiling before `Current`;
5. read caller-controlled `Current`;
6. revalidate Count immediately after `Current`;
7. retain the definition and increment observed cardinality;
8. after traversal, require exact observed cardinality and perform the final Count rebound.

A hostile source therefore cannot temporarily mutate Count inside `MoveNext()` or `Current`, restore it before the final rebound, and have inconsistent title-block mapping data admitted.

## Deterministic evidence

`SemanticTitleBlockKnownCountIntegritySmoke` remains authoritative for:

1. pre-enumeration negative/oversized/conflicting Count rejection;
2. early overrun precedence against a later throwing tail and no read of the overrun element's `Current`;
3. under-yield mismatch;
4. uniform post-traversal Count drift;
5. independent generic/read-only/non-generic surface drift producing post-traversal conflict;
6. post-traversal negative Count;
7. stable exact-at-128 counted input;
8. enumerable-only 128-item streaming bound.

`SemanticTitleBlockKnownCountStabilitySmoke` adds deterministic hostile traversal coverage for:

- Count drift caused by `MoveNext()`, proving failure before `Current`;
- Count drift caused by `Current`, proving failure before retention/semantic processing;
- a stable counted one-item control with the strengthened Count-observation budget;
- a pure streaming control that remains supported.

`scripts/preflight-semantic-title-block-known-count-stability.py` pins the production ordering `Count rebound -> MoveNext -> Count rebound -> over-yield/ceiling -> Current -> Count rebound -> retention`, plus the final traversal rebound. `scripts/preflight-semantic-title-block-map.py` and existing Core guards remain authoritative for pure-Core behavior, explicit fields, bounded tags, case-insensitive uniqueness, deterministic ordering, immutable snapshots, and no-native-API constraints.

## Source-safe validation

Run:

```text
python scripts/preflight-semantic-title-block-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Hosted source/Core/V25 compile CI is sufficient for repository integration of this Core-only defect, but it must never be represented as licensed BricsCAD `LOCAL_PASS` evidence.

## Exclusions

This package does not change title-block native CAD materialization, layouts/viewports, Sheet semantics, field rendering, duplicate-tag policy, release/signing paths, or LOCAL_ONLY qualification under parent #77. It strengthens only caller-controlled collection integrity at the Core materialization boundary.