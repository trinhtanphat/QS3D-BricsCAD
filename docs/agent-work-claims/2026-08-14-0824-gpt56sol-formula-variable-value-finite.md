# Work claim — Formula variable value finite integrity

- Status: `COMPLETED`
- Agent: `gpt56sol-formula-variable-value-finite-20260814-0824`
- Baseline main SHA: `726dcd4e1cda5a56ac64677cacc4614b7b17b913`
- Scope: `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`; `tests/QS3D.Core.SmokeTests/FormulaFiniteSafetySmoke.cs`.

## Confirmed defect

`ExpressionEvaluator.NormalizeVariables()` validated every supplied variable name before parsing, including unused variables, but did not validate supplied values. Non-finite values were rejected only later when a variable was referenced by the expression. Consequently the same malformed variable context could pass or fail solely according to expression text: an unused `NaN`/`Infinity` survived normalization while a referenced one failed. This was inconsistent with the evaluator's existing finite-input contract and with full-context name validation.

## Implemented

- Claim-only commit: `4ed4fec84827241edc5bb64cf760aa467413518d`.
- Source fix: `38e03875ea83e6bc918e486293411b3586b8caa8`.
  - `NormalizeVariables()` now rejects `NaN` and positive/negative infinity before adding the variable to the normalized context.
  - Existing blank-name and duplicate-name validation still has precedence.
  - Finite unused variables remain accepted.
- Focused regression: `1fa65c2167d578f496743000e46a58b87039ee52`.
  - unused `+Infinity`, `-Infinity` and `NaN` fail closed;
  - a finite unused variable remains accepted.

## Validation actually executed

- Refreshed live `main` before claim, after claim and after source/test writes; no competing Formula claim appeared in recent Formula history.
- Remote commit readback verified the source diff is limited to the two-line finite-value guard in `NormalizeVariables()`.
- Remote commit readback verified focused finite-safety cases only were added to `FormulaFiniteSafetySmoke`.
- Current-main ancestry after the regression write included `1fa65c2167d578f496743000e46a58b87039ee52` directly before concurrent claim-completion work, preserving both Formula commits.
- No GitHub Actions were dispatched. This runtime does not provide the repository's managed/native build environment, so no executable .NET/BricsCAD PASS is claimed.

## Excluded scope

Formula grammar/functions beyond this invariant, Rules/domain callers, persistence, quantity/cost semantics, UI/host integration, release/update and native CAD behavior are unchanged.

## Completion

`COMPLETED`: malformed non-finite values can no longer hide in an unused Formula variable context; the source guard and focused regression are on remote `main`, concurrent work was preserved, and unavailable runtime/native gates are explicitly unclaimed.
