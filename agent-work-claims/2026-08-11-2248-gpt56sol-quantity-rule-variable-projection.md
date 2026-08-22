# Work claim — quantity rule variable projection integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-variable-projection-20260811-2248`
- Registered: `2026-08-11T22:48:00+07:00`
- Completed: `2026-08-11T22:53:00+07:00`
- Baseline main SHA: `a4242d4cb4a4fcee742fee3925a3e8e03ddb4f5c`
- Priority: evidence-driven Core regression hardening during owner-requested `continue all`

## Completed scope

Hardened `QuantityRuleEngine` variable projection so unrelated numeric metadata whose key is blank/whitespace cannot poison otherwise valid quantity-rule evaluation.

## Changed surfaces

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableProjectionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableProjectionSmokeRegistration.cs`
- this claim file for coordination/close-out

## Defect fixed

`BuildVariables()` imports numeric family/element properties through `AddNumeric(...)`. Public property dictionaries can contain blank or whitespace-only keys. The formula evaluator correctly rejects blank variable names, so an unrelated numeric metadata entry that cannot be referenced by the expression could make a valid quantity rule fail.

`AddNumeric(...)` now skips whitespace-only property keys at the projection boundary. `ExpressionEvaluator` remains strict, and valid named numeric properties retain the existing behavior.

## Regression coverage

Focused smoke coverage verifies:

- whitespace-only numeric family metadata is ignored;
- whitespace-only numeric element metadata is ignored;
- valid `Factor` and `LengthM` properties still project into the expression;
- the matching rule evaluates to the expected value;
- canonical `Rule:<Output>` provenance is still recorded.

The smoke is registered through a dedicated `ModuleInitializer` file, avoiding shared smoke-runner edits.

## Integration

- Claim commit: `6d866816c61fc60476af2f24f22c83b19e57d1ec`
- Atomic implementation commit: `90e69810e8b1f6400ab4a5af8c5a2e6e24e7051b`
- Temporary branch refresh merge: `d52790ef1553ceae4e9d8a7a2fb98902887bd608`
- PR: `#512` — `fix(rules): ignore blank projected variable keys`
- `main` integration merge: `8564b3d0dab74de44175443914faa913447d3382`
- Later `main` observed during post-merge verification: `b96a1f7376c564036f05defac5ecacc8a0737ac2`

The integration merge is an ancestor of the later observed `main`, and the intervening commits did not modify this lane's files.

## Validation actually performed

- Re-read `QuantityRuleEngine.cs` from remote `main`; the blank-key filter is present directly inside `AddNumeric(...)` before numeric parsing/projection.
- Re-read the focused smoke and its `ModuleInitializer` registration from remote `main`.
- Verified PR #512 contained exactly three changed files and GitHub reported it mergeable after refreshing `main` into the temporary branch.
- Verified `8564b3d0dab74de44175443914faa913447d3382` is an ancestor of later observed `main` `b96a1f7376c564036f05defac5ecacc8a0737ac2`.
- GitHub combined commit status for the integration merge returned no status contexts.
- GitHub Actions were not dispatched.
- Local compilation/Core smoke execution and BricsCAD V25 runtime execution were not available in this remote connector environment, so no unexecuted build/runtime PASS is claimed.

## Exclusions retained

No `ExpressionEvaluator`, domain dictionary contract, quantity-rule category/create-command/UI/settings/reporting, shared runner, BricsCAD V25/native/runtime, updater/licensing, Build3D, Xref, rebar, persistence/interchange, Actions, release, or LOCAL_PASS changes/claims were made.
