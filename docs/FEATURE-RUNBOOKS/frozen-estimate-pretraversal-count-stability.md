# Frozen estimate pre-traversal Count stability

Lane-Key: `issue-5272`

## Purpose

`FrozenEstimateProjection.Create` accepts counted and streaming `IEnumerable<EstimateLine>` sources. When a source exposes a known Count, that Count is an admission boundary for the immutable projection traversal.

A counted source can run arbitrary code from `GetEnumerator()`. If that call changes the source Count, the previously admitted cardinality is stale before traversal begins. The projection must therefore revalidate the known Count after `GetEnumerator()` and before the first `MoveNext()`, and continue to revalidate before and after every subsequent `MoveNext()`.

## Deterministic regression

`FrozenEstimatePreTraversalCountStabilitySmoke` supplies an `ICollection<EstimateLine>` whose initial Count is zero and whose `GetEnumerator()` changes Count to one. The regression requires rejection through the existing Count-stability contract with zero `MoveNext()` calls. This distinguishes fail-closed admission from detecting the drift only after traversal has already started.

The smoke also keeps stable counted and streaming controls to ensure the stronger boundary does not reject ordinary empty inputs or unknown-count streaming enumerables.

## Preserved behavior

The change does not alter the 10,000-line ceiling, duplicate estimate-line identity checks, row materialization, deterministic sorting, immutable row exposure, or post-traversal Count validation. Unknown-count streaming sources remain supported.

## Validation

Run:

```text
python scripts/preflight-frozen-estimate-pretraversal-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

Protected PR `preflight` and `core` must both be SUCCESS on the exact candidate head before merge.

No licensed BricsCAD runtime is required for this deterministic Core collection/state correctness package.