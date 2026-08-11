# Work claim — bulge midpoint overflow safety

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulge-midpoint-overflow-20260811-2339`
- Registered: `2026-08-11T23:39:00+07:00`
- Baseline main SHA: `bfce67a17e2c2fc87adcd9e6fb3e059147e8e905`
- Priority: evidence-driven Core numeric stability during owner-requested `continue all`

## Reserved scope

Harden `BulgeArcTessellator` midpoint construction so valid finite same-sign endpoint coordinates do not overflow solely because the midpoint is computed as `(start + end) * 0.5`.

## Expected surfaces

- `src/QS3D.Core/Geometry/BulgeArcTessellator.cs`
- `tests/QS3D.Core.SmokeTests/RoomBoundaryRegressionSmoke.cs`
- this claim file for close-out

## Concrete defect

`Point2.DistanceTo` already supports large finite same-sign coordinates with a finite local delta, but `BulgeArcTessellator` forms its midpoint by adding the two absolute coordinates before halving. Two finite endpoints near the positive or negative double limit can therefore have a finite chord and valid arc geometry while the midpoint addition overflows to infinity, causing `arcCenter` validation to reject an otherwise representable tessellation.

## Explicit exclusions

- No change to bulge angle, sagitta, segment-count, winding/direction, room-boundary, rebar, native BricsCAD, UI, updater/licensing, interchange, Actions, release, or LOCAL_PASS behavior.
- No weakening of existing finite/radius/center/tessellation safety guards.

## Validation plan

- Preserve all existing bulge/room-boundary regression scenarios.
- Add focused large finite same-sign endpoint coverage whose chord, radius, center and tessellated points remain finite but whose naive endpoint sum would overflow.
- Re-fetch current source/test blobs immediately before each write and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched; no local BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Representable large-coordinate bulge arcs no longer fail from midpoint intermediate overflow, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation actually performed.
