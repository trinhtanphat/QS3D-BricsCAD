# QS3D Grid auto-number — BricsCAD V25

Updated: 2026-08-10 (UTC+7)

## Command

`QS3DGRIDNUMBERAUTO`

This command adds a guarded spatial-ordering interaction on top of the existing semantic Grid workflow. It does not replace:

- `QS3DGRID` for source capture;
- `QS3DGRIDNUMBER` for explicit click-order naming;
- `GridNamingService` for label validation and mutation.

Use manual `QS3DGRIDNUMBER` whenever the intended order is not a simple reviewed parallel-line family.

## Reviewed workflow

1. select already-captured Grid source entities;
2. every selected source must resolve to exactly one semantic `ElementCategory.Grid`;
3. every selected Grid must own exactly one authoritative source Handle;
4. every source must be a native `LINE`;
5. all LINE sources must be horizontal to one common WCS plan elevation; a 3D-sloped or different-elevation family fails closed;
6. pick two points defining the **explicit ordering axis** from start to end;
7. prompt points are converted **UCS → WCS** before comparison with native LINE coordinates;
8. Core `GridSpatialOrderingPlanner.OrderParallelLines` verifies the family and computes order;
9. choose Numeric/Alphabetic label options;
10. review the planned first/last Grid and explicitly confirm;
11. only then does `GridNamingService.Renumber` mutate semantic labels.

Confirmation is fail-closed: type explicit `Yes` to apply; Enter/default means No and exits without semantic mutation.

The direction of the explicit ordering axis controls the increasing label direction. Reversing the picked axis reverses the projected order; the command does not silently infer a preferred left/right or bottom/top convention.

## Fail-closed spatial policy

Automatic ordering is intentionally limited to **parallel LINE** references. Core rejects a line that is not perpendicular to the explicit ordering axis within the reviewed alignment tolerance. It also rejects duplicate/overlapping projected coordinates instead of choosing a tie by handle, id, creation time or current selection order.

**ARC/radial** Grid ordering is not inferred by this command. Radial systems need a separate policy covering center ownership, angle normalization, sweep direction and wrap-around at 0/2π.

The V25 layer also requires all selected LINE references to share the same plan elevation. The current Core planner is a 2D XY ordering planner, so projecting Grid lines from different planes into one order would overstate what has been reviewed.

## Coordinate-system contract

BricsCAD point input follows the current UCS, while semantic Grid LINE geometry is read from native database entities. The command therefore transforms both ordering-axis prompt points by `Editor.CurrentUserCoordinateSystem` before constructing the Core `Point2` axis. This keeps the spatial plan in WCS even when the operator works in a rotated/transformed UCS.

The command ignores the Z component of the ordering axis only after the axis endpoints are in WCS. Its XY direction must remain finite and non-zero.

## Atomic semantic boundary

All CAD work is read-only. The command does not create, erase or modify LINE entities.

Before semantic mutation it captures `ProjectStateSnapshot`. If `GridNamingService.Renumber` or the audit operation fails, the project state is restored. A failed restore is surfaced as an aggregate error rather than silently accepting a partial rename.

PICKFIRST restoration, selection sync and palette refresh happen after successful semantic mutation and are best-effort UI operations.

## Relationship to native annotation

After auto-number succeeds, existing Grid annotation may intentionally become stale until it is rebuilt. `GeneratedGridAnnotationHealthService` and the V25 live annotation checker surface that drift. Run `QS3DGRIDANNOTATE` or `QS3DGRIDANNOTATEALL` to replace owned native bubble/text output after reviewing the new labels.

Auto-number itself does not rebuild annotation. This keeps semantic numbering rollback independent from CAD generated-output replacement.

## Limits

- maximum selected Grid family: 2000;
- source geometry: captured native LINE only;
- one authoritative source Handle per Grid;
- one common plan elevation;
- label sequence, start index, numeric padding, prefix/suffix and duplicate-label validation remain owned by `GridNamingService`;
- spatial ambiguity remains owned by `GridSpatialOrderingPlanner`.

## Runtime qualification

Source implementation is `REMOTE_DONE`; BricsCAD runtime validation remains `LOCAL_ONLY` until an exact-SHA licensed V25 matrix executes.

Minimum V25 matrix:

1. horizontal parallel LINE family in WCS;
2. same family under rotated and translated UCS, confirming identical WCS order;
3. reverse ordering-axis direction and confirm reversed labels;
4. reject a non-parallel LINE without semantic mutation;
5. reject overlapping/projected-duplicate lines without semantic mutation;
6. reject a 3D-sloped LINE and mixed plan elevations;
7. reject ARC/radial selection;
8. cancel at selection, first axis point, second axis point, naming prompts and final confirmation with zero semantic mutation;
9. duplicate-label collision against Grid outside the batch with full semantic rollback;
10. Numeric/Alphabetic, prefix/suffix and padding boundaries;
11. save/reopen `.qsdb`, then rebuild native annotation and run `QS3DHEALTH`;
12. multi-DWG isolation plus Unicode/HiDPI UI review.

Do not describe `QS3DGRIDNUMBERAUTO` as BricsCAD V25 runtime-certified until that matrix has actually executed.
