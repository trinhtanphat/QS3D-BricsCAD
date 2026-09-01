# Curtain XLSX status aggregation precision

## Scope

This runbook covers only the V25 `QS3DCURTAINXLSX` command's user-facing status totals in `CurtainWallScheduleCommands.cs`. It does not change Curtain schedule grouping/calculation, `CurtainWallXlsxExporter`, workbook schema, native geometry, release workflows, or licensed-runtime acceptance.

## Defect

On protected `main@76b1f7e3ca1990bacac088b8e7c502f739571844`, the command totals `NetGlassAreaM2` and `FrameLengthM` with repeated `QuantityReportMath.Add(...)` before calling the XLSX exporter. `QuantityReportMath.Add` intentionally rejects a non-zero addend that is swallowed by one binary64 addition. Consequently a valid sequence such as `1e16`, `1`, `1` can abort the command at the first small addend even though the correctly rounded final total `10000000000000002` is representable.

Because the status computation runs before `CurtainWallXlsxExporter.Export(...)`, this precision artifact can prevent an otherwise valid workbook from being written.

## Required contract

- Keep checked `PanelCount` accumulation through `QuantityReportMath.AddCount`.
- Keep separate compensated states for glass-area and frame-length status totals.
- Admit only finite, non-negative values.
- Allow intermediate rounding loss when compensation can recover a representable final total.
- Fail closed when final non-zero compensation is materially unrepresentable, when a prior accumulated value would be lost, or when finite arithmetic overflows.
- Finalize both status totals before XLSX publication and format those finalized values in the existing status string.
- Do not alter workbook/export semantics merely to compute the status line.

## TDD / validation

1. `scripts/preflight-curtain-export-status-aggregation-precision.py` must be RED against the pairwise protected-main implementation.
2. Implement the smallest adapter-local compensated status-total correction.
3. The focused guard must turn GREEN without weakening its pairwise-regression or final-compensation assertions.
4. Automatic exact-head branch CI must pass reservation, generic/source/feature guards, Core validation and trusted V25 compilation selected by the repository classifier.
5. Reconcile current protected main if it advances, obtain fresh exact-head evidence, then require protected PR `preflight` + `core` SUCCESS before expected-head merge.

## Runtime classification

`NOT_APPLICABLE` for this deterministic source/compile contract. Hosted/source evidence is not a licensed BricsCAD `LOCAL_PASS` claim.
