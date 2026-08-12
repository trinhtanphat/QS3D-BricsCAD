# Work claim — Polyline opening projection overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-polyline-opening-projection-overflow-20260812-0733`
- Registered: `2026-08-12T07:33:00+07:00`
- Baseline main SHA: `dfb5c06dafa0fa79d0817560bfab5587ebd2f988`
- Priority: evidence-driven Core opening geometry hardening during owner-requested `continue all`

## Reserved scope

Make `PolylineOpeningCutPlanner` clamp finite out-of-segment opening projections before reconstructing an unbounded raw dot product, without changing cut dimensions, corner/junction policy, host selection, or native authoring.

## Concrete defect

Each centerline segment evaluated `fromStartX * ux + fromStartY * uy` directly. For a finite opening center far beyond a long diagonal segment, both products can be finite while their sum overflows, even though the correct closest point is simply the finite segment endpoint. Numeric failure occurred before the existing physical guard could reject a cutter centered at/through a polyline endpoint.

## Implementation

- `29a242982c0c746082a9ebc9d0528a23e76e0dcf` — scales the opening-to-segment vector before the unit-direction dot product, clamps start/end in the scaled domain, and reconstructs an absolute `along` value only for a bounded interior projection. Endpoint clamps reuse the exact finite source endpoint.
- `2480dc798af64c3acc37136d20c1d74c8ed2a104` — adds public `Plan()` smoke coverage with a finite long diagonal host and finite beyond-end opening center whose old raw dot sum overflows; regression requires the existing polyline corner/junction policy rejection rather than numeric projection failure.

## Validation

- Re-read `PolylineOpeningCutPlanner.cs` from current `main`; source blob `e8d538bdd54ad25287e81cd92821c0c87b1dfaae` contains scaled projection and endpoint clamping.
- Re-read `PolylineOpeningProjectionOverflowSmoke.cs` from current `main`; test blob `d7e6d1ef8cbc0b627296a298147ced0e636d59ca` contains the focused public regression.
- Independent arithmetic for the fixture gives finite segment length about `1.131e308`, old raw projection `+Infinity`, and finite endpoint offset about `7.071e307`, so the numeric defect is isolated from the existing maximum-offset guard.
- No GitHub Actions were dispatched.
- No local .NET compile/test runner or BricsCAD V25/V26 runtime PASS is claimed from this web session.

## Explicit exclusions

- No `OpeningCutPlanner` formulas, cutter clearance, maximum-offset policy, polyline corner/junction rule, host matching, native BricsCAD cut/materialization, UI, Actions, release, or LOCAL_PASS changes.

## Completion

Polyline opening planning no longer fails solely because an out-of-segment projection scalar exceeds the numeric range when the finite endpoint is the correct closest point; the existing physical policy remains authoritative and this source-only claim is complete.
