# Work claim — Material Usage primary-unit integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-usage-primary-unit-integrity-20260812-1433`
- Registered: `2026-08-12T14:33:00+07:00`
- Baseline main SHA: `b948a404ed8ea8168497c764d40fe3854a4df4c4`
- Priority: P2 deterministic reporting / fail-closed export integrity

## Confirmed defect

`ProjectMaterial` permits a non-empty custom unit token up to 24 characters, while `MaterialUsageRow.PrimaryQuantity` recognized only normalized `m`, `m2`, `m3`, and `kg`. Any other non-empty unit returned `0d`.

`MaterialUsageXlsxExporter` writes `PrimaryQuantity` directly into the visible `KL chính` column next to the row's `UnitHint`. A custom catalog material with an unsupported unit could therefore produce a workbook row whose unit was non-empty but whose primary quantity was silently reported as zero even when the row had non-zero measured metrics or element count.

## Reserved scope

- `src/QS3D.Core/Reporting/MaterialUsageSchedule.cs`
- `tests/QS3D.Core.SmokeTests/MaterialUsagePrimaryUnitIntegritySmoke.cs`
- this claim file

## Completed fix

`MaterialUsageRow.PrimaryQuantity` preserves existing mappings for `m`, `m²`/`m2`, `m³`/`m3`, and `kg`, and preserves the existing `0d` behavior when `UnitHint` is empty. Any other non-empty normalized unit now fails closed instead of fabricating a zero primary quantity. No new free-form unit semantics are inferred.

## Integration evidence

- Claim commit: `1ae8c0f7eede6b3bda58aa1f85f1d14c4e5ba885`
- Source fix: `860f99ecbf81126426321d005ad174c84f2398a2`
- Focused smoke: `e8093b6e363330b3cc5f92de6c1c0fc5830e5d64`
- Source diff re-read: implementation commit changes only the final unsupported-unit branch of `PrimaryQuantity`.
- Smoke re-read: supported units select their existing metric, empty unit remains zero, unsupported unit throws, and XLSX export refuses the unsupported row before publication.

## Validation boundary

Static/source verification only in this hosted session. GitHub Actions were not dispatched or rerun. No local `dotnet`/Core smoke execution and no BricsCAD V25/V26 runtime PASS are claimed.

## Excluded scope

- no material catalog schema or allowed-unit expansion;
- no native BricsCAD table/runtime changes;
- no changes to measured metric selection/grouping;
- no GitHub Actions or licensed runtime qualification.
