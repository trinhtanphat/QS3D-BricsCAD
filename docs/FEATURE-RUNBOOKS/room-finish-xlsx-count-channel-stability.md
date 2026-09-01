# Room-finish XLSX count-channel stability

## Scope
Protect `RoomFinishXlsxExporter.Export` from mutable multi-interface collection Count contracts while it snapshots caller-provided rows.

## Failure mode
A source can expose consistent `IReadOnlyCollection<T>`, `ICollection<T>`, and non-generic `ICollection` counts at admission, then mutate only one admitted Count channel inside the row indexer. A final `rows.Count` read observes only the read-only channel and can miss the drift.

## Contract
Bind the exact known Count interfaces exposed at admission and revalidate the same interface set and values before and after each row indexer, after each semantic row snapshot, after snapshot traversal, and after row stability validation before filesystem publication. Any count-source shape change, conflicting channel, range violation, or drift fails before destination creation/replacement.

## Deterministic regression
`RoomFinishXlsxCountChannelStabilitySmoke` uses a hostile multi-interface source whose `ICollection<T>.Count` changes from 1 to 2 inside the first indexer while the read-only and non-generic channels remain 1. Export must reject after exactly one indexer read and before touching the destination directory. A stable multi-interface control must still export successfully.

## Validation

```text
python scripts/preflight-room-finish-xlsx-count-channel-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Runtime: `NOT_APPLICABLE`; deterministic Core export correctness only.
