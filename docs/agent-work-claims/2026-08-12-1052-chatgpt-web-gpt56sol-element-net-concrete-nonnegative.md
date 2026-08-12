# Work claim — ElementInstance net concrete non-negative invariant

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:52:00+07:00`
- Baseline main SHA: `1765f34f13baf916ba8c8893794d9364e6da8af5`
- Priority: evidence-driven remote-safe domain quantity integrity

## Reason

`ElementInstance` validates `GrossConcreteM3` and `DeductionM3` independently as finite non-negative measurements, but `NetConcreteM3` currently returns their subtraction even when deduction exceeds gross. `QuantityReportBuilder` explicitly requires `NetConcreteM3` to be non-negative, so an otherwise setter-valid instance can expose a guaranteed-invalid negative net quantity and fail only later at the reporting boundary.

## Intended scope

Make `NetConcreteM3` fail closed when deduction exceeds gross concrete while preserving independent setter/population order, zero/equal deduction behavior, finite-overflow handling, and all other measurement properties.

## Changed surfaces

- `src/QS3D.Core/Domain/ElementInstance.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.
