# Work claim — Family property removal freshness parity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-family-property-removal-freshness`
- Registered: `2026-08-12T08:17:00+07:00`
- Last Updated: `2026-08-12T08:23:00+07:00`
- Baseline main SHA: `43ce80deabf48be8f6bba1dd7c6115b08459d6eb`
- Priority: deterministic Core freshness-policy mismatch found during owner-requested `continue all`
- Task Key: `CORE-FAMILY-PROPERTY-REMOVAL-FRESHNESS-PARITY`
- Implementation PR: `#645`
- Main integration commit: `b930f8488ae6bbc752b1aad31506e78690201aef`

## Confirmed defect

`ProjectFamilyService.SetProperty(...)` propagates inherited values through `ProjectElement.SetProperty(...)`, which applies `ElementGeometryPolicy` to distinguish ordinary semantic properties, geometry-affecting properties and generated-output-only properties.

`ProjectFamilyService.RemoveProperty(...)` bypassed that policy when an inherited instance value was removed: it directly removed the dictionary key and unconditionally called `element.MarkDirty(Properties | Quantity | Geometry)`. Removing an ordinary inherited property such as `Scale` therefore dirtied Geometry and could mark existing generated output stale, while generated-output-only properties such as `Material` could not express their existing selective policy correctly.

## Implemented scope

Added internal `ProjectElement.RemoveProperty(...)`, symmetric with `SetProperty(...)`:

- validates/canonicalizes the property name consistently;
- returns a no-op when the key is absent;
- removes the present key;
- computes the same `AffectsGeneratedGeometry(...)` and `AffectsGeneratedOutput(...)` policy;
- marks Properties/Quantity plus conditional Geometry;
- uses the private selective `MarkDirtyCore(...)` stale-output boundary.

`ProjectFamilyService.RemoveProperty(...)` now routes inherited instance-property removal through this primitive instead of direct dictionary mutation + unconditional Geometry dirty.

## Regression source

Added `tests/QS3D.Core.SmokeTests/FamilyPropertyRemovalFreshnessSmoke.cs` with module-initializer coverage for:

- ordinary inherited `Scale`: property removed, project revision +1, only Properties/Quantity dirty, generated solid remains fresh;
- geometry inherited `WidthM`: Geometry/Properties/Quantity dirty and generated solid stale;
- generated-output-only inherited `Material`: Properties/Quantity dirty, Geometry remains clean, generated solid stale;
- explicit instance override: Family default removed while override, element dirty state/timestamp and generated-output state remain unchanged.

## Coordination / exclusions preserved

- Family assignment, activation, create/rename, persistence schemas, BulkEdit, Selection, WPF/native BricsCAD adapters were not changed.
- `ElementGeometryPolicy` property lists and generated-state definitions were not changed.
- The new removal primitive is internal; no public Core API expansion was introduced.
- No force-push, GitHub Actions/build/release dispatch, or V25/V26 runtime qualification was performed.

## Validation evidence

- Claim registration was committed to `main` before source edits at `733ed8ee3e11c6123a125a93d0077bb47036a7ad`.
- Exact path history showed no current competing edits on `ProjectElement.cs` or `ProjectFamilyService.cs`; moving-main readback immediately before PR creation still had original blobs `5f95ebf18d6b343b6d478627f7346b768bd2576c` and `fe05aa288a0ca45be04073dba1a6fcd59c3c0829`.
- Internal remove primitive commit: `4808516e3b6be125f4c31e47ac6be866b7968ff6`.
- Family service routing commit: `c97d6d66101559e23533144944da597765c0476d`.
- Focused smoke/head commit: `4be2d3ea95b793a48bac331ef2fd8ef322de84f5`.
- PR #645 exact diff was reviewed before merge and contained exactly three files, `+144/-3`; production changes were the 13-line internal remove primitive and replacement of direct Family removal mutation with that primitive.
- Server-side squash merge with exact expected head `4be2d3ea95b793a48bac331ef2fd8ef322de84f5` produced `b930f8488ae6bbc752b1aad31506e78690201aef`.
- Post-merge readback confirms `ProjectElement.cs` blob `f12c7a541774dddd3b0c2b506a63b921bff1a182` and `ProjectFamilyService.cs` blob `b2f901503d26bdc5c29e3efd38a98dec9b85edb1` contain the intended policy-aware removal path.
- The smoke executable/build was not run in this connector-only environment; no local .NET or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion

`COMPLETED`: current `main` applies the same geometry/generated-output freshness policy to inherited Family property removal as to property setting while preserving explicit overrides and existing project revision semantics.