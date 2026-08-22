# Work claim — Quantity revision readonly result snapshots

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:05:00+07:00`
- Completed: `2026-08-12T09:35:00+07:00`
- Baseline main SHA: `bd5a2bd242ddc924fd68c84867492e96d0e96ccd`
- Claim commit: `6deaa656fa694b23e739927d70f6d46d13ceb458`
- Source fix commit: `7c8963f03259a197da9b9fa41f6cec32fe095f47`
- Smoke commit: `8f0b933ac03a737ec6dbc24d9b69aec7fd7677bd`
- Registration commit: `bf5c60c47590cbd5b28c57458e6875538373b5cf`
- Priority: evidence-driven remote-safe Core result integrity during owner-requested `continue all`

## Reserved scope

Make the collections returned by `QuantityRevisionReport.Build(...)` and `QuantityRevisionReport.Summarize(...)` structurally read-only at runtime instead of exposing mutable `List<T>` instances through `IReadOnlyList<T>` interfaces.

## Expected surfaces

- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- one dedicated `QS3D.Core.SmokeTests` regression file plus isolated module-initializer registration
- this claim file

## Excluded scope

- No changes to `QuantityRevisionRow` / `QuantityRevisionSummary` property mutability or value semantics.
- No revision snapshot persistence, XML schema, dependency canonicalization, capture identity, compare semantics, quantity math, percentage thresholds, or report formatting changes.
- No BricsCAD V25/V26 native/runtime changes and no release/build work.

## Implemented

- `QuantityRevisionReport.Build(...)` now returns `rows.AsReadOnly()` rather than exposing its mutable backing `List<QuantityRevisionRow>` through `IReadOnlyList<QuantityRevisionRow>`.
- `QuantityRevisionReport.Summarize(...)` now returns `result.AsReadOnly()` for the same structural guarantee.
- Focused smoke coverage builds a real quantity revision snapshot, preserves deterministic `VolumeM3` content (`2 -> 3`), verifies neither result is a mutable `List<T>`, and verifies mutation through `IList<T>.Add(...)` is rejected.
- A dedicated module-initializer registration invokes the smoke without taking a shared registration-file dependency.

## Validation

- Source publication: `7c8963f03259a197da9b9fa41f6cec32fe095f47`.
- Focused smoke publication: `8f0b933ac03a737ec6dbc24d9b69aec7fd7677bd`.
- Dedicated smoke registration publication: `bf5c60c47590cbd5b28c57458e6875538373b5cf`.
- Ancestry check from `bf5c60c47590cbd5b28c57458e6875538373b5cf` to observed current `main` `4721cc060f242edc67e4d2ec14cb2981ce8e6f60` reports `ahead_by=212`, `behind_by=0`, with the registration commit as merge base, so the full publication chain remains reachable from `main`.
- Static/exact-diff/ancestry validation only. No GitHub Actions were dispatched and no repository `dotnet` or licensed BricsCAD V25/V26 runtime PASS is claimed from this hosted session.

## Coordination

Recent readonly-result claims reserve Recognition rule terms, wall-footprint/room-boundary geometry results, FeatureFlags snapshots, semantic sheet/view catalogs, revision-compare results, and other unrelated surfaces. This lane stayed limited to `QuantityRevisionReport` result collection structure.

## Completion condition

Satisfied: both quantity-revision result APIs expose structurally read-only collections with focused regression coverage integrated on current `main`, and this claim is `COMPLETED` with exact publication and ancestry evidence.
