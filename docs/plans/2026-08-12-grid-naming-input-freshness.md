# Grid naming input freshness

## Problem

`GridNamingService.Renumber(ProjectState, IEnumerable<string>, GridNamingOptions?)` accepts caller-controlled lazy input. The method currently enumerates `orderedGridElementIds` without establishing a project freshness boundary. Enumeration can execute arbitrary caller code and can mutate/touch the same `ProjectState`; after enumeration the method still validates/resolves targets, plans labels, calls `project.Touch()`, and updates Grid metadata against a state newer than the one on which target materialization started.

## Invariant

- Capture `project.ChangeVersion` immediately before the first enumeration of `orderedGridElementIds`.
- Keep the existing bounded enumeration and ID normalization unchanged.
- Immediately after enumeration completes, compare the current `ChangeVersion` with the captured value.
- If the project changed, throw `InvalidOperationException` before empty/duplicate/options validation, project resolution, label planning, `project.Touch()`, or element mutation.
- Stable lazy sequences retain existing behavior.

## Regression

Add deterministic Core smoke coverage for:

1. stable lazy Grid IDs still renumber normally;
2. a lazy input that calls `project.Touch()` and yields a valid Grid ID fails closed before Grid label/sequence mutation;
3. a lazy input that calls `project.Touch()` and then yields no IDs fails with the freshness error before the normal empty-input error, locking the guard immediately after materialization.

Register the smoke without modifying the shared runner if the repository's ModuleInitializer convention is available.

## Static preflight

Add a focused script that locks source ordering as:

`ChangeVersion capture` → `foreach orderedGridElementIds` → `freshness comparison` → `ids.Count == 0`

and verifies the deterministic smoke plus registration remain present.

## Exclusions

No Grid Annotation health/owner work, BricsCAD command lifecycle/native annotation changes, naming-format redesign, persistence changes, GitHub Actions dispatch, or remote BricsCAD runtime qualification.
