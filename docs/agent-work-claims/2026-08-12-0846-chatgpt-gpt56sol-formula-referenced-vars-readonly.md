# Work claim — Formula referenced-variable readonly result

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-formula-referenced-vars-readonly`
- Registered: `2026-08-12T08:46:00+07:00`
- Completed: `2026-08-12T08:51:00+07:00`
- Baseline main SHA: `2243fd1470d4958cccbdb3e34f827a0b12be543b`
- Claim commit: `e1334abfbed846f98118be122258b26206b5da9b`
- Branch source commit: `ba7ec7b06c52f014f065c535bceeb43a2205e4a1`
- Branch smoke commit: `fedd2ce0f7d48154376fbc4319f0269c02438d6b`
- Pull request: `#663`
- Main integration commit: `9c2a66b6d5a54922d6a6e755d58ff18870dfe5ca`
- Priority: deterministic Core API contract integrity during owner-requested continue-all audit
- Task Key: `CORE-FORMULA-REFERENCED-VARIABLES-READONLY`

## Confirmed defect

`ExpressionEvaluator.GetReferencedVariables(...)` declared `IReadOnlyCollection<string>` but returned its mutable `List<string>` directly. A caller could cast the returned object back to `List<string>` and mutate the result despite the public readonly contract.

## Completed scope

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs` now returns `result.AsReadOnly()`.
- `tests/QS3D.Core.SmokeTests/ExpressionReferencedVariablesReadOnlySmoke.cs` pins first-reference order, case-insensitive deduplication, function-name exclusion and mutation rejection through mutable/generic collection paths.
- No formula grammar, evaluation arithmetic, variable binding or business-rule behavior was changed.

## Validation performed

- Reviewed PR #663 patch: exactly one source-line change plus one focused smoke file.
- Compared branch against moving `main`; commits since the claim did not touch either Formula source or the new smoke path.
- Squash-merged PR #663 with expected head `fedd2ce0f7d48154376fbc4319f0269c02438d6b`.
- Re-read both integrated files from `main` and confirmed the read-only wrapper and smoke are present.
- No GitHub Actions/build/release dispatch was performed.
- No local .NET build or BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Completion condition

Completed. The Formula referenced-variable result is genuinely read-only on current `main`, focused regression coverage is committed, and the reservation is released.
