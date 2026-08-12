# Work claim — Material catalog persisted resource bounds

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-catalog-resource-bounds-20260812-0835`
- Registered: `2026-08-12T08:35:00+07:00`
- Baseline main SHA: `3702743fa1b0067d8a6955492d071aa8f72ebd0e`
- Priority: evidence-driven persisted-input resource bounds during owner-requested `continue all`

## Confirmed defect

`ProjectMaterialCatalog.ReadCustom(ProjectState)` supports at most 500 custom materials but currently performs unbounded `raw.Split('\n')` before checking the record count. Each record similarly performs unbounded `Split('|')` before requiring exactly four fields. Malformed delimiter-dense persisted metadata can therefore allocate arrays far beyond the supported catalog contract before failing.

The writer can never produce metadata anywhere near 1 MiB under the existing 500-record and decoded field-length caps, so an explicit serialized safety limit can reject impossible persisted state before tokenization without reducing valid catalog capacity.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs` — `ReadCustom(...)` persisted-resource preflight/tokenization only.
- `tests/QS3D.Core.SmokeTests/MaterialCatalogResourceBoundsSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

- Reject stored material metadata above 1 MiB before any split/decode allocation.
- Materialize at most `MaxCustomMaterials + 1` line tokens, then preserve the existing >500 fail-closed contract.
- Materialize at most 5 field tokens per record, then preserve the existing exactly-four-fields contract.
- Preserve canonical Base64, strict UTF-8, field trimming/lengths, empty-record rejection, built-in shadowing, duplicate id/name checks, custom-material capacity, idempotent upsert, reference propagation and mutation atomicity.

## Coordination

The material Base64 canonicality claim is `COMPLETED`; this lane does not change its decode/re-encode identity contract or existing focused smoke. No UI/native lifecycle, material usage schedule, Family/Element reference semantics, persistence store or release/update files are reserved.

## Validation plan

Prove ordinary canonical metadata still reads, a 501-record payload fails with the existing custom-material limit, a five-field record fails with the existing invalid-record contract, and >1 MiB stored metadata fails before parsing. Re-fetch source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
