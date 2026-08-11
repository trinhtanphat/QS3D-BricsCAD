# Agent Work Claim — Interchange null-element validation

- Agent: ChatGPT remote agent
- Owner: OpenAI ChatGPT
- Status: ACTIVE
- Scope: Harden `ProjectInterchangeSemanticReferenceValidator.Validate(ProjectState)` so malformed project element collections containing `null` fail through the validator's explicit domain error instead of throwing from the pre-validation sort.
- Claimed Files:
  - `src/QS3D.Core/Export/ProjectInterchangeSemanticReferenceValidator.cs`
  - focused Core smoke/regression file(s) covering interchange semantic-reference validation
  - focused static preflight only if an existing canonical preflight owns this contract
  - this claim file
- Branch: `agents/interchange-null-element-validation-20260811`
- Started At: 2026-08-11 23:46 +07:00
- Last Updated: 2026-08-11 23:47 +07:00
- Local Dependencies: None for the pure-Core source fix/regression. No BricsCAD V25 runtime PASS is claimed by this remote lane.
- Validation Plan: source-safe focused regression + diff/static review; no GitHub Actions dispatch; preserve semantic-reference behavior for valid projects and snapshot validation.
- Coordination: Do not edit Quantity Settings PR #535 or Start Center PR #536 lanes. Re-sync `main` and open PRs before the source slice.
