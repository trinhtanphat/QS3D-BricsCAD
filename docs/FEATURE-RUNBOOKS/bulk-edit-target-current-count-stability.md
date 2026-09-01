# Bulk-edit target Current-induced Count stability

Issue: #4854  
Lane-Key: `issue-4854`  
Runtime: `NOT_APPLICABLE` — deterministic Core/input-integrity behavior.

## Contract

Bulk mutation target enumeration treats every supported collection `Count` surface as an admitted integrity boundary. After a successful `MoveNext`, the traversal must reject overrun before reading `Current`, read `Current` exactly once, and immediately rebind every originally admitted Count surface before accepting/materializing that target.

A `Current` getter that causes Count growth, shrink, a negative Count, disappearance/addition of a supported Count surface, or disagreement between supported Count interfaces must fail closed before the returned target is accepted. Existing pre/post-`MoveNext` rebounds, the 10,000-entry cap, under-yield/final rebounds, project ownership/canonical identity checks, ChangeVersion freshness and all-or-nothing mutation semantics remain mandatory.

Both BulkEdit input boundaries are covered: object targets (`IEnumerable<ProjectElement>`) and id targets (`IEnumerable<string>`).

## Deterministic validation

Run:

```text
python scripts/preflight-bulk-edit-target-current-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The source guard is auto-discovered by aggregate feature preflight and pins the ordering `Count rebound -> MoveNext -> Count rebound -> overrun guard -> Current exactly once -> Count rebound -> target acceptance` independently for both BulkEdit traversals.

Licensed BricsCAD runtime evidence is neither required nor claimed for this Core-only package.
