# Dependency-impact enumerator Count stability

## Scope

Core-only deterministic validation for caller-controlled `IEnumerable<string>` roots passed to `DependencyImpactPlanner.Plan`.

## Contract

When the root source exposes a known generic, read-only, or non-generic `Count`, `CanonicalRoots` must revalidate that same known Count immediately before and immediately after caller-controlled `GetEnumerator()` acquisition. A hostile source that grows, shrinks, becomes negative, or exposes conflicting Count evidence during acquisition must fail before the first `MoveNext` or semantic `Current` read. Existing MoveNext/Current/terminal Count integrity, project-root bound, canonical-id validation, dedup/sort behavior, and pure-streaming support remain unchanged.

## Deterministic evidence

Run:

```text
python scripts/preflight-dependency-impact-enumerator-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused smoke covers acquisition-time growth, shrink, negative Count and conflicting Count evidence, requiring zero `MoveNext` and zero `Current` reads on rejection. Stable counted and streaming controls must still produce the expected dependency-impact plan.

## Runtime boundary

No licensed BricsCAD runtime evidence is required. This is deterministic Core input-integrity behavior; do not label remote/static validation as `LOCAL_PASS`.
