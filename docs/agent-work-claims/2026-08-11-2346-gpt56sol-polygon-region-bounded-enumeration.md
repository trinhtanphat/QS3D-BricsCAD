# Work claim — polygon region bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-polygon-region-bounded-enumeration-20260811-2346`
- Registered: `2026-08-11T23:46:00+07:00`
- Baseline main SHA: `65472488a08f60ea0662cd13e224e7d0f7d9547c`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `PolygonRegionSetTopology.NormalizeAndValidate` enforce its existing `MaxRegions = 256` contract during enumeration rather than materializing an unbounded source before checking the cap.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolygonRegionSetTopology.cs`
- an isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`NormalizeAndValidate(IEnumerable<PolygonRegionSeed2>)` currently calls `regions.ToList()` and checks `materialized.Count > MaxRegions` only afterward. A huge or non-terminating enumerable can therefore be consumed without limit despite the API's explicit 256-island safety bound.

## Explicit exclusions

- No region-id, outer/hole, overlap/touch/nesting, scanline, total-vertex, native BricsCAD, UI, updater/licensing, interchange, Actions, release, or LOCAL_PASS semantics changes.

## Validation plan

- Preserve existing multi-region topology behavior.
- Add a non-terminating enumerable probe that throws if read past item `MaxRegions + 1`; verify oversize input is rejected after exactly 257 yielded regions.
- Re-fetch the current source blob immediately before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

The 256-island contract bounds enumeration/allocation as well as accepted cardinality, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
