# Issue #3672 — StructuralWall formwork Rule 1/2 regression matrix

Status: ACTIVE  
Lane-Key: issue-3672  
Owner/session: interactive-20260824-wf7c2a  
Canonical branch: `agent/interactive-20260824-wf7c2a/issue-3672-wall-formwork-rule-matrix`  
Baseline main: `ddac682ea2cad82e35ca9bded2f53d89599fa9a8`

## Scope

This follow-up locks the already-merged StructuralWall formwork behavior from #3660/#3662 and #3665/#3666. It adds regression coverage for free ends, partial/full/bounded contact deductions, door-at-floor versus sill/window reveal formwork, opening plus concrete contact audit ordering, native BREP residual/union invariants, supported concrete-neighbor categories, and V26 shared-source inclusion.

BLT3D remains a clean-room parity reference only. No proprietary BLT source, binary or license asset is part of this lane.

## Validation

- exact branch shared CI: pending;
- protected PR `preflight` / `core`: pending;
- licensed BricsCAD runtime: not claimed by this remote-safe regression lane.
