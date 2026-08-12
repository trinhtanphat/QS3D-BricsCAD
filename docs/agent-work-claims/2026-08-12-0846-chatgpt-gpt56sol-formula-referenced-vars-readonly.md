# Work claim — Formula referenced-variable readonly result

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-formula-referenced-vars-readonly`
- Registered: `2026-08-12T08:46:00+07:00`
- Baseline main SHA: `2243fd1470d4958cccbdb3e34f827a0b12be543b`
- Priority: deterministic Core API contract integrity during owner-requested continue-all audit
- Task Key: `CORE-FORMULA-REFERENCED-VARIABLES-READONLY`

## Confirmed defect

`ExpressionEvaluator.GetReferencedVariables(...)` declares `IReadOnlyCollection<string>` but currently returns its mutable `List<string>` directly. A caller can cast the returned object back to `List<string>` and mutate the result despite the public readonly contract. Other Core result surfaces already use read-only wrappers for the same contract.

## Reserved scope

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- one focused Core smoke file and isolated registration if required by the smoke-test convention
- this claim file for close-out

## Contract

- return a genuinely read-only referenced-variable collection;
- preserve first-reference order, case-insensitive deduplication, identifier/function distinction, numeric-token validation and expression limits;
- no formula grammar, evaluation arithmetic, variable binding or business-rule changes.

## Validation plan

Add deterministic Core smoke coverage proving the returned collection preserves expected order/content and cannot be mutated through a mutable collection/list cast or generic collection mutation path.

No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim from this remote lane.
