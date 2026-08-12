# Work claim — Material Usage primary-unit integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-usage-primary-unit-integrity-20260812-1433`
- Registered: `2026-08-12T14:33:00+07:00`
- Baseline main SHA: `b948a404ed8ea8168497c764d40fe3854a4df4c4`
- Priority: P2 deterministic reporting / fail-closed export integrity

## Confirmed defect

`ProjectMaterial` permits a non-empty custom unit token up to 24 characters, while `MaterialUsageRow.PrimaryQuantity` recognizes only normalized `m`, `m2`, `m3`, and `kg`. Any other non-empty unit currently returns `0d`.

`MaterialUsageXlsxExporter` writes `PrimaryQuantity` directly into the visible `KL chính` column next to the row's `UnitHint`. A custom catalog material with an unsupported unit can therefore produce a workbook row whose unit is non-empty but whose primary quantity is silently reported as zero even when the row has non-zero measured metrics or element count.

## Reserved scope

- `src/QS3D.Core/Reporting/MaterialUsageSchedule.cs`
- one focused Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Expected fix

Preserve existing mappings for `m`, `m²`/`m2`, `m³`/`m3`, and `kg`, and preserve the existing `0d` behavior when `UnitHint` is empty. For any other non-empty normalized unit token, fail closed instead of fabricating a zero primary quantity. Do not infer new unit semantics such as pieces/litres/count from free-form catalog text.

## Regression plan

- supported length/area/volume/mass unit spellings keep selecting the corresponding metric;
- empty `UnitHint` keeps returning zero;
- a non-empty unsupported custom unit throws instead of returning zero;
- a material-usage XLSX preflight cannot silently publish an unsupported-unit row with `KL chính = 0`.

## Excluded scope

- no material catalog schema or allowed-unit expansion;
- no native BricsCAD table/runtime changes;
- no changes to measured metric selection/grouping;
- no GitHub Actions or licensed runtime qualification.

## Completion condition

Source and focused smoke are integrated to `main`, current source/test are re-read, and this claim is marked `COMPLETED` with exact integration SHA(s).
