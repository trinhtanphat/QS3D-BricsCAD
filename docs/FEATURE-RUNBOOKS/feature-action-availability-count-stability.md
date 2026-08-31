# Feature Action availability Count stability

Lane-Key: `issue-4530`

## Contract

`FeatureActionBarBuilder.Build` accepts caller-controlled availability snapshots, so supported collection Count surfaces are part of the trust boundary. A counted source must not expose `IEnumerator.Current` for an entry beyond its advertised Count, Count must remain stable through traversal, and the existing eight-state ceiling must remain authoritative for unknown-count streams.

`SnapshotAvailability` therefore traverses explicitly in this order: successful `MoveNext` -> eight-state ceiling admission -> known-Count admission -> `Current` -> validation/retention. After completed traversal, observed cardinality must equal the admitted Count and supported Count surfaces are rebound to reject drift.

Unknown-count streams remain supported. Their ninth successful `MoveNext` is sufficient to reject the source at the established eight-state ceiling without reading ninth `Current` inside `SnapshotAvailability`.

## Deterministic validation

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-feature-action-availability-count-stability.py
```

The smoke independently measures Count reads, `MoveNext` calls and `Current` reads. A Count=1 source yielding two states must fail on the second successful `MoveNext` with exactly one `Current` read. A source whose Count changes only after an otherwise exact traversal must fail during Count rebound. Honest counted availability and existing lazy boundary+1 behavior remain accepted/rejected at their established boundaries.

This is a deterministic Core UI/domain integrity contract. Licensed BricsCAD runtime is not required and no `LOCAL_PASS` is implied.
