# Work claim — Wall junction bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-junction-bounded-enumeration-20260812-0004`
- Registered: `2026-08-12T00:04:00+07:00`
- Baseline main SHA: `51622e193c45827bf4b5b56ac738697907c8d7f6`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `WallJunctionPlanner.Plan` enforce its existing `MaxSegments = 10000` contract while enumerating `IEnumerable<WallAxisSegment>`, rather than after unrestricted materialization.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallJunctionPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`Plan` currently calls `source.ToList()` before checking `raw.Count > MaxSegments`. A huge or non-terminating enumerable can therefore be consumed without limit before the declared 10,000-segment guard; candidate indexing, sweep/intersection discovery and classification all occur only afterward.

## Explicit exclusions

- No wall junction numeric math, candidate indexing, sweep ordering, tolerance, L/T/X/Multi classification, read-only command lifecycle, ownership/materialization, native V25, UI, Actions, release, or LOCAL_PASS semantics changes.

## Validation plan

- Preserve all existing junction behavior.
- Add a non-terminating valid wall-segment enumerable that throws if item 10,002 is requested; verify oversize input rejects after exactly 10,001 yielded segments, before duplicate-ID or geometry validation.
- Re-fetch current source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

The 10,000-segment safety limit bounds enumeration/allocation as well as accepted cardinality, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
