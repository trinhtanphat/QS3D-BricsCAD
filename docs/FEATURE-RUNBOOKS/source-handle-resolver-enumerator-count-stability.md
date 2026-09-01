# SourceHandleResolver enumerator Count stability

## Scope

Core-only deterministic validation for caller-controlled root semantic element IDs passed to `SourceHandleResolver.Resolve`.

## Contract

When the root source exposes generic, read-only, or non-generic known `Count`, `MaterializeRootElementIds` must revalidate that Count immediately before and immediately after caller-controlled `GetEnumerator()` acquisition. Acquisition-time growth, shrink, negative Count, or conflicting Count evidence must fail before first `MoveNext` or semantic `Current` access. Existing 10,000-entry cap, pre/post-MoveNext and post-Current Count rebounds, completed-cardinality validation, canonical semantic-id handling, project freshness/ownership checks, and pure streaming support remain intact.

## Deterministic evidence

Run:

```text
python scripts/preflight-source-handle-resolver-enumerator-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused smoke proves acquisition-time growth, shrink, negative Count and conflicting Count rejection with zero `MoveNext` and zero `Current` reads, plus stable counted and streaming controls.

## Runtime boundary

No licensed BricsCAD runtime is required. This is deterministic Core input-integrity behavior; remote/static validation must not be reported as `LOCAL_PASS`.
