# Work claim — Formula reference grammar parity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-formula-reference-grammar-parity`
- Registered: `2026-08-12T08:55:00+07:00`
- Completed: `2026-08-12T09:05:00+07:00`
- Baseline main SHA: `6bb1cf3f0a15eb78805d9273fb2b18ccf7b8e98a`
- Claim commit: `910c9fd9b2d8fa29576c54dce6a492e3a0c858e5`
- Source commit: `5954521393578aea948fa3987c470695abfee8eb`
- Smoke commit: `718f1d73095afce30452d5d3c8b50f4925f8c44f`
- Main observed after regression: `f85f26d0d0034244266a9106fae8ed78c2bfbfb1`
- Priority: deterministic Core formula diagnostics during owner-requested continue-all audit
- Task Key: `CORE-FORMULA-REFERENCE-GRAMMAR-PARITY`

## Confirmed defect

`ExpressionEvaluator.GetReferencedVariables(...)` used a lexical scanner instead of the evaluator grammar parser. `QuantityRuleEngine` consumed those references to build its dependency graph before formula evaluation, so malformed mutually-referencing expressions such as `B +` and `A +` could surface a circular-dependency diagnostic before their actual syntax error.

## Completed scope

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs` now performs reference discovery through the same parser grammar used by evaluation.
- Parser reference mode records first-seen variables case-insensitively while preserving display order and the existing read-only result wrapper.
- Reference mode consumes and validates grammar/function shape but skips variable binding and runtime arithmetic, so dependency inspection does not introduce division-by-zero/non-finite execution failures.
- `tests/QS3D.Core.SmokeTests/FormulaReferenceGrammarParitySmoke.cs` covers incomplete syntax, false-cycle prevention through `QuantityRuleEngine`, non-executing `A / 0` dependency inspection, retained runtime division-by-zero safety, and function-arity validation.
- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` required no source edit.

## Validation performed

- Reviewed the source commit diff and re-read the integrated `ExpressionEvaluator.cs` from moving `main` after the smoke commit.
- Reviewed the focused smoke commit and its module-initializer registration style against the existing Formula smoke convention.
- Confirmed the earlier referenced-variable readonly wrapper remains present.
- Confirmed concurrent `main` movement after the regression did not remove the integrated Formula source patch when re-read.
- No GitHub Actions/build/release dispatch was performed.
- No local .NET build or BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Completion condition

Completed. Formula dependency discovery now shares evaluator grammar without executing runtime arithmetic, malformed expressions fail before false circular-dependency classification, focused regression coverage is committed on `main`, and this reservation is released.
