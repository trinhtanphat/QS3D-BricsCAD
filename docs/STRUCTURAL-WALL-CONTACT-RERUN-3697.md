# #3697 StructuralWall penetration contact source-fix handoff

Status: `SOURCE_FIX / PENDING_EXACT_SHA_CI_AND_LOCAL_RERUN`

Lane-Key: `issue-3681-penetration-overdeduction`

Canonical source branch: `agent/chatgpt-gpt56sol/issue-3697-wall-contact-interface-fix`

Parent licensed qualification: #3681 / #72.

## Licensed failure being corrected

Licensed BricsCAD V25.2.10 on exact source `cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb` reproduced the same result twice for the sanitized one-end 0.05 m penetration control:

- gross vertical/formwork area: `2.6688 m2`;
- observed residual/net: `2.4288 m2`;
- observed deduction: `0.2400 m2`;
- expected residual/net: `2.5088 m2`;
- expected deduction: `0.1600 m2`.

The native overlap itself is real (`0.008 m3`). The error is area attribution: subtracting the clipped overlap creates the intended `0.1600 m2` end-face loss plus two `0.05 m x 0.8 m = 0.0400 m2` side-strip losses. Treating every lost original vertical boundary as concrete-contact area therefore over-deducts by `0.0800 m2`.

## Source correction

`StructuralWallConcreteContactService` now separates two responsibilities:

1. native `Solid3d` residual booleans continue to union-resolve overlapping cutters;
2. concrete-contact area is accumulated only from intersection/probe faces that lie on an original target vertical-face plane **and** whose original candidate has native BREP vertices on the exterior half-space of that target face.

For an exterior target boundary, its interior half-space is derived from the target Solid3d BREP vertices. Candidate sidedness is also derived from BREP vertices. Bounding boxes remain broad-phase only.

Consequences:

- the true penetrating end face is eligible because the candidate crosses from exterior through that target plane;
- the two coplanar side strips are rejected because the candidate lies only on the plane plus the target-interior half-space;
- a true touching-only neighbor remains eligible because its body lies on the exterior side and the existing native offset probe creates the bounded contact intersection;
- ambiguous/missing BREP sidedness fails the measurement closed instead of publishing guessed contact;
- V25 `ExternalBoundedSurface` planar unwrapping remains unchanged;
- stale generated-BREP and semantic-capture refresh behavior remain unchanged.

`ResidualVerticalAreaM2` in `QS3DWALLCONTACTPROBE` is now the logical contact residual `gross - authoritative contact area`, not the physical post-boolean vertical boundary area. This keeps the diagnostic aligned with the formwork quantity contract and avoids exposing #3697 side-strip topology as a false net-area deduction.

## Focused regression guard

`scripts/preflight-structural-wall-contact-penetration-side-strips.py` locks the sanitized control:

- eligible end contact = `0.1600 m2`;
- side strip A = `0.0400 m2`, rejected;
- side strip B = `0.0400 m2`, rejected;
- historical incorrect total = `0.2400 m2`;
- exterior-side authority must remain native BREP vertex topology, not `GeometricExtents`.

The existing residual/contact rule guards are also updated so `grossVerticalAreaCad - residualVerticalAreaCad` cannot become contact authority again.

## Required local rerun after protected CI

Local worker must fetch the exact pushed source SHA published after protected CI is green, build BricsCAD V25 `Release|x64`, NETLOAD that exact binary, then rerun #3681 without editing production source locally.

Required focused cells:

- one-end 0.05 m penetration: deduction `0.1600 m2`, residual/net `2.5088 m2`;
- BLT two-end control: gross `2.6688 m2`, deduction `0.3200 m2`, net `2.3488 m2`;
- full-face touching-only contact;
- partial contact;
- overlapping-neighbor union with no double subtraction;
- stale/missing/unresolvable BREP fail-closed;
- semantic capture refresh;
- save/cold-reopen and applicable Undo/Redo / second-DWG isolation.

Hosted/source CI is not `LOCAL_PASS`. Only sanitized licensed V25 evidence tied to the exact source/plugin/Core identity may close #3681.
