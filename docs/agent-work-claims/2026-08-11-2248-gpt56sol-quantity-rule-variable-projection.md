# Work claim — quantity rule variable projection integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-variable-projection-20260811-2248`
- Registered: `2026-08-11T22:48:00+07:00`
- Baseline main SHA: `a4242d4cb4a4fcee742fee3925a3e8e03ddb4f5c`
- Priority: evidence-driven Core regression hardening during owner-requested `continue all`

## Reserved scope

Harden `QuantityRuleEngine` variable projection so unrelated numeric metadata whose key is blank/whitespace cannot poison otherwise valid quantity-rule evaluation.

## Expected surfaces

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableProjectionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableProjectionSmokeRegistration.cs`
- this claim file for close-out

## Concrete defect

`BuildVariables()` imports numeric family/element properties through `AddNumeric(...)`. Public property dictionaries can contain blank or whitespace-only keys. The formula evaluator correctly rejects blank variable names, so a numeric metadata entry that cannot even be referenced by the expression can make an unrelated valid rule fail.

The projection boundary should ignore keys that cannot be formula identifiers instead of weakening evaluator validation.

## Explicit exclusions

- No `ExpressionEvaluator` changes.
- No `ProjectState`, `ProjectFamily`, or `ProjectElement` dictionary contract changes.
- No quantity-rule category/create-command/UI/settings/reporting changes.
- No shared `Program.cs` or `SmokeTestRegistration.cs` edits; focused regression will use the current `ModuleInitializer` pattern.
- No BricsCAD V25/native/runtime, updater/licensing, Build3D, Xref, rebar, persistence/interchange, Actions, or release work.

## Validation plan

- A valid rule succeeds even when family/element properties contain unrelated numeric whitespace-only keys.
- Valid numeric properties still project and participate in evaluation.
- Focused regression verifies expected quantity and provenance.
- Refresh/compare `main` before implementation, avoid concurrent overlap, publish atomically, then re-read source/test from remote `main`.

## Completion condition

The projection ignores unreferenceable whitespace-only property names without weakening formula validation, focused regression coverage is on current `main`, and this claim is marked `COMPLETED` with exact integration SHA and validation actually performed.
