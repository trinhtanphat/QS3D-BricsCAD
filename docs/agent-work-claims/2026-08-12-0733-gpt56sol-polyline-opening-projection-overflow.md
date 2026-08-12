# Work claim — Polyline opening projection overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-polyline-opening-projection-overflow-20260812-0733`
- Registered: `2026-08-12T07:33:00+07:00`
- Baseline main SHA: `dfb5c06dafa0fa79d0817560bfab5587ebd2f988`
- Priority: evidence-driven Core opening geometry hardening during owner-requested `continue all`

## Reserved scope

Make `PolylineOpeningCutPlanner` clamp finite out-of-segment opening projections before reconstructing an unbounded raw dot product, without changing cut dimensions, corner/junction policy, host selection, or native authoring.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolylineOpeningCutPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

Each centerline segment currently evaluates `fromStartX * ux + fromStartY * uy` directly. For a finite opening center far beyond a long diagonal segment, both products can be finite while their sum overflows, even though the correct closest point is simply the finite segment endpoint. Numeric failure occurs before the existing physical guard can reject a cutter centered at/through a polyline endpoint.

## Explicit exclusions

- No `OpeningCutPlanner` formulas, cutter clearance, maximum-offset policy, polyline corner/junction rule, host matching, native BricsCAD cut/materialization, UI, Actions, release, or LOCAL_PASS changes.

## Validation plan

- Scale the opening-to-segment vector before the unit-direction dot product and compare the scaled projection to scaled segment length; clamp start/end before reconstructing the bounded interior `along` value.
- Preserve ordinary interior projection and deterministic segment tie breaking.
- Add focused public `Plan()` smoke coverage with a finite long diagonal host and a finite opening center beyond its endpoint where the old dot sum overflows; require the existing host-length/corner policy exception rather than numeric projection overflow.
- Re-fetch target source after claim before implementation and never overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Polyline opening planning no longer fails solely because an out-of-segment projection scalar exceeds the numeric range when the finite endpoint is the correct closest point, regression is committed on current `main`, and this claim is marked `COMPLETED`.
