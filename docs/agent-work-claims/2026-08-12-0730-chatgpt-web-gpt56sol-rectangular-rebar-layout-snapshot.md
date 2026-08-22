# Work claim — Rectangular rebar layout snapshot

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:30:00+07:00`
- Completed: `2026-08-12T07:33:00+07:00`
- Baseline main SHA: `a70ccd6b966fbbf18816d152f18cb0092586005b`
- Priority: evidence-driven remote-safe rebar result integrity

## Reason

`RectangularRebarLayout` was a public planning result that stored the caller-supplied `IReadOnlyList<Point2>` reference directly. A mutable `List<Point2>` therefore remained aliased into the result: mutating or clearing the source list after construction changed `BarCenters` even though a planner result is expected to be a stable snapshot. Neighboring rebar result types such as `BeamLongitudinalRebarLayout` already materialize owned lists.

## Reserved scope

Materialize an owned read-only snapshot of `BarCenters` in `RectangularRebarLayout`. Preserve planner geometry, bar ordering/count, clear envelope values, public API types and native integration behavior. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs` (`RectangularRebarLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/RectangularRebarLayoutSnapshotSmoke.cs`
- this claim file

## Excluded scope

- No changes to bar spacing/count/cover/diameter formulas or planner limits.
- No validation-policy expansion beyond breaking the mutable collection alias.
- No CAD/native/UI behavior changes and no BricsCAD runtime claim.
- No GitHub Actions dispatch.

## Completion

- Claim commit: `7f94e8471576c9f13ff54e69608ad3c0efa0ce51`.
- Implementation commit: `2ba7c2e96be48a11afac6ccd193fb8aecaafa04e` — copy caller centers into an owned `List<Point2>` and expose it read-only.
- Regression commit: `2ba1a77dbdc9c41e008a75314e7592f45ea1c701` — mutate/clear the source center list after construction and verify the layout remains stable; also preserve the normal 2x2 corner layout and clear half-extents.
- Validation actually performed:
  - re-fetched current `RectangularRebarLayout` and confirmed the constructor materializes an owned read-only center list;
  - re-fetched the dedicated smoke and confirmed alias mutation plus expected 2x2 corner ordering are covered;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

No current/recent claim was found for `RectangularRebarLayout` collection aliasing; the last direct planner commit was the original Core feature implementation.

## Completion condition

Satisfied: current `main` returns stable rectangular rebar layout centers independent of caller collection mutation, focused regression coverage is present, and this claim is released as `COMPLETED`.
