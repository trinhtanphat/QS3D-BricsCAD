# Zone assignment input freshness

## Problem

`ProjectZoneService.Assign(ProjectState, string, IEnumerable<ProjectElement>)` accepts caller-controlled lazy targets. After resolving the selected Zone and current project elements, the method enumerates `elements`, deduplicates owned targets, then calculates changed targets and mutates `ZoneId`. No `ChangeVersion` check surrounds the caller enumeration, so a lazy sequence can mutate/touch the same `ProjectState` while yielding an otherwise-valid owned element and assignment will continue on stale assumptions.

## Invariant

- Preserve existing Zone lookup and project-element ownership resolution.
- Capture `project.ChangeVersion` immediately before enumerating the caller-supplied `elements` sequence.
- Keep existing null/ownership validation and duplicate collapse unchanged.
- Immediately after enumeration completes, fail closed with `InvalidOperationException` if `ChangeVersion` changed.
- Perform that freshness rejection before changed-target calculation, zero-change no-op, `project.Touch()`, or `ZoneId`/dirty mutation.
- Stable target collections retain current behavior.

## Regression

Add deterministic Core smoke coverage for:

1. stable lazy owned target assigns the Zone normally;
2. lazy target calls `project.Touch()` and yields an owned element, then fails before `ZoneId`/dirty mutation;
3. lazy target calls `project.Touch()` and yields no elements, then fails with the freshness error before the normal zero-change no-op.

Register the smoke via the repository's ModuleInitializer convention.

## Static preflight

Lock source ordering as:

`ChangeVersion capture` → `foreach (var element in elements)` → `freshness comparison` → `changed = ...`

and require the focused smoke plus registration.

## Exclusions

No `ProjectFloorService`, vertical-level, Zone UI audit, active-zone, create/update/delete, persistence, Actions, build/release, or BricsCAD runtime changes.
