# Work claim — Closed two-vertex polyline length

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:20:00+07:00`
- Baseline main SHA: `059c488d8eaf473bab5e6d30444640a84b5775b9`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`PolylineMetrics.Length(points, closed)` added the closing segment only when `points.Count > 2`. For exactly two distinct vertices, `closed=true` therefore returned only the forward A→B segment even though BricsCAD closed-polyline semantics add a segment from the last vertex back to the first. The Core metric undercounted such a closed polyline by one segment.

## Reserved scope

Make `PolylineMetrics.Length` include the last→first closing segment whenever a polyline with at least two vertices is marked closed. Preserve open-polyline length, area semantics, finite/overflow guards, point distance behavior, and all other geometry planners. Add a dedicated CAD-independent regression smoke for open versus closed two-vertex length plus the existing multi-vertex closed behavior.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolylineMetrics.cs`
- `tests/QS3D.Core.SmokeTests/PolylineClosedLengthSmoke.cs`
- this claim file

## Excluded scope

- No changes to wall/curtain/opening/room polygon planners, bulges, CAD adapters, UI, or BricsCAD V25 runtime.
- No changes to `PolylineMetrics.SignedArea`/`Area`.
- No GitHub Actions dispatch.

## Validation plan

- Assert two points `(0,0)` and `(3,4)` have open length `5` and closed length `10`.
- Assert a three-vertex closed polyline still includes exactly one closing segment.
- Re-fetch current `main` and target blob before writes; never force-push.
- Hosted environment has no .NET SDK, so record static/source verification and do not claim an executed `dotnet` run.

## Coordination

No current claim found names `PolylineMetrics` or polyline metric length. Other active geometry claims target distinct planners. The change was confined to the generic Core metric and a dedicated module-initialized smoke.

## Completion

- Implementation commits:
  - `620d51b99ca6dc38663c1416319f57c8771c701c` — include the last→first segment for every closed polyline that reaches the existing `Count >= 2` metric path.
  - `6e976e085f5ccf5c4cba73a71be07a110bbcfabb` — add dedicated open/closed two-vertex and multi-vertex length regression coverage.
- Final observed `main` before claim close: `254e97aa0535d2a1cf85a1a979821f03d63d7f42`.
- Validation actually performed:
  - re-fetched `PolylineMetrics.cs` from current `main` and confirmed only the closing-segment condition changed;
  - re-fetched the new smoke and confirmed exact open `5`, closed `10`, open triangle-path `7`, and closed triangle `12` assertions plus module initialization are present;
  - cross-checked BricsCAD documentation that closing a polyline creates a segment from the last vertex to the first;
  - did not execute `dotnet` because the hosted environment does not provide the .NET SDK;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: no new native contract; the fix aligns the CAD-independent metric with documented closed-polyline semantics.

## Completion condition

Satisfied: current `main` computes the closing segment for two-vertex closed polylines, includes regression coverage, and this claim is released as `COMPLETED`.
