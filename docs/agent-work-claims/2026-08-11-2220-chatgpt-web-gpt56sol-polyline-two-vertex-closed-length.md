# Work claim — Closed two-vertex polyline length

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:20:00+07:00`
- Baseline main SHA: `059c488d8eaf473bab5e6d30444640a84b5775b9`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`PolylineMetrics.Length(points, closed)` currently adds the closing segment only when `points.Count > 2`. For exactly two distinct vertices, `closed=true` therefore returns only the forward A→B segment even though BricsCAD closed-polyline semantics add a segment from the last vertex back to the first. The Core metric undercounts such a closed polyline by one segment.

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

No current claim found names `PolylineMetrics` or polyline metric length. Other active geometry claims target distinct planners. The change is intentionally confined to the generic Core metric and a dedicated module-initialized smoke.

## Completion condition

Current `main` computes the BricsCAD-consistent closing segment for two-vertex closed polylines, includes regression coverage, and this claim is marked `COMPLETED` with implementation SHA(s) and validation actually performed.
