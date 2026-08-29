# Floor mutation target Count stability

Lane-Key: `issue-4511`

## Contract

Floor mutation entry points that accept caller-controlled `IEnumerable<ProjectElement>` inputs must fail closed when a supported collection Count disagrees with traversal cardinality, without exposing `IEnumerator.Current` for entries beyond the advertised Count.

The existing 10,000-target streaming ceiling retains precedence over a dishonest smaller Count. `ProjectFloorService.ResolveOwnedElements` therefore enumerates explicitly in this order: successful `MoveNext` -> increment observed traversal cardinality -> hard-cap admission -> known-Count admission -> `Current`. Once observed traversal exceeds a known Count, the extra entry is counted but `Current` is not read; bounded `MoveNext` traversal continues until exhaustion (then Count mismatch fails) or entry 10,001 (then the hard-cap contract fails first).

Completed traversal must equal the snapshotted Count, project `ChangeVersion` must remain unchanged through enumeration, and current-project ownership must be rebound before mutation. Honest counted and pure streaming inputs remain supported.

## Deterministic validation

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-floor-mutation-target-count-stability.py
```

The adversarial smoke independently counts `MoveNext` and `Current` reads. A Count=1 source yielding two entries must complete the bounded traversal with three `MoveNext` calls (two true plus terminal false), only one `Current` read, then fail the Count/traversal equality check without project mutation. Existing Floor bound coverage additionally requires a dishonest Count=1 source that keeps yielding to fail at entry 10,001 with the hard-cap diagnostic while never reading `Current` beyond the advertised Count.

Licensed BricsCAD runtime is not part of this Core contract and no `LOCAL_PASS` is implied by these checks.
