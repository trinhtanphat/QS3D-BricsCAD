# Agent Work Claim — Semantic Schedule Placement Left/Top Margin

- Status: `COMPLETED`
- Owner: ChatGPT web / GPT-5.6 Sol
- Started: 2026-08-12 09:35 +07:00
- Completed: 2026-08-12 09:38 +07:00
- Start commit observed: `4e311927d63c01b0e77227de79073823fafad979`
- Claim commit: `73d6902543991a4f0c20c12390f413c3925bf2f6`
- Fix commit: `e9900d8317b5cbd4f011feaeeeaa801d2623714d`
- Regression commit: `a400a8bb8baa02fe1541fab90d5776ebf8da4c14`
- Related roadmap/issue: Documentation layer / semantic schedule placement

## Purpose

Prevent semantic schedule placement candidates derived from existing view edges from escaping the configured left/top schedule margins.

## Allowed scope

- `src/QS3D.Core/Documentation/SemanticSchedulePlacementPlanner.cs`
- focused `tests/QS3D.Core.SmokeTests/SemanticSchedulePlacementSmoke.cs` regression coverage
- this claim file

## Excluded scope

- semantic schedule persistence/schema changes
- native BricsCAD Layout/PaperSpace/Table mutation
- sheet/view planner behavior outside schedule candidate placement
- quantity/reporting engines
- UI/ribbon/updater/licensing
- BricsCAD runtime qualification

## Proven defect

`FindPlacement(...)` seeded candidate X/Y coordinates with the configured left/top margins, but also appended `occupied` region right/bottom edges plus the configured gap. An existing view may validly sit inside the schedule margin. If that view is sufficiently small, its derived edge candidate can be numerically less than `MarginLeftMm` or `MarginTopMm`. Because the candidate loop checked only right/bottom limits, the planner could return a schedule placement inside the configured left/top margin.

The prior `ExistingViewOutsideScheduleMarginRemainsValid` smoke used a 30 mm view at (2,2) with an 8 mm gap and 20 mm margins, deriving 40 mm candidates; it did not exercise the sub-margin candidate branch.

## Implemented contract

- Generated Y candidates below `MarginTopMm` are skipped before fit/overlap evaluation.
- Generated X candidates below `MarginLeftMm` are skipped before fit/overlap evaluation.
- Existing deterministic candidate ordering and overlap/gap behavior remain unchanged for candidates inside the usable region.
- The focused smoke now uses a 2 mm x 2 mm existing view at (2,2). With the 8 mm default gaps it derives 12 mm edge candidates against 20 mm left/top margins, which the old implementation would select. The regression now requires the returned placement to be exactly (20,20).

## Validation

- Re-read the claim, target planner and smoke from live `main` after claim registration before implementation.
- Source fix commit `e9900d8317b5cbd4f011feaeeeaa801d2623714d` is an ancestor of regression commit `a400a8bb8baa02fe1541fab90d5776ebf8da4c14`; compare reported `behind_by: 0`.
- The compare from fix to regression/current head showed no later modification to `SemanticSchedulePlacementPlanner.cs`; the only target-scope change after the fix was the focused smoke update.
- No GitHub Actions were manually dispatched.
- No local .NET or BricsCAD runtime PASS is claimed in this remote-source lane.

## Overlap note

Recent history was checked immediately before registration. Existing semantic schedule placement claims were completed; no newer claim for left/top margin candidate filtering was found. Concurrent commits observed during implementation touched unrelated Selection, Reporting, Services, Grid and claim/preflight lanes.
