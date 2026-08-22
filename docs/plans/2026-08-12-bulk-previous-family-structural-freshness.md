# Plan — Bulk previous Family structural freshness

## Goal

Prevent `BulkEditService.AssignFamily(...)` from migrating inherited defaults against a structurally replaced/removed previous Family when caller-controlled lazy target-ID enumeration changes `project.Families` without advancing `ProjectState.ChangeVersion`.

## Implementation

1. Snapshot the project Family collection as a unique case-insensitive ID -> exact `ProjectFamily` instance map before enumerating `elementIds`.
2. Preserve the existing target-ID semantic `ChangeVersion` freshness check.
3. Recheck the full Family ownership snapshot immediately after target enumeration and before target-Family/selected-element freshness checks and before any previous-Family property snapshot is read.
4. Reject count, null, duplicate-ID, removal, and same-ID replacement drift with `InvalidOperationException`.
5. Preserve current all-or-nothing assignment behavior and public APIs.

## Regression

Add a focused Core smoke that proves:

- stable lazy assignment still migrates inherited previous-Family defaults to the target Family;
- same-ID replacement of a previous Family during lazy enumeration fails closed while `ChangeVersion` remains unchanged;
- removal of a previous Family during a mutating empty lazy target sequence fails closed before the existing empty/no-op path can return.

Register the smoke with a standalone `ModuleInitializer` and add a static preflight locking snapshot/enumeration/version/full-Family-ownership ordering.

## Validation boundary

Connector-only work will read back committed source/test/preflight from `main`. It will not claim execution of the .NET smoke executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime unless those are actually run.
