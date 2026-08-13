# Work claim — MTR-05R Quantity Report signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr05-report-signed-zero-20260813-1507`
- Registered: `2026-08-13T15:07:00+07:00`
- Baseline main SHA: `50bfa41c56f446b12b870815f7c73bacf15ef544`
- Priority: `MTR-05 / P0 continuous hardening` — canonical report quantities must not expose IEEE negative zero through public report rows

## Confirmed defect

`QuantityReportMath.NonNegative(...)` rejected NaN, Infinity and finite negative values, but returned exact zero unchanged. IEEE negative zero therefore survived this shared report-normalization boundary because `-0.0 < 0.0` is false.

`ProjectQuantityReportBuilder` applies that helper when normalizing public `QuantityReportRow` metrics, including `MassKg`. A row carrying explicit `MassKg = -0.0` could therefore be returned with a negative-zero sign bit even though the value is semantically a non-negative zero. Existing reporting and unit canonicality work already treats signed-zero representation splits as observable identity/reporting defects.

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
- `tests/QS3D.Core.SmokeTests/QuantityReportSignedZeroSmoke.cs`
- this claim file

## Excluded scope

- No `UnitScale`, `ProjectUnitPolicy`, MeasurementTrace/Snapshot/Delta/Inspector, rate/cost, persistence, exporter or UI changes.
- No Auto Room reporting-fixture changes.
- No edits to the shared `ProjectQuantitySmoke.cs` or smoke registration list; the focused regression uses the repository's existing `ModuleInitializer` smoke pattern.
- No native BricsCAD V25/V26 adapter or LOCAL qualification changes.
- No GitHub Actions and no BricsCAD native PASS claim.

## Implementation

- Claim-only registration: `e024741e67cdf2b466e8f3b749a3745a484814d3`.
- Claim-only reservation refinement: `40a8ecdf414617d514670a5f286dfef65d2cc246` — narrowed the test reservation from the shared `ProjectQuantitySmoke.cs` to a dedicated `QuantityReportSignedZeroSmoke.cs` before any source/test write.
- Source fix: `5426f0a801ad0f51d288a79391467b688e471f8d` — `QuantityReportMath.NonNegative(...)` now canonicalizes every exact-zero result to positive `0d` only after the existing finite and finite-negative guards.
- Focused regression: `26cd6210944c89130154b79c502418e3c6172004` — a `ModuleInitializer` smoke builds a real semantic project, sends explicit IEEE negative-zero `WeightKg` through public `ProjectQuantityReportBuilder.Detail(...)`, and requires `MassKg` to be numeric zero with a positive-zero sign bit; it also guards ordinary positive mass.

## Reconciliation

- After the initial claim, concurrent changes touched only a Quantity Insight claim, the MeasurementTrace contract smoke, and the V25 release workflow; `compare_commits` showed no overlap with reserved Reporting source/test surfaces.
- Between the source fix and regression commit, the only concurrent change modified the Quantity Insight claim; no Reporting source or reserved test path overlap occurred.
- Current `main` was re-fetched after source/test publication and was exactly `26cd6210944c89130154b79c502418e3c6172004` before closing this claim.

## Validation actually executed

- Re-fetched exact remote `src/QS3D.Core/Reporting/QuantityReportMath.cs` at `26cd6210944c89130154b79c502418e3c6172004` and verified the existing finite/negative guards remain intact and the only new source behavior is final exact-zero canonicalization.
- Re-fetched exact remote `tests/QS3D.Core.SmokeTests/QuantityReportSignedZeroSmoke.cs` and verified the regression exercises public `ProjectQuantityReportBuilder.Detail(...)`, constructs negative zero by sign bit, requires `BitConverter.DoubleToInt64Bits(...) == 0L`, and preserves an ordinary positive mass.
- Re-fetched the smoke project and verified it targets `net8.0`; the new `.cs` file is included by the SDK project's default compile glob, matching the repository's existing `ModuleInitializer` smoke pattern.
- Local executable managed smoke/build: `NOT_RUN` — this execution environment has no `dotnet`, `csc`, `mcs`, or `msbuild` command available.
- GitHub Actions: `NOT_RUN` / not dispatched.
- BricsCAD native qualification: `NOT_APPLICABLE` to this pure Core reporting representation fix; no native PASS is claimed.

## Completion condition

Satisfied: claim-first coordination is recorded, the narrow source fix and public-builder sign-bit regression are present on remote `main`, intervening commits were reconciled without overlap, remote source/test were read back, and validation is reported only at the level actually executed.
