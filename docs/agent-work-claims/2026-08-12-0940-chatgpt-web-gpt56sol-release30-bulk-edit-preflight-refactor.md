# Work claim — release #30 Bulk Edit preflight refactor reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-bulk-edit-preflight-refactor`
- Registered: `2026-08-12T09:40:00+07:00`
- Baseline main SHA: `8144ac7e23930351a12d116ec4f878dd639487ce`
- Priority: QS3D Cloud V25 Preview Build & Release #30 has Bulk Edit failures from static gates that still pin direct property dictionary/DirtyFlags mutation and an obsolete helper boundary after property mutation was encapsulated in `ProjectElement.SetProperty(...)`.

## Reserved scope

Reconcile only `scripts/preflight-bulk-edit-canonicalization.py` and `scripts/preflight-bulk-edit-null-target.py` with the current `BulkEditService` implementation. Preserve Core production source and existing smoke coverage unchanged.

## Canonical evidence

- Both SetProperty and MultiplyNumericProperty still canonicalize through `SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName)` and read through the canonical `key`.
- Pending updates now apply through `update.Element.SetProperty(key, update.Value)`; `ProjectElement.SetProperty` owns canonical property/dirty behavior, replacing direct `Properties[key] = ...` plus `DirtyFlags(...)` calls.
- `OwnedDistinct(ProjectState, IEnumerable<ProjectElement>)` still exists and fail-closes null project entries, blank/duplicate project IDs, null caller targets, blank target IDs and foreign object instances.
- The null-target gate fails only because it tries to end the helper slice at the removed `private static ElementDirtyFlags DirtyFlags` method.
- Existing `BulkEditCanonicalizationSmoke` still covers canonical keys, Geometry dirty propagation, corrupt project atomicity and null object targets.

## Expected surfaces

- `scripts/preflight-bulk-edit-canonicalization.py`
- `scripts/preflight-bulk-edit-null-target.py`
- this claim file for close-out

## Excluded scope

- No edits to `BulkEditService.cs`, `ProjectElement`, mutation executor, smoke tests, Family assignment or property policy.
- No behavior changes to dirty flags, canonical keys, target ownership, input bounds or project revision semantics.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Canonicalization gate: retain canonical-key/read checks and mutation-executor checks; require `update.Element.SetProperty(key, update.Value);` instead of removed direct dictionary/DirtyFlags calls.
- Null-target gate: isolate `OwnedDistinct` using the next current helper `MaterializeBounded` and retain every null/blank/foreign validation assertion.
- Preserve smoke registration and null-target atomicity smoke assertions.
- Re-fetch both exact gate blobs before writes, read back after commits, verify ancestry and close with exact SHAs.

## Coordination

Repository search found no active reservation for these Bulk Edit preflight scripts or the current BulkEditService refactor contract.

## Completion condition

Both Bulk Edit gates follow the encapsulated property mutation/current helper layout without weakening canonicalization, geometry dirty or target-validation protections, are pushed to `main`, and this claim is closed with exact evidence.
