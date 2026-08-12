# Work claim — Formula reference grammar parity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-formula-reference-grammar-parity`
- Registered: `2026-08-12T08:55:00+07:00`
- Baseline main SHA: `6bb1cf3f0a15eb78805d9273fb2b18ccf7b8e98a`
- Priority: deterministic Core formula diagnostics during owner-requested continue-all audit
- Task Key: `CORE-FORMULA-REFERENCE-GRAMMAR-PARITY`

## Confirmed defect

`ExpressionEvaluator.GetReferencedVariables(...)` currently uses a lexical scanner instead of the evaluator grammar parser. `QuantityRuleEngine` consumes those references to build its dependency graph before formula evaluation, so a malformed expression can participate in cycle detection and surface a circular-dependency diagnostic before its actual syntax error. For example, mutually-referencing rules with expressions `B +` and `A +` can be classified as a cycle even though both expressions are grammatically incomplete.

## Reserved scope

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- one focused Core smoke file under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

`src/QS3D.Core/Rules/QuantityRuleEngine.cs` is read-only context for this lane unless a source change proves strictly necessary after parser integration.

## Contract

- reference discovery must consume the same expression grammar as evaluation;
- malformed grammar/function calls must fail before dependency-cycle classification;
- reference discovery must not execute runtime arithmetic or variable binding, so syntactically valid expressions such as `A / 0` can still be inspected for dependencies;
- preserve expression length/depth/argument limits, numeric-token validation, first-reference order, case-insensitive deduplication, and the genuine read-only result contract already on `main`;
- no business-rule output semantics change for valid evaluated expressions.

## Validation plan

Add deterministic Core smoke coverage for incomplete syntax, false-cycle prevention through `QuantityRuleEngine`, valid runtime-invalid dependency inspection, and preservation of ordered/case-insensitive references.

No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim from this remote lane.
