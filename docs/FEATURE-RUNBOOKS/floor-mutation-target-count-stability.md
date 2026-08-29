# Floor mutation target Count stability

Lane-Key: `issue-4511`

## Contract

Floor mutation entry points that accept caller-controlled `IEnumerable<ProjectElement>` inputs must fail closed when a supported collection Count disagrees with traversal cardinality. For a known Count `N`, an `(N+1)`th successful `MoveNext()` must be rejected before `IEnumerator.Current` is read. The 10,000 target hard cap has the same no-overread requirement.

`ProjectFloorService.ResolveOwnedElements` therefore enumerates explicitly in this order: `MoveNext` -> known-Count admission -> hard-cap admission -> `Current` -> null/ownership/dedup validation. Completed traversal must still equal the snapshotted Count, project `ChangeVersion` must remain unchanged through enumeration, and current-project ownership must be rebound before mutation.

## Deterministic validation

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-floor-mutation-target-count-stability.py
```

The adversarial smoke independently counts `MoveNext` and `Current` reads and proves a Count=1 source yielding two entries is rejected with `MoveNext=2` and `Current=1`, without changing project version or target floor assignment. Honest counted and pure streaming inputs remain accepted.

Licensed BricsCAD runtime is not part of this Core contract and no `LOCAL_PASS` is implied by these checks.
