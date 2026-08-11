# Agent work claim — formula finite-safety

Status: ACTIVE

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Baseline main SHA observed before reservation: `586ff531f50a9ca71162fe7c9622e6329995dfbc`

## Scope

Harden the CAD-independent formula evaluator so non-finite values cannot be hidden by later arithmetic/function evaluation and produce a finite-but-invalid final result.

Expected implementation surfaces:

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- `tests/QS3D.Core.SmokeTests/Program.cs`
- this claim file for completion status

## Concrete defect

`ExpressionEvaluator` currently rejects `NaN`/`Infinity` only at the final parse result. Intermediate overflow (for example `1e308 * 1e308`) or a caller-supplied non-finite variable can therefore flow through a later function such as `min`/`max` and be masked into a finite result. Formula evaluation should fail closed as soon as a non-finite operand/result appears.

## Exclusions

- No BricsCAD V25/native runtime changes.
- No UI, updater, installer, quantity-palette, Ribbon, Direct Draw, interchange, rebar, or active claimed work.
- No formula-language expansion or syntax changes beyond finite-number safety.
- No GitHub Actions dispatch.

## Validation plan

- Add deterministic smoke regression(s) proving intermediate overflow and non-finite variables fail closed.
- Preserve existing finite arithmetic/function behavior.
- Review the exact final diff against the latest `main` before integration.
