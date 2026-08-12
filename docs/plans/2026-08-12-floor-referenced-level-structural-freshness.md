# Plan — Floor referenced-level structural freshness

## Goal

Prevent `AssignBottomLevel(...)` / `AssignTopLevel(...)` from validating vertical placement against a structurally replaced or removed opposite `FloorDefinition` when caller-controlled lazy target enumeration changes `project.Floors` without advancing `ProjectState.ChangeVersion`.

## Implementation

1. Snapshot the unique project Floor collection as ID -> exact `FloorDefinition` instance before enumerating mutation targets in the two vertical-level assignment APIs.
2. Preserve the existing target enumeration `ChangeVersion` check and selected-element ownership checks supplied by `ResolveOwnedElements(...)`.
3. Preserve the existing target-Floor exact ownership check.
4. Before opposite-level elevation validation, inspect each selected element's current opposite level ID. If it references a Floor that existed in the pre-enumeration snapshot, require the current project lookup to resolve to that exact snapshot instance. If the referenced Floor was removed/replaced, fail closed before `project.Touch()` or property/dirty mutation.
5. Leave unrelated Floors and same-instance Floor value changes outside this structural identity guard.

## Regression

Add focused Core smoke coverage proving:

- stable lazy `AssignBottomLevel` still succeeds with a valid existing top level;
- same-ID replacement of the existing top level during lazy target enumeration fails closed with unchanged `ChangeVersion` and no bottom-level mutation;
- same-ID replacement of the existing bottom level during lazy `AssignTopLevel` enumeration fails closed;
- removal of an opposite level fails closed before vertical assignment mutation.

Register the smoke through `ModuleInitializer` and add a static preflight locking snapshot/enumeration/existing freshness/opposite-level freshness/elevation-validation ordering.

## Validation boundary

Connector-only work will read back committed source/test/preflight from `main`. It will not claim execution of the .NET smoke executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime unless actually run.
