# #3687 StructuralWall live-BREP contact source-fix handoff

Status: `SOURCE_FIX / PENDING_EXACT_SHA_LOCAL_RERUN`

Lane-Key: `issue-3687`

Canonical source branch: `agent/chatgpt-gpt56sol/issue-3687-structwall-brep-contact-fix`

Parent licensed qualification: #3681 / #3679 / #72.

## Failure being corrected

Licensed BricsCAD V25.2.10 on the prior exact source `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31` proved that a real native StructuralWall/Column overlap exists (`0.008 m3` for the 0.05 m penetration control), while production `ConcreteContactAreaM2` remained zero.

The old service used the original wall for intersection classification, then attempted to subtract the complete neighbor Solid3d from a residual clone. A neighbor that extends outside the wall can therefore have a valid positive intersection while the wider subtraction fails. That failure was previously swallowed, leaving the residual unchanged and publishing a false zero deduction. The old transient volume check also relied only on `Brep.GetVolume()`, while QS3D's authoritative Solid3d capture already uses `Solid3d.MassProperties.Volume`.

## Source correction

`StructuralWallConcreteContactService` now:

- prefers `Solid3d.MassProperties.Volume` for transient native boolean volume, with BREP volume only as fallback;
- intersects every candidate against the **current residual**, not the original target;
- subtracts only the clipped positive intersection/contact solid from that residual;
- therefore union-resolves overlapping neighbors naturally as the residual shrinks;
- fails the measurement closed when a candidate reaches native contact processing but intersect/subtract/offset fails, rather than converting native failure to `0 m2` contact;
- exposes bounded diagnostics: target solids, candidate solids, original vertical face seeds, positive-volume cuts, contact-probe cuts, failed native cuts, gross vertical area, residual vertical area and final deduction.

`QS3DWALLCONTACTPROBE` is a read-only helper for the exact-SHA licensed rerun. It resolves exactly one selected semantic StructuralWall and prints only sanitized aggregate diagnostic counts/areas; it does not create a project, write semantic/CAD state, or print IDs/Handles.

## Local rerun

Local worker must fetch/checkout the exact pushed source SHA published by the #3687 PR after protected CI is green, build V25 `Release|x64`, then rerun the existing #3681 matrix. Do not edit production source on the local lane.

For the known one-end 0.05 m penetration control:

- gross vertical/formwork area: `2.6688 m2`;
- expected concrete contact deduction: `0.1600 m2`;
- expected net formwork: `2.5088 m2`;
- probe should show at least one candidate, four vertical face seeds for the rectangular wall control, at least one successful positive-volume cut, zero failed native cuts, and residual vertical area `2.5088 m2` within the established quantity tolerance.

For the two-end BLT control:

- gross formwork: `2.6688 m2`;
- contact deduction: `0.3200 m2`;
- net formwork: `2.3488 m2`.

Also rerun partial contact, overlapping-neighbor union, stale/missing BREP clearing, capture refresh, save/cold-reopen and applicable Undo/Redo / second-DWG isolation from #3681.

A hosted/source CI pass is not `LOCAL_PASS`. Runtime result remains one of sanitized `PASS`, `FAIL`, or `NO_RESULT` tied to the exact plugin/Core/source identity.
