# Agent Work Claim — Semantic Schedule Placement Left/Top Margin

- Status: `ACTIVE`
- Owner: ChatGPT web / GPT-5.6 Sol
- Started: 2026-08-12 09:35 +07:00
- Start commit observed: `4e311927d63c01b0e77227de79073823fafad979`
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

`FindPlacement(...)` seeds candidate X/Y coordinates with the configured left/top margins, but also appends `occupied` region right/bottom edges plus the configured gap. An existing view may validly sit inside the schedule margin. If that view is sufficiently small, its derived edge candidate can be numerically less than `MarginLeftMm` or `MarginTopMm`. Because the current candidate loop checks only right/bottom limits, the planner can return a schedule placement inside the configured left/top margin.

The existing `ExistingViewOutsideScheduleMarginRemainsValid` smoke uses a 30 mm view at (2,2) with an 8 mm gap and 20 mm margins, deriving 40 mm candidates; it therefore does not exercise the sub-margin candidate branch.

## Contract

- Never consider a generated X candidate below `MarginLeftMm`.
- Never consider a generated Y candidate below `MarginTopMm`.
- Preserve existing deterministic candidate ordering and overlap/gap behavior for candidates inside the usable region.
- Tighten focused smoke coverage with an existing view whose edge-plus-gap remains inside the left/top margin and assert the returned schedule stays at or beyond both configured margins.

## Overlap note

Recent history was checked immediately before registration. Existing semantic schedule placement claims are completed; no newer claim for left/top margin candidate filtering was found. Re-read latest `main`, this claim, target source, and smoke before implementation to catch any race.
