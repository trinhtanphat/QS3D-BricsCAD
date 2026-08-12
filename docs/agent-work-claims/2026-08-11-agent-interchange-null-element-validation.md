# Agent Work Claim — Interchange null-element validation

- Agent: ChatGPT remote agent
- Owner: OpenAI ChatGPT
- Status: COMPLETED
- Scope: Harden `ProjectInterchangeSemanticReferenceValidator.Validate(ProjectState)` so malformed project element collections containing `null` fail through the validator's explicit domain error instead of throwing from the pre-validation sort.
- Claimed Files:
  - `src/QS3D.Core/Export/ProjectInterchangeSemanticReferenceValidator.cs`
  - `tests/QS3D.Core.SmokeTests/ProjectInterchangeSemanticReferenceValidationSmoke.cs`
  - this claim file
- Branch: `agents/interchange-null-element-validation-fix-20260811`
- Started At: 2026-08-11 23:46 +07:00
- Last Updated: 2026-08-11 23:55 +07:00
- Completed At: 2026-08-11 23:55 +07:00
- Implementation: PR #542, squash commit `c096bfafc27d8590b3b34f6f23d8abaaf1f0007d`.
- Validation: focused Core regression source added and PR diff reviewed; GitHub Actions were not dispatched per repository policy; no BricsCAD V25/runtime PASS claimed or required for this pure-Core lane.
- Result: null semantic elements now reach the validator's explicit `InvalidOperationException` guard instead of throwing `NullReferenceException` from pre-validation ordering.
