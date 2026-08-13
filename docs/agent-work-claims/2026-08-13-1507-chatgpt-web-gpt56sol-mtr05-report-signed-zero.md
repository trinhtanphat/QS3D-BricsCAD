# Work claim — MTR-05R Quantity Report signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr05-report-signed-zero-20260813-1507`
- Registered: `2026-08-13T15:07:00+07:00`
- Baseline main SHA: `50bfa41c56f446b12b870815f7c73bacf15ef544`
- Priority: `MTR-05 / P0 continuous hardening` — canonical report quantities must not expose IEEE negative zero through public report rows

## Confirmed defect

`QuantityReportMath.NonNegative(...)` rejects NaN, Infinity and finite negative values, but returns exact zero unchanged. IEEE negative zero therefore survives this shared report-normalization boundary because `-0.0 < 0.0` is false.

`ProjectQuantityReportBuilder` applies that helper when normalizing public `QuantityReportRow` metrics, including `MassKg`. A row carrying explicit `MassKg = -0.0` can therefore be returned with a negative-zero sign bit even though the value is semantically a non-negative zero. Existing reporting and unit canonicality work already treats signed-zero representation splits as observable identity/reporting defects.

## Reserved scope

Canonicalize exact-zero results returned by `QuantityReportMath.NonNegative(...)` to positive `0d` after the existing invalid-value guard, and add a focused public-builder regression that distinguishes the sign bit.

The change preserves:

- existing NaN/Infinity rejection;
- existing finite-negative rejection;
- ordinary positive report quantities;
- report grouping, provenance, selection and material/density semantics;
- all MeasurementTrace payload/fingerprint behavior.

## Expected surfaces

- `src/QS3D.Core/Reporting/QuantityReportMath.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs`
- this claim file

## Excluded scope

- No `UnitScale`, `ProjectUnitPolicy`, MeasurementTrace/Snapshot/Delta/Inspector, rate/cost, persistence, exporter or UI changes.
- No Auto Room reporting-fixture changes.
- No native BricsCAD V25/V26 adapter or LOCAL qualification changes.
- No GitHub Actions and no BricsCAD native PASS claim.

## Validation plan

- Regression through public `ProjectQuantityReportBuilder` output, not a helper-only assertion.
- Assert numeric zero and `BitConverter.DoubleToInt64Bits(value) == 0L` so signed zero cannot pass by ordinary equality.
- Retain current invalid-value rejection behavior and ordinary report semantics.
- Re-fetch current `main` before source commit, inspect intervening commits for overlap, then verify remote source/test after push.

## Completion condition

The claim is complete only when the narrow source fix and focused regression are on current `main`, intervening commits are reconciled without overlap, remote source/test are read back, executable/native validation is reported truthfully, and this claim is closed `COMPLETED`.
