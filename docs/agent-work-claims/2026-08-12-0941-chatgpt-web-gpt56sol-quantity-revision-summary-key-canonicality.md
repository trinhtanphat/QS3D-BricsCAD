# Work claim — Quantity revision summary key canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:41:00+07:00`
- Baseline main SHA: `b3d1ac9c07b368fb153701c09865074824a0926d`
- Priority: evidence-driven remote-safe revision reporting integrity

## Reason

`QuantityRevisionReport.Build` explicitly rejects non-canonical padded quantity keys, but public `Summarize(IEnumerable<QuantityRevisionRow>)` accepts mutable rows and groups any nonblank `QuantityName` verbatim. As a result, `NetVolumeM3` and ` NetVolumeM3 ` can be emitted as separate semantic summaries instead of failing closed at the summary boundary.

## Intended scope

Require nonblank summary quantity names to be canonical without surrounding whitespace before grouping, while preserving the existing behavior that rows with blank quantity names are ignored, case-insensitive grouping is retained, finite/overflow-safe accumulation remains unchanged, and Build semantics are untouched.

## Changed surfaces

- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.
