# Work claim — Family property removal freshness parity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-family-property-removal-freshness`
- Registered: `2026-08-12T08:17:00+07:00`
- Last Updated: `2026-08-12T08:17:00+07:00`
- Baseline main SHA: `43ce80deabf48be8f6bba1dd7c6115b08459d6eb`
- Priority: deterministic Core freshness-policy mismatch found during owner-requested `continue all`
- Task Key: `CORE-FAMILY-PROPERTY-REMOVAL-FRESHNESS-PARITY`

## Confirmed defect

`ProjectFamilyService.SetProperty(...)` propagates inherited values through `ProjectElement.SetProperty(...)`, which applies `ElementGeometryPolicy` to distinguish ordinary semantic properties, geometry-affecting properties and generated-output-only properties.

`ProjectFamilyService.RemoveProperty(...)` bypasses that policy when an inherited instance value is removed: it directly removes the dictionary key and unconditionally calls `element.MarkDirty(Properties | Quantity | Geometry)`. Because public `MarkDirty(...)` also treats Properties/Geometry as generated-output stale triggers, removing an ordinary inherited property such as `Scale` unnecessarily dirties Geometry and can mark existing generated output stale. The same removal path cannot express the existing `Material` policy correctly either: generated output should stale without requiring Geometry dirty.

## Reserved scope

Add an internal `ProjectElement.RemoveProperty(...)` primitive symmetric with `SetProperty(...)`: remove only when the property is present, compute the same `AffectsGeneratedGeometry` / `AffectsGeneratedOutput` policy, mark Properties/Quantity plus conditional Geometry, and invoke the selective internal dirty/stale path. Route inherited Family property removal through that primitive.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- one focused CAD-independent Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- Family assignment, activation, create/rename, persistence schemas, BulkEdit, Selection, WPF/native BricsCAD adapters.
- changes to `ElementGeometryPolicy` property lists or generated-state definitions.
- public API expansion: the new remove primitive should remain internal to Core unless existing source constraints require otherwise.
- GitHub Actions, build/release dispatch, V25/V26 runtime qualification.

## Validation plan

- Removing inherited ordinary `Scale` removes the Family + instance property, advances project revision once, marks Properties/Quantity but not Geometry, and does not stale an existing generated solid.
- Removing inherited geometry property `WidthM` marks Geometry and stales an existing generated solid.
- Removing inherited generated-output-only property `Material` leaves Geometry clean while still marking existing generated solid stale.
- Explicit instance overrides remain preserved by the existing Family removal semantics.
- Re-fetch moving `main` and both target blobs after claim publication and immediately before integration; review exact PR diff before merge.

## Coordination

Recent exact file history shows no new `ProjectElement.cs` commit since the prior completed freshness/stale hardening on 2026-08-11 and no new `ProjectFamilyService.cs` commit since its completed null-target/canonical assignment lanes. Current open PR #640 is XLSX-only and does not overlap these files. No discovered active claim owns Family property removal freshness at registration time.

## Completion condition

Current `main` applies the same geometry/generated-output freshness policy to inherited Family property removal as to property setting, with focused regression source and this claim marked `COMPLETED` with exact integration evidence.
