# Work claim — ElementInstance net concrete floor-zero semantics

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:08:00+07:00`
- Baseline main SHA: `4c7eab8a0494a10da5211332a74cbf01d106167d`
- Claim commit: `65b4adc6bcc68d3637f637f0709c6e11d0981b0d`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect fixed

`ElementInstance` validates every stored measurement as finite and non-negative, but `NetConcreteM3` previously returned `GrossConcreteM3 - DeductionM3` without flooring the result. A valid pair of stored measurements such as gross `1` and deduction `2` therefore exposed a negative concrete volume. The Core quantity arithmetic contract already provides floor-zero subtraction, and reporting quantity helpers require non-negative quantities, so this legacy derived-property boundary was inconsistent with the established non-negative quantity semantics.

`NetConcreteM3` now preserves the existing finite-result guard and returns `Math.Max(0d, value)` for finite subtraction results.

## Implementation surfaces

- `src/QS3D.Core/Domain/ElementInstance.cs`
- `tests/QS3D.Core.SmokeTests/ElementInstanceNetConcreteFloorZeroSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ElementInstanceNetConcreteFloorZeroRegistration.cs`
- this claim file

## Product commits

- `90e9b4a863cafbde898993a3395438ad24f4cd23` — `fix(core): floor net concrete at zero`
- `303c4e9dc15bc6c3f06cc4d81e1c98b5a8028424` — `test(core): cover net concrete floor zero`
- `9aa4f73b268fbc8d22914e9fbb6051b45692e965` — `test(core): register net concrete floor zero smoke`

## Regression coverage

Focused smoke source verifies:

- normal finite subtraction remains exact (`10 - 3 = 7`);
- equal gross/deduction returns zero;
- deduction greater than gross floors to zero instead of exposing a negative volume;
- maximum finite equality remains zero;
- existing NaN, Infinity, and negative measurement setter guards remain enforced.

Registration uses a dedicated module initializer and does not edit shared smoke registration.

## Coordination / validation truth

- The claim was published and re-read from `main` before product changes.
- `ElementInstance.cs` was re-read after claim publication and still had the expected pre-fix blob before the source write.
- Exact implementation diff was re-read after push and contains only the one-line floor-zero return change.
- Exact smoke and registration diffs were re-read after push.
- Comparison from implementation commit `90e9b4a863cafbde898993a3395438ad24f4cd23` to registration commit `9aa4f73b268fbc8d22914e9fbb6051b45692e965` reported `behind_by: 0` with the implementation commit as merge base; intervening concurrent changes were disjoint claim/docs files plus this smoke coverage.
- At the ancestry checkpoint, `main` was exactly `9aa4f73b268fbc8d22914e9fbb6051b45692e965`.
- Hosted environment has no confirmed .NET SDK / BricsCAD runtime execution in this lane, so no runtime PASS is claimed.
- No GitHub Actions were dispatched.

## Exclusions respected

No reporting aggregation, ProjectElement quantity storage, family/domain identity, persistence, native adapter, or BricsCAD runtime code was changed.

## Completion condition

Satisfied for remote/source scope: net concrete cannot become negative from otherwise valid finite non-negative gross/deduction inputs, existing setter guards remain intact, focused regression source is registered on `main`, and the claim is released as `COMPLETED`.
