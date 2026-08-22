# Grid naming reserved-label integrity

## Problem

`GridNamingHealthService` treats case-insensitive duplicate semantic Grid labels as an Error, but `GridNamingService.Renumber(...)` previously collapsed duplicate labels owned entirely by non-target Grids into a `HashSet` and continued. Renumbering an unrelated target could therefore succeed while leaving an ambiguity that the same Core model-health contract considers invalid.

## Boundary

This fix is intentionally limited to duplicate labels that cannot be repaired by the requested batch: every owner of the duplicate is outside `orderedGridElementIds`. A pre-existing duplicate involving a target Grid remains repairable because that target's label will be replaced by the requested renumber operation.

## Implementation

- Normalize each non-empty non-target Grid label with the existing trim behavior.
- Insert it into the existing case-insensitive reserved-label set.
- If insertion returns `false`, fail closed before plan creation, `ProjectState.Touch()`, or target mutation.
- Preserve all existing batch-size, element-id, category, sequence, affix, collision and no-op contracts.

## Regression

`GridNamingReservedLabelIntegritySmoke` covers:

1. two non-target Grids carrying `"  KEEP  "` and `"keep"` reject an unrelated target renumber with target state and `ChangeVersion` unchanged;
2. a duplicate old label shared by one target and one non-target remains repairable when the target is renumbered to a new label.

The smoke is registered via a dedicated module initializer so this lane does not edit the shared smoke registration hotspot. `scripts/preflight-grid-naming-reserved-label-integrity.py` pins the fail-closed source token and both regression scenarios.

## Validation boundary

This is CAD-independent Core source logic. Source/static verification and committed smoke coverage are appropriate remotely. Do not claim BricsCAD V25 runtime PASS or a full build unless those are actually executed in a capable environment.
