# Work claim — Quantity revision readonly result snapshots

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:05:00+07:00`
- Baseline main SHA: `bd5a2bd242ddc924fd68c84867492e96d0e96ccd`
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

## Validation plan

- Prove `Build(...)` returns a structurally read-only collection that cannot be cast back to mutable `List<QuantityRevisionRow>` or mutated through `IList<T>`.
- Prove `Summarize(...)` has the same structural immutability.
- Preserve deterministic row/summary content, ordering, finite-number behavior, and existing null-row validation.
- Re-fetch current source before each product write, review exact pushed diffs, and verify the close commit remains reachable from current `main`.
- No GitHub Actions dispatch; no .NET/BricsCAD runtime PASS claim from this remote lane.

## Coordination

Recent readonly-result claims reserve Recognition rule terms, wall-footprint/room-boundary geometry results, FeatureFlags snapshots, and other unrelated surfaces. Recent revision work does not reserve `QuantityRevisionReport.cs`; this claim is intentionally limited to returned collection structure.

## Completion condition

Both quantity-revision result APIs expose structurally read-only collections with focused regression coverage integrated on current `main`, and this claim is closed with exact pushed SHAs/evidence.
