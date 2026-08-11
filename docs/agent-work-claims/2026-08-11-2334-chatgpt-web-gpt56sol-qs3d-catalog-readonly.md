# Work claim — QS3D static catalog read-only integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:34:00+07:00`
- Baseline main SHA: `e302ecb1e5bcbb14d598d895a51e5e077fb396db`
- Priority: evidence-driven remote-safe Core global-state hardening

## Reason

`Qs3dCatalog` exposes its three static catalogs as `IReadOnlyList<string>`, but each backing value is a runtime `string[]`. A caller can downcast the public property to `IList<string>`/`string[]` and mutate an entry in place, corrupting the process-global catalog for every later caller despite the read-only API surface.

## Reserved scope

Wrap all three static catalog backing collections in non-mutable read-only collections so public callers cannot alter global catalog contents through a downcast. Preserve item text, order, counts and the existing `IReadOnlyList<string>` API. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Domain/Qs3dCatalog.cs`
- `tests/QS3D.Core.SmokeTests/Qs3dCatalogReadOnlySmoke.cs`
- this claim file

## Excluded scope

- No changes to catalog labels/order, menu/UI construction, material catalog, semantic categories, Direct Draw, or BricsCAD V25 runtime.
- No feature additions or localization changes.
- No GitHub Actions dispatch.

## Validation plan

- Cast each exposed catalog to `IList<string>` and assert indexed mutation throws `NotSupportedException`.
- Assert original first item and all catalog counts remain unchanged after the attempted mutations.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

No current or recent claim was found for `Qs3dCatalog` or static catalog collection ownership. Material Catalog claims concern a separate persisted project catalog.

## Completion condition

Current `main` prevents mutation of the process-global QS3D static catalog through public read-only list properties, includes focused regression coverage, and this claim is marked `COMPLETED`.
