# Work claim — Formula variable value finite integrity

- Status: `ACTIVE`
- Agent: `gpt56sol-formula-variable-value-finite-20260814-0824`
- Baseline main SHA: `726dcd4e1cda5a56ac64677cacc4614b7b17b913`
- Scope: `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`; `tests/QS3D.Core.SmokeTests/FormulaFiniteSafetySmoke.cs`.

## Confirmed defect

`ExpressionEvaluator.NormalizeVariables()` validates every supplied variable name before parsing, including unused variables, but does not validate supplied values. Non-finite values are rejected only later when a variable is referenced by the expression. Consequently the same malformed variable context can pass or fail solely according to expression text: an unused `NaN`/`Infinity` survives normalization while a referenced one fails. This is inconsistent with the evaluator's existing finite-input contract and with full-context name validation.

## Goal

Fail closed during variable normalization when any supplied value is `NaN` or positive/negative infinity, including unused variables. Preserve current variable-name normalization, duplicate detection, finite unused variables, parser grammar, arithmetic checks, signed-zero canonicalization and referenced-variable behavior. Add focused finite-safety smoke regression.

## Excluded scope

Formula grammar/functions beyond this invariant, Rules/domain callers, persistence, quantity/cost semantics, UI/host integration, release/update and native CAD behavior are out of scope.

## Coordination

Claim published before source/test changes. Refresh `main` and re-check overlapping claims/commits before implementation; preserve concurrent work and do not force-push or dispatch Actions.
