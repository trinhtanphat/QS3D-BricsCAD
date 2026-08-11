# Work claim — bulge arc midpoint overflow integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulge-midpoint-overflow-20260811-2340`
- Registered: `2026-08-11T23:40:00+07:00`
- Baseline main SHA: `4d4b6e96cc6dbdcd266d8c385b8a1b60cd643958`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Harden `BulgeArcTessellator` midpoint arithmetic so a valid arc with large same-sign finite coordinates and a finite chord is not rejected solely because `(start + end)` overflows before division by two.

## Expected surfaces

- `src/QS3D.Core/Geometry/BulgeArcTessellator.cs`
- `tests/QS3D.Core.SmokeTests/BulgeArcMidpointOverflowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/BulgeArcMidpointOverflowSmokeRegistration.cs`
- this claim file for close-out

## Concrete defect

The tessellator validates endpoints and chord as finite, then computes the midpoint as `(start.X + end.X) * 0.5` / `(start.Y + end.Y) * 0.5`. Two same-sign finite coordinates can have a finite delta/chord while their intermediate sum overflows to infinity. A geometrically representable arc can therefore fail later at center validation even though the required midpoint itself is finite.

## Explicit exclusions

- No native BricsCAD geometry/materialization changes.
- No tessellation tolerance, segment-count, bulge-angle or ownership policy changes.
- No curtain/rebar/interchange/UI/updater/licensing/Actions/release/LOCAL_PASS work.

## Validation plan

- A large same-sign finite semicircle whose coordinate sum overflows but whose chord/center/arc remain finite tessellates successfully with finite output points and exact endpoints.
- Existing ordinary arc behavior remains unchanged.
- Non-finite endpoints remain rejected by existing validation.
- Re-fetch/compare `main`, publish through a feature branch/PR without force-push, then re-read remote `main`.

## Completion condition

Midpoint arithmetic no longer creates avoidable infinity for representable arcs, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact integration SHA and actual validation performed.
