# Work claim — ElementInstance net concrete finite closure

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `62beefb3f90e7459f32bf2cdbf6181c017cbfbca`
- Priority: owner-requested whole-repository audit; Core numeric integrity

## Verified defect

`ElementInstance` validates every stored measurement assigned through its setters with `RequireFinite(...)`, but `NetConcreteM3` is computed as an unchecked floating-point subtraction of `GrossConcreteM3 - DeductionM3`. Two individually finite doubles can overflow on subtraction (for example finite values near opposite `double` extremes), allowing the public derived quantity to become `Infinity` despite the type's finite-measurement contract.

This is the same numeric-closure class already treated elsewhere in Core: validating operands is not sufficient when a derived arithmetic result can leave the finite domain.

## Reserved scope

- `src/QS3D.Core/Domain/ElementInstance.cs`
- one focused `QS3D.Core.SmokeTests` regression for finite/overflowing `NetConcreteM3`
- smoke-test registration only if the test harness requires it
- `docs/ELEMENTINSTANCE-NET-CONCRETE-FINITE-PLAN-2026-08-12.md` (new)
- this claim file

## Non-overlap

- Do not change `ProjectElement`, quantity/reporting builders, XLSX exporters, formula engine, Floor/Zone services, or BricsCAD source.
- Preserve current semantics for all finite net-concrete results, including negative finite results; this lane only closes the non-finite arithmetic result hole.
- No GitHub Actions dispatch and no release publication.

## Intended contract

1. `NetConcreteM3` returns the same arithmetic result when that result is finite.
2. A subtraction that produces `NaN`/`Infinity` fails closed instead of exposing a non-finite semantic quantity.
3. Existing finite validation on stored measurements remains unchanged.
4. Regression coverage includes both a normal finite result and an overflowing pair of finite operands.

## Validation / release conditions

- Commit this claim before substantive code.
- Commit a planning MD before implementation.
- Re-fetch exact current source immediately before editing.
- Add focused Core smoke coverage.
- Verify source/test ancestry against current `main` with `behind_by: 0` before closure.
- Do not claim licensed BricsCAD V25 runtime PASS.
