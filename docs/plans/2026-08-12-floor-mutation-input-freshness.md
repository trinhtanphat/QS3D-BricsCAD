# Floor mutation target input freshness

## Problem

`ProjectFloorService.Assign(...)`, `AssignBottomLevel(...)`, `AssignTopLevel(...)`, and `ClearVerticalLevels(...)` all rely on `ResolveOwnedElements(ProjectState, IEnumerable<ProjectElement>)`. The helper snapshots project-owned elements and then enumerates caller-controlled targets without a project freshness boundary. Enumeration can execute arbitrary caller code and call `project.Touch()` while yielding otherwise-owned elements; the caller then continues validation/no-op calculation and mutation against a newer project state.

## Invariant

- Keep project-element snapshotting and ownership validation unchanged.
- Capture `project.ChangeVersion` immediately before enumerating caller-controlled `elements`.
- Preserve null target rejection, instance ownership checks, duplicate collapse, and deterministic ID ordering.
- Immediately after enumeration completes, fail closed with `InvalidOperationException` when `ChangeVersion` differs.
- Because the guard lives in the shared helper, all four Floor mutation APIs must reject stale target materialization before their own validation/no-op/mutation phases.
- Stable target inputs retain existing behavior.

## Regression

Add deterministic Core smoke coverage demonstrating:

1. stable lazy targets still work through Floor assignment;
2. a lazy target that touches the project and yields an owned element fails before `FloorId` mutation;
3. a lazy target that touches the project and yields no elements fails before the zero-change no-op;
4. a vertical-level caller also inherits the shared freshness guard, proving the helper protects more than plain `Assign`.

Register the smoke using the repository's ModuleInitializer convention.

## Static preflight

Lock helper ordering as:

`ChangeVersion capture` → `foreach (var element in elements)` → `freshness comparison` → ordered read-only return

and require source calls from `Assign`, `AssignBottomLevel`, `AssignTopLevel`, and `ClearVerticalLevels`, plus smoke and registration presence.

## Exclusions

No Floor/Level reference canonicality, elevation math, create/update/delete/activate semantics, UI audit changes, persistence, Actions/build/release dispatch, or licensed BricsCAD runtime qualification.
