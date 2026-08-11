# Agent work claim — formula variable case-folding

Status: ACTIVE

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Baseline main SHA observed before reservation: `d41feb1d3cdc2d34811316d5a7b400c1f2b4079d`

## Scope

Make formula variable binding consistently case-insensitive, matching the evaluator's existing case-insensitive referenced-variable semantics and function/constant behavior.

Expected implementation surfaces:

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- `tests/QS3D.Core.SmokeTests/FormulaVariableCaseSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file for completion status

## Concrete defect

`ExpressionEvaluator.GetReferencedVariables` deduplicates identifiers with `StringComparer.OrdinalIgnoreCase`, but `Evaluate` passes the caller's `IReadOnlyDictionary<string, double>` directly into the parser. A normal case-sensitive `Dictionary<string, double>` containing `Width` therefore fails to bind a formula reference such as `width`, even though the formula subsystem otherwise treats identifiers case-insensitively.

## Exclusions

- No formula grammar expansion, persistence, reporting, BricsCAD V25/native runtime, UI, updater, installer, Ribbon, Direct Draw, interchange, or rebar changes.
- No changes to the completed formula finite-safety contract.
- No GitHub Actions dispatch.

## Validation plan

- Add deterministic Core-only smoke coverage proving mixed-case variable references bind against a normal case-sensitive caller dictionary.
- Preserve existing finite-value rejection and formula behavior.
- Review the exact final diff against latest `main` before integration.
