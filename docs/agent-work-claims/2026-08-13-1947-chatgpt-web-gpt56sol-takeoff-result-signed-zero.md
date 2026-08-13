# Work claim — TakeoffResult signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-takeoff-result-signed-zero-20260813-1947`
- Registered: `2026-08-13T19:47:00+07:00`
- Baseline main SHA: `fb7444993e11f04ee8db770fe59b5264785001a9`
- Priority: P1 measurement/takeoff canonicality. `TakeoffResult` rejects negative/non-finite values but stores IEEE `-0d` unchanged because `-0d < 0d` is false. `MeasurementTrace` already canonicalizes zero through `MeasurementTraceContract.RequireFinite`, so `QuantityEngine.CalculateWithTrace` can expose a signed-negative-zero `TakeoffResult.Value` while the paired canonical trace exposes positive zero for the same quantity.

## Reserved scope

- `src/QS3D.Core/Takeoff/TakeoffResult.cs`
- new `tests/QS3D.Core.SmokeTests/TakeoffResultSignedZeroSmoke.cs`
- new `tests/QS3D.Core.SmokeTests/TakeoffResultSignedZeroRegistration.cs`
- this claim file for closeout

## Intended bounded change

Canonicalize accepted zero values at the `TakeoffResult` boundary (`value == 0d ? 0d : value`) so direct results and QuantityEngine outputs cannot retain negative-zero sign bits. Preserve existing handle/unit trimming, enum validation, finite/non-negative validation, and positive quantity behavior. Add focused smoke coverage for direct construction plus `Calculate` / `CalculateWithTrace` zero parity.

## Excluded scope

- no `MeasurementTrace`, `MeasurementSnapshot`, formula, quantity-rule, unit-conversion or persistence changes;
- no MAP-01B files, QSC-01A files, report/UI/native BricsCAD work, GitHub Actions or force-push;
- no managed/native PASS claim unless actually executed.

## Coordination

Current active MAP-01B QSDB-v4 mapping persistence reserves ProjectState/persistence files, and current active QSC-01A reserves only new QS rule-profile files; both are disjoint. Recent exact claim search found no Takeoff signed-zero lane. Earlier MTR-03 raw takeoff trace projection is completed history.

## Completion condition

Claim-only commit is published first; overlap is rechecked; production fix and focused registered smoke are pushed to current `main`; exact remote diffs/ancestry are verified; claim is closed `COMPLETED` with actual validation boundaries recorded.
