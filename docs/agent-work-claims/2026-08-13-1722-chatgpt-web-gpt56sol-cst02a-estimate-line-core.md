# Work claim — CST-02A frozen EstimateLine core

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-cst02a-estimate-line-core-20260813-1722`
- Registered: `2026-08-13T17:22:00+07:00`
- Baseline main SHA: `5b3a048206bf07d43bb81e53da0e419a7f67e233`
- Priority: `CST-02 / P1` — establish the smallest deterministic estimate-line contract from the completed REV-01 measurement snapshot and CST-01A rate domain

## Confirmed gap

The workstream requires `EstimateLine` to distinguish measured quantity, estimating quantity, waste/commercial adjustment, unit rate and final amount, while remaining traceable to a frozen measurement snapshot and rate snapshot. Current `src/QS3D.Core/Cost/` contains only `RateBook.cs`; repository history/search contains no `EstimateLine` implementation or CST-02 commit. REV-01 `MeasurementSnapshot` and CST-01A `RateBook` are completed prerequisites.

## Reserved scope

Add one pure-Core immutable `EstimateLine` factory/contract that consumes:

- a frozen `MeasurementSnapshot` plus exact canonical trace identity `(semantic identity, source identity, quantity key)`;
- a `RateBook`, `CostCode`, currency and UTC as-of timestamp;
- one explicit additive `commercialAdjustmentQuantity` in the measurement unit, plus an adjustment reason when the adjustment is non-zero.

For this narrow sub-lane:

- the selected snapshot trace `NetValue` is the measured quantity; no geometry/live quantity lookup occurs;
- the rate is resolved through `RateBook.Resolve(...)` using the trace unit, requested cost code/currency and UTC as-of time; unmatched rates fail visibly;
- measured quantity is converted once to `decimal` for commercial arithmetic; overflow and non-zero-to-zero conversion loss fail visibly;
- `EstimatingQuantity = MeasuredQuantity + CommercialAdjustmentQuantity` in checked decimal arithmetic and must remain non-negative;
- `FinalAmount = EstimatingQuantity * UnitRate` in checked decimal arithmetic;
- the line retains the immutable selected `MeasurementTrace`, `RateItem`, `RateBookId` and rate as-of timestamp so the commercial result remains traceable to the frozen measurement/rate inputs;
- exact unit matching is preserved; this lane performs no unit conversion or FX conversion;
- zero commercial adjustment is allowed without a reason; non-zero adjustment requires canonical nonblank reason text.

## Expected surfaces

- new `src/QS3D.Core/Cost/EstimateLine.cs` — immutable line/factory and narrow validation helpers only;
- new `tests/QS3D.Core.SmokeTests/EstimateLineSmoke.cs` — snapshot/rate binding, arithmetic, traceability and failure regression;
- new `tests/QS3D.Core.SmokeTests/EstimateLineRegistration.cs` — focused ModuleInitializer registration;
- this claim file.

## Excluded scope

- No percentage/waste policy engine, markup/tax/discount/rounding policy, FX, unit conversion or remote rates.
- No persistence/schema/migration, Estimate/BQ collection, CST-03 revision cost impact or CST-04 renderer/export.
- No geometry, ProjectState, semantic entity or measurement-rule mutation.
- No edits to `MeasurementSnapshot`, `MeasurementTrace` or `RateBook` unless a concrete blocking defect is separately proven/claimed.
- No WPF/BricsCAD/native/local qualification and no GitHub Actions dispatch.

## Validation plan

- Re-fetch current `main` after this claim-only commit and reconcile any Cost/Estimate overlap before source write.
- Smoke covers exact snapshot trace selection, effective-rate selection, positive/negative/zero additive adjustment, measured/estimating/rate/final amount separation, immutable trace/rate evidence, unmatched/missing trace, unit/currency/rate-time contract, decimal overflow and sub-decimal underflow failure where constructible.
- Re-fetch exact source/test/registration blobs from pushed `main` before closeout.
- Managed build/smoke execution remains `NOT_RUN` unless a real .NET execution path becomes available; source inspection is not reported as executable PASS.

## Coordination

- CST-01A and REV-01A are completed prerequisites and remain unmodified.
- CST-03/CST-04 remain future lanes and are not reserved here.
- Current MTR-03R, diagnostic/UI, Curtain and LOCAL/native work are explicitly excluded and non-overlapping.

## Completion condition

A claim-first immutable EstimateLine core plus focused registered smoke is present on current `main`; commercial arithmetic is deterministic/checked and traceable to frozen measurement/rate inputs without geometry/rate assumptions leaking into semantic entities; remote blobs are verified; and this claim is closed with exact pushed SHAs plus validation actually executed.
