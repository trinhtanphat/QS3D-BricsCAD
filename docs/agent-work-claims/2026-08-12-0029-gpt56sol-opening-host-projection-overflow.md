# Work claim — Opening host projection overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-opening-host-projection-overflow-20260812-0029`
- Registered: `2026-08-12T00:29:00+07:00`
- Baseline main SHA: `441a4ba8ed0e9efcd8af0a49aaba94e1aeeeee46`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `OpeningHostMatcher.ClosestPointOnSegment` handle finite opening-to-host offsets whose raw projection dot product overflows even though endpoint clamping yields a finite valid closest point.

## Expected surfaces

- `src/QS3D.Core/Geometry/OpeningHostMatcher.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`ClosestPointOnSegment` normalizes the host direction but still evaluates `qx * ux + qy * uy` directly. For a finite opening point far beyond a long diagonal segment, both terms can be finite while their sum overflows. The mathematically correct projection is simply beyond the segment end, so the closest point is the finite endpoint; throwing on the unneeded unbounded scalar rejects representable host matching.

## Explicit exclusions

- No host ranking/gap/ambiguity policy, source enumeration cap, host identity, Auto Host lifecycle, Opening Property/native V25, cut/materialization, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Scale the opening offset before the unit-direction dot product and compare the scaled projection against the scaled segment length so endpoint clamping occurs before reconstructing an unbounded along-distance.
- Preserve ordinary interior/start/end projection behavior.
- Add focused smoke coverage with a finite diagonal host and finite opening point beyond its endpoint where the old raw dot sum overflows but endpoint distance/gap remain finite.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Opening host matching no longer fails solely because an out-of-segment projection scalar would exceed the numeric range when the finite endpoint is the correct closest point, regression is integrated on current `main`, and this claim is marked `COMPLETED`.
