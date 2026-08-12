# Work claim — Rectangular rebar layout snapshot

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:30:00+07:00`
- Baseline main SHA: `a70ccd6b966fbbf18816d152f18cb0092586005b`
- Priority: evidence-driven remote-safe rebar result integrity

## Reason

`RectangularRebarLayout` is a public planning result but stores the caller-supplied `IReadOnlyList<Point2>` reference directly. A mutable `List<Point2>` therefore remains aliased into the result: mutating or clearing the source list after construction changes `BarCenters` even though a planner result is expected to be a stable snapshot. Neighboring rebar result types such as `BeamLongitudinalRebarLayout` already materialize owned lists.

## Reserved scope

Materialize an owned read-only snapshot of `BarCenters` in `RectangularRebarLayout`. Preserve planner geometry, bar ordering/count, clear envelope values, public API types and native integration behavior. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs` (`RectangularRebarLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/RectangularRebarLayoutSnapshotSmoke.cs`
- this claim file

## Excluded scope

- No changes to bar spacing/count/cover/diameter formulas or planner limits.
- No validation-policy expansion beyond breaking the mutable collection alias.
- No CAD/native/UI behavior changes and no BricsCAD runtime claim.
- No GitHub Actions dispatch.

## Validation plan

- Construct a layout from a mutable `List<Point2>`, mutate/clear the source list, and assert the layout preserves its original centers/order.
- Assert a normal 2x2 rectangular planner case still returns the four expected corner centers and clear half-extents.
- Re-fetch current source blob before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

No current/recent claim was found for `RectangularRebarLayout` collection aliasing; the last direct planner commit is the original Core feature implementation.

## Completion condition

Current `main` returns stable rectangular rebar layout centers independent of caller collection mutation, focused regression coverage is present, and this claim is marked `COMPLETED`.
