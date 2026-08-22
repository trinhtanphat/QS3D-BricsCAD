# Work claim — Opening host projection overflow

- Status: `COMPLETED`
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

`ClosestPointOnSegment` normalized the host direction but still evaluated `qx * ux + qy * uy` directly. For a finite opening point far beyond a long diagonal segment, both terms can be finite while their sum overflows. The mathematically correct projection is simply beyond the segment end, so the closest point is the finite endpoint; throwing on the unneeded unbounded scalar rejected representable host matching.

## Implementation

- `ce8f8a7a02517cb944e4abe559bb65bd2748e129` — scale the opening offset, compare scaled projection against scaled segment length, and clamp start/end before reconstructing an interior along-distance.
- `e5028138be2e5ba2deb7f58426e71869564dc805` — add focused smoke coverage for a finite long diagonal host with an opening beyond its endpoint where the old raw dot sum overflowed but endpoint distance/gap remain finite.

## Validation performed

- Re-fetched target source after claim registration and confirmed the raw unit-direction dot sum remained before editing.
- Re-fetched committed source and confirmed endpoint decisions are now made in scaled projection space before interior along-distance reconstruction.
- Re-fetched the smoke fixture and confirmed it asserts matched host identity, exact endpoint closest-point, finite centerline distance and finite accepted gap.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No host ranking/gap/ambiguity policy, source enumeration cap, host identity, Auto Host lifecycle, Opening Property/native V25, cut/materialization, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Opening host matching no longer fails solely because an out-of-segment projection scalar would exceed the numeric range when the finite endpoint is the correct closest point, focused regression is integrated on `main`, and this claim is closed.
