# Work claim — quantity rule variable key canonicalization

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-variable-key-canonicalization-20260811-2255`
- Registered: `2026-08-11T22:55:00+07:00`
- Baseline main SHA: `c41ceec37e66453438926ae14f201627afb1bec8`
- Priority: follow-on Core invariant hardening during owner-requested `continue all`

## Reserved scope

Canonicalize nonblank variable names at the `QuantityRuleEngine` projection boundary so leading/trailing whitespace cannot defeat intended source precedence or create duplicate variables only after the evaluator normalizes names.

## Expected surfaces

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableKeyCanonicalizationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableKeyCanonicalizationSmokeRegistration.cs`
- this claim file for close-out

## Concrete defect

The engine projects family properties, then element properties, then quantities into a case-insensitive dictionary, which establishes a deliberate later-source-wins precedence. Keys are currently stored with surrounding whitespace intact. Thus `" Factor "` and `"factor"` coexist until `ExpressionEvaluator.NormalizeVariables()` trims them and rejects the normalized duplicate, turning a valid precedence case into a rule failure.

The same issue can occur between property variables and directly exposed quantity dictionary keys. Projection should use the same trim + case-insensitive key identity before evaluation.

## Explicit exclusions

- No `ExpressionEvaluator` changes.
- No domain dictionary API or persistence schema changes.
- No quantity-rule category/create/UI/settings/reporting changes.
- No shared smoke runner edits; focused `ModuleInitializer` registration only.
- No BricsCAD V25/native/runtime, updater/licensing, Build3D, Xref, rebar, Actions, release, or LOCAL_PASS work.

## Validation plan

- Padded family key and canonical element key collapse to one variable and element precedence wins.
- Padded element property and canonical/padded quantity key collapse to one variable and quantity precedence wins.
- Valid rule evaluates expected value and records provenance.
- Existing blank-key filtering remains effective.
- Refresh/compare `main`, publish atomically through a temporary branch/PR if needed, and re-read remote `main` after integration.

## Completion condition

Projected variable names are canonicalized before precedence is applied, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact integration SHA and validation actually performed.
