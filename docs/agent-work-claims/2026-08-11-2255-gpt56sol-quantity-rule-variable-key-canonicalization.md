# Work claim — quantity rule variable key canonicalization

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-variable-key-canonicalization-20260811-2255`
- Registered: `2026-08-11T22:55:00+07:00`
- Completed: `2026-08-11T22:59:30+07:00`
- Baseline main SHA: `c41ceec37e66453438926ae14f201627afb1bec8`
- Priority: follow-on Core invariant hardening during owner-requested `continue all`

## Completed scope

Canonicalized nonblank variable names at the `QuantityRuleEngine` projection boundary so leading/trailing whitespace cannot defeat intended source precedence or create duplicate variables only after evaluator normalization.

## Changed surfaces

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableKeyCanonicalizationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableKeyCanonicalizationSmokeRegistration.cs`
- this claim file for close-out

## Defect fixed

The engine projects family properties, then element properties, then quantities into a case-insensitive dictionary, which establishes a deliberate later-source-wins precedence. Previously surrounding whitespace remained part of the projected key, so `" Factor "` and `"factor"` could coexist until `ExpressionEvaluator.NormalizeVariables()` trimmed them and rejected the normalized duplicate.

Projection now uses one `AddVariable(...)` boundary that skips whitespace-only names and stores nonblank names trimmed. Family, element, and quantity sources therefore share the same trim + case-insensitive identity before precedence is applied.

## Regression coverage

Focused smoke coverage verifies:

- padded family `Factor` and canonical element `factor` collapse to one variable and the later element value wins;
- padded element `LengthM` and padded direct quantity `lengthm` collapse to one variable and the later quantity value wins;
- whitespace-only property and quantity keys remain ignored;
- the rule evaluates to the expected value `15`;
- canonical `Rule:ProjectedQuantity` provenance is recorded.

The regression is registered through a dedicated `ModuleInitializer`, with no shared runner edit.

## Integration

- Claim commit: `d660d7ec1b5c93fbeee5c23f8f69b29cd05c0382`
- Atomic implementation commit: `4b390222f76286ab0e1f6eb14cda06d051a3206a`
- Temporary branch refresh merge: `75bd57c428206521f491ed463f39fde0aedbc3c8`
- PR: `#514` — `fix(rules): canonicalize projected variable keys`
- `main` integration merge: `e79ae1e070eb05e564bf92a8e6dbd33321c16819`
- Later `main` observed during post-merge verification: `ab3117366977c0de07a8d6464a31609f6d3f492e`

The integration merge is an ancestor of the later observed `main`; intervening commits did not modify this lane's files.

## Validation actually performed

- Re-read `QuantityRuleEngine.cs` from remote `main`; quantities and numeric properties both flow through canonical `AddVariable(...)`.
- Re-read the focused regression and its `ModuleInitializer` registration from remote `main`.
- Verified PR #514 contained exactly three changed files and GitHub reported it mergeable after refreshing `main` into the temporary branch.
- Verified `e79ae1e070eb05e564bf92a8e6dbd33321c16819` is an ancestor of later observed `main` `ab3117366977c0de07a8d6464a31609f6d3f492e`.
- GitHub Actions were not dispatched.
- Local compilation/Core smoke execution and BricsCAD V25 runtime execution were not available in this remote connector environment, so no unexecuted build/runtime PASS is claimed.

## Exclusions retained

No `ExpressionEvaluator`, domain dictionary API, persistence schema, quantity-rule category/create/UI/settings/reporting, shared runner, BricsCAD V25/native/runtime, updater/licensing, Build3D, Xref, rebar, Actions, release, or LOCAL_PASS changes/claims were made.
