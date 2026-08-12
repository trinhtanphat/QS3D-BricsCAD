# Work claim — ElementInstance net concrete floor-zero semantics

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:08:00+07:00`
- Baseline main SHA: `4c7eab8a0494a10da5211332a74cbf01d106167d`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect

`ElementInstance` validates every stored measurement as finite and non-negative, but `NetConcreteM3` currently returns `GrossConcreteM3 - DeductionM3` without flooring the result. A valid pair of stored measurements such as gross `1` and deduction `2` therefore exposes a negative concrete volume. The Core quantity arithmetic contract already provides `QuantityMath.SubtractFloorZero`, and reporting quantity helpers require non-negative quantities, so this legacy derived-property boundary is inconsistent with the established non-negative quantity semantics.

## Intended implementation surfaces

- `src/QS3D.Core/Domain/ElementInstance.cs`
- focused smoke source under `tests/QS3D.Core.SmokeTests/`
- dedicated smoke registration under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended change

Preserve existing finite/non-negative setters and overflow behavior, but floor finite negative net concrete results to zero. Cover normal subtraction, over-deduction, and overflow/finiteness behavior without changing unrelated quantity or reporting code.

## Coordination / validation

- No product source has been edited before this claim.
- Recent `main` history was checked for `ElementInstance`/net-concrete overlap before registration.
- No GitHub Actions will be dispatched.
- No .NET SDK or BricsCAD runtime PASS will be claimed unless actually executed.