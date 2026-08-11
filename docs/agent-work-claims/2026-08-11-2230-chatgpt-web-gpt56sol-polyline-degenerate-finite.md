# Work claim — degenerate polyline finite validation

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:30:00+07:00`
- Baseline main SHA: `658b4c3251fbd77cd31505957db46104c2b3b5ea`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect

`PolylineMetrics` rejects non-finite point coordinates on normal metric paths, but its degenerate early returns bypass those guards: `Length` returns `0` before touching coordinates when fewer than two points are supplied, and `SignedArea` returns `0` before validating coordinates when fewer than three points are supplied. A one-point length or one/two-point area containing `NaN`/`Infinity` is therefore accepted as the finite metric `0`, even though the same coordinates are rejected once enough vertices are present.

## Reserved scope

Make the degenerate metric paths preserve their existing numeric result (`0`) only when all supplied coordinates are finite. Do not change closed/open length semantics, area formula, point distance math, or polygon planners.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolylineMetrics.cs`
- `tests/QS3D.Core.SmokeTests/PolylineDegenerateFiniteSmoke.cs`
- `tests/QS3D.Core.SmokeTests/PolylineDegenerateFiniteRegistration.cs`
- this claim file

## Excluded scope

- No `Point2` changes.
- No wall, curtain, opening, room, bulge, tessellation, rebar, CAD adapter, UI, installer, reporting, persistence, licensing or BricsCAD V25 runtime changes.
- No change to the completed two-vertex closed-length contract.
- No GitHub Actions dispatch.

## Validation plan

- Prove empty finite sequences retain zero length/area.
- Prove one-point finite length and one/two-point finite signed area retain zero.
- Prove NaN/infinite coordinates fail closed even on early-return cardinalities.
- Use dedicated module-initializer registration rather than shared smoke registration.
- Re-fetch current target blob immediately before product write and re-read exact pushed diffs afterward.
- Hosted environment has no .NET SDK; do not claim executed `dotnet` tests or V25 runtime qualification.

## Coordination

The immediately preceding `PolylineMetrics` claim for closed two-vertex length is `COMPLETED`; this lane intentionally preserves that behavior and targets only validation skipped by degenerate early returns.

## Completion condition

Current `main` treats non-finite coordinates consistently across normal and degenerate polyline metric paths, focused regression source is registered, exact diffs are reviewed, and this claim is closed without overwriting concurrent work.