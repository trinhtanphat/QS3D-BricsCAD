# Work claim — Material catalog persisted resource bounds

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-catalog-resource-bounds-20260812-0835`
- Registered: `2026-08-12T08:35:00+07:00`
- Completed: `2026-08-12T08:36:00+07:00`
- Baseline main SHA: `3702743fa1b0067d8a6955492d071aa8f72ebd0e`
- Claim commit: `9b6c343a5920e3e02eda59c4c43591aa85f92dac`
- Source commit: `f46ff076cc4771c1fe80cd4ed0ef1882076e6770`
- Regression commit: `556a0d553587925c4fc9dbaa047db021ccbdd1d4`
- Priority: evidence-driven persisted-input resource bounds during owner-requested `continue all`

## Confirmed defect fixed

`ProjectMaterialCatalog.ReadCustom(ProjectState)` supports at most 500 custom materials but previously performed unbounded `raw.Split('\n')` before checking the record count. Each record similarly performed unbounded `Split('|')` before requiring exactly four fields. Malformed delimiter-dense persisted metadata could therefore allocate arrays far beyond the supported catalog contract before failing.

## Completed change

- Stored material metadata above 1 MiB is rejected before split/decode allocation. This remains above every payload the existing writer can produce under 500-record and decoded field-length caps.
- Line tokenization is bounded to `MaxCustomMaterials + 1`, after which the existing >500 custom-material error remains authoritative.
- Field tokenization is bounded to 5, after which the existing exactly-four-fields error remains authoritative.
- Canonical Base64, strict UTF-8, field trimming/lengths, empty-record rejection, built-in shadowing, duplicate id/name checks, custom-material capacity, idempotent upsert, reference propagation and mutation atomicity are unchanged.

## Regression evidence

`MaterialCatalogResourceBoundsSmoke` proves ordinary canonical metadata still reads, a 501-record payload fails with the custom-material limit, a five-field record fails with the invalid-record contract, and >1 MiB stored metadata fails with the serialized safety limit.

## Read-back validation

Current `main` source was re-fetched after source/regression publication and contains the 1 MiB preflight plus bounded 501-line / 5-field Split overloads. The focused smoke was also re-fetched from `main` with all four boundary cases intact.

## Coordination respected

The completed material Base64 canonicality behavior and its regression remain unchanged. No UI/native lifecycle, material usage schedule, Family/Element reference semantics, persistence store or release/update files were changed.

## Validation boundary

Remote source/smoke read-back only. No GitHub Actions were dispatched; no executable Core build/smoke PASS and no BricsCAD V25/V26 runtime qualification are claimed.
