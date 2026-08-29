# Progress snapshot Count stability

## Scope

This runbook validates deterministic Core Progress snapshot materialization. It does not require BricsCAD, a license, or private DWG data.

## Contract

`ProgressDomainContract.Snapshot<T>` treats supported collection `Count` surfaces as an integrity contract, not a capacity hint. Generic, read-only, and non-generic Count evidence must agree before traversal; the first item beyond an admitted Count must fail before `Current`/semantic processing/retention can win; exact traversal must then rebind Count evidence before sorting or canonical digest publication. Sources without deterministic Count evidence remain bounded by `MaximumEntries=10000`.

## Deterministic validation

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-progress-snapshot-count-stability.py
```

The registered smoke covers early overrun/no-overread, under-yield, uniform post-traversal drift, post-traversal interface conflict, stable counted input, and streaming input. The source guard pins ordering so Count overrun remains ahead of streaming-limit, null-entry, and retention semantics.

## Acceptance boundary

PASS means deterministic Core source/build/smoke integrity only. It is not licensed BricsCAD `LOCAL_PASS` evidence.
