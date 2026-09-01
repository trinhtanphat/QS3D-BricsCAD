# Room Finish export status aggregation precision

## Scope

`QS3DFINISHXLSX` publishes the authoritative Room Finish workbook and reports a user-visible total of `PrimaryQuantity` across the emitted schedule rows. The status total is presentation arithmetic only: it must not change workbook rows, grouping, count, provenance, regeneration, or export ordering.

## Precision contract

Cross-row status aggregation uses `QuantityReportMath.FiniteAccumulator`. Each `PrimaryQuantity` is first validated with `QuantityReportMath.NonNegative`, then accumulated independently from the checked integer Count total. The accumulator is finalized once after row traversal.

This preserves a representable final total when small positive rows temporarily fall below pairwise binary64 resolution. The control sequence `1e16`, `1`, `1` must be allowed to reach the representable final value `10000000000000002`; the adapter must not restore strict pairwise `QuantityReportMath.Add` for this cross-row status fold.

Non-finite, negative, and overflowing status inputs remain fail-closed. `QuantityReportMath.AddCount` remains authoritative for checked element-count aggregation.

## Product boundaries

- `RoomFinishXlsxExporter` and the workbook schema/content are unchanged.
- Detached regeneration and the existing-project read-only boundary are unchanged.
- The save dialog still precedes project/export work, preserving the established command interaction.
- Post-export palette/editor reporting remains best effort only.
- V26 compiles the shared V25 adapter source, so this contract has one implementation across V25/V26; do not add a duplicate V26 command implementation.

## Deterministic validation

Run:

```text
python scripts/preflight-room-finish-export-status-aggregation-precision.py
```

The guard requires compensated accumulation, explicit non-negative validation, checked Count aggregation, rejection of the former pairwise status fold, and V26 shared-source inclusion. Aggregate repository preflight and protected PR `preflight` + `core` remain required before merge.

No licensed BricsCAD runtime evidence is required for this bounded arithmetic correction; hosted/static/build evidence is authoritative for the source contract and must not be represented as `LOCAL_PASS`.