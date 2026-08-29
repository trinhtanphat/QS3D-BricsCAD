# Room Finish visible-total aggregation precision

Status: `SOURCE_GUARDED / REMOTE_SAFE`

Issue: #4694  
Surface: `RoomFinishScheduleWindow.ApplyFilter()`  
V25 source: `src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.xaml.cs`

## Contract

- Visible element `Count` remains checked through `QuantityReportMath.AddCount`.
- Visible Length and Area totals use compensated finite, non-negative accumulation rather than strict pairwise `QuantityReportMath.Add` or raw `+=` folding.
- A mixed-magnitude visible sequence such as `1e16`, `1`, `1` may produce the representable final total `10000000000000002` instead of failing on the first small contribution.
- A materially unrepresentable final compensation, including the `2^53 + 1` class, still fails closed; compensation is not silently discarded merely to display a total.
- Search/filter semantics, row binding, group count, formatting, and the existing Room Finish schedule data model remain unchanged.
- V26 consumes the shared V25 source tree. Do not introduce a divergent V26 implementation for this arithmetic path.

## Source gate

Run:

```text
python scripts/preflight-room-finish-visible-total-aggregation-precision.py
```

The guard requires the compensated accumulator/finalization path, preserves checked Count aggregation, and rejects restoration of pairwise `QuantityReportMath.Add` or `+=` visible Length/Area accumulation.

## TDD evidence

Test-only head `b37c453ec10f76dc48e5d16cf2554ab01ad8a7aa` produced automatic branch run `33265919345`. Reservation/Lane-Key/path collision and generic source guard passed, while `All discovered feature source guards` failed because production `RoomFinishScheduleWindow.ApplyFilter()` still used the old pairwise visible-total fold. This is the intended RED before the production fix.

## Validation boundary

This is deterministic adapter/UI arithmetic and is remotely source/build verifiable. Hosted CI or source review is not a licensed BricsCAD UI runtime claim; no `LOCAL_PASS` is asserted by this runbook.
