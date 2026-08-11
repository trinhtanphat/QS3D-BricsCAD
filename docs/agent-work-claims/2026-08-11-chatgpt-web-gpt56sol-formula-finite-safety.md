# Agent work claim — formula finite-safety

Status: COMPLETED

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Baseline main SHA observed before reservation: `586ff531f50a9ca71162fe7c9622e6329995dfbc`

## Scope

Harden the CAD-independent formula evaluator so non-finite values cannot be hidden by later arithmetic/function evaluation and produce a finite-but-invalid final result.

Implementation surfaces:

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- `tests/QS3D.Core.SmokeTests/FormulaFiniteSafetySmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Concrete defect fixed

`ExpressionEvaluator` previously rejected `NaN`/`Infinity` only at the final parse result. Intermediate overflow such as `1e308 * 1e308`, or a caller-supplied non-finite variable, could flow through a later function such as `min`/`max` and be masked into a finite result.

The evaluator now fails closed when binary arithmetic, unary negation, function output, or variable lookup produces/contains a non-finite value. Valid finite formulas retain the existing behavior.

## Exclusions

- No BricsCAD V25/native runtime changes.
- No UI, updater, installer, quantity-palette, Ribbon, Direct Draw, interchange, rebar, or other claimed work.
- No formula-language expansion or syntax changes beyond finite-number safety.
- No GitHub Actions dispatch.

## Validation

Added `FormulaFiniteSafetySmoke` covering:

- multiplication overflow hidden behind `min`;
- addition overflow hidden behind `max`;
- positive-infinity variable input;
- NaN variable input;
- existing finite `min` and multiplication behavior.

Validation is deterministic/Core-only and is registered in the existing smoke runner. No licensed BricsCAD runtime is required for this contract.
