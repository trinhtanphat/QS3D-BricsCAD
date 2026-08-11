# Agent work claim — formula variable case-folding

Status: COMPLETED

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Baseline main SHA observed before reservation: `d41feb1d3cdc2d34811316d5a7b400c1f2b4079d`
Claim commit: `0001c038e59b564bba7f691f6fb44cf8b5d6db95`

## Scope

Make formula variable binding consistently case-insensitive, matching the evaluator's existing case-insensitive referenced-variable semantics and function/constant behavior.

Implementation surfaces:

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- `tests/QS3D.Core.SmokeTests/FormulaVariableCaseSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Concrete defect fixed

`ExpressionEvaluator.GetReferencedVariables` already deduplicates identifiers with `StringComparer.OrdinalIgnoreCase`, but `Evaluate` previously passed the caller's `IReadOnlyDictionary<string, double>` directly into the parser. A normal case-sensitive `Dictionary<string, double>` containing `Width` therefore failed to bind a formula reference such as `width`.

`Evaluate` now normalizes caller variables into an `OrdinalIgnoreCase` lookup before parsing. Case-only duplicate names are rejected explicitly instead of producing order-dependent results, and the existing non-finite value checks remain in force after normalization.

## Exclusions

- No formula grammar expansion, persistence, reporting, BricsCAD V25/native runtime, UI, updater, installer, Ribbon, Direct Draw, interchange, or rebar changes.
- No changes to the completed formula finite-safety contract beyond preserving it through the normalized lookup.
- No GitHub Actions dispatch.

## Validation

Added and registered `FormulaVariableCaseSmoke` covering:

- mixed-case addition against a normal case-sensitive caller dictionary;
- mixed-case multiplication;
- fail-closed case-only duplicate variable names;
- preservation of non-finite variable rejection through case-insensitive lookup.

Reviewed the pushed commit diffs and re-read `ExpressionEvaluator.cs` from current remote `main` after integration. The Core target remains `netstandard2.0`; the implementation uses APIs compatible with that target. GitHub Actions were not dispatched per repository policy.

## Product commits

- `6f0aeba8729dab3748c2540dfa03d66f7f9d1c56` — `fix(formulas): normalize variable lookup casing`
- `1b2ac435347308314d334d51581acb3cffa765bf` — `test(formulas): cover case-insensitive variable binding`
- `a3ee890e2560aa9fb2135596f3140ef5d5598e71` — `test(formulas): register variable casing smoke`
