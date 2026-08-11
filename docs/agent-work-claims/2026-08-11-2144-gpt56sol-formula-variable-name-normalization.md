# Agent Work Claim

- Agent: `GPT-5.6 Sol`
- Status: `DONE`
- Started at: `2026-08-11T21:44:12+07:00`
- Scope: Harden CAD-independent formula variable-name normalization so caller dictionary keys with incidental leading/trailing whitespace bind consistently, while blank names and duplicates after trim + case-fold fail closed instead of producing ambiguous lookup behavior.
- Primary files:
  - `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
  - `tests/QS3D.Core.SmokeTests/FormulaVariableNameNormalizationSmoke.cs`
  - `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
  - `docs/agent-work-claims/2026-08-11-2144-gpt56sol-formula-variable-name-normalization.md`
- Tests intended:
  - Trimmed caller variable names bind to formula identifiers.
  - Whitespace-only variable names fail closed.
  - Names colliding after trim + case-insensitive normalization fail closed.
  - Existing case-insensitive and finite-value behavior remains unchanged.
- Dependencies:
  - Builds on the completed `formula-variable-casefold` and `formula-finite-safety` lanes already present on `main`; no active overlap found in the current claim directory.
- Notes:
  - Pure Core/netstandard-compatible change; no BricsCAD host/native runtime, updater, UI, Ribbon, Direct Draw, quantity, rebar, or interchange surfaces are in scope.
  - GitHub Actions were not dispatched; repository policy and this execution environment do not provide a local BricsCAD runtime.

## Result

`ExpressionEvaluator.NormalizeVariables` now trims caller-supplied variable names before inserting them into its case-insensitive lookup. Whitespace-only names are rejected, and names that collide after trim plus case-fold are rejected deterministically.

Regression coverage verifies trimmed-name binding, trimmed duplicate rejection, and blank-name rejection. The new smoke is registered in the existing Core smoke runner.

## Product commits

- `5fe1601e8a6878f4130669b4d70639331b665d94` — `test(formulas): cover variable name normalization`
- `69087e52ca9ae314415c13e412d9342e57ca36d4` — `fix(formulas): normalize variable name whitespace`
- `f3d2af6508d546c85fee68bf719e05aff1fb1196` — `test(formulas): register variable name normalization smoke`

## Validation

- Re-read `ExpressionEvaluator.cs`, the new regression smoke, and `SmokeTestRegistration.cs` from current remote `main` after integration; all intended changes remain present despite concurrent commits.
- The implementation uses APIs compatible with the existing Core target and does not introduce host/runtime dependencies.
- GitHub reports no commit status contexts for the final product commit; no GitHub Actions workflow was dispatched.
- Local compilation/runtime execution could not be performed in this execution environment because the local container has neither GitHub CLI/network resolution to clone the private repository nor a licensed BricsCAD runtime. This limitation is recorded rather than claiming an unexecuted build passed.
