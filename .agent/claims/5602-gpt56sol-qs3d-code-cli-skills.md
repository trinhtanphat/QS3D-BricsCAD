# Reservation-v2 claim — #5602 QS3D Code CLI + skills

Issue: #5602
Parent: #5545
Plan: `docs/superpowers/plans/2026-09-04-qs3d-code-embedded-agent-harness.md`
Reservation-Protocol: v2
Owner: `account:trinhtanphat|session:gpt56sol-20260904t1027-cli-skills`
Lane-Key: `issue-5602`
Ownership-Key: `qs3d-code.cli-skills-v1`
Branch: `agent/gpt56sol-20260904t1027-cli-skills/issue-5602-qs3d-code-cli-skills`
Baseline: `main@f6c2b4c33eb0e20e219d324df4b4b43777c3fb8b`

Expected-Paths:
- `src/QS3D.Code.Cli/QS3D.Code.Cli.csproj`
- `src/QS3D.Code.Cli/Program.cs`
- `src/QS3D.Code.Cli/Qs3dCliApplication.cs`
- `src/QS3D.Code.Cli/RepositorySkillLoader.cs`
- `src/QS3D.Code.Cli/ConsoleTraceRenderer.cs`
- `tests/QS3D.Code.Cli.SmokeTests/QS3D.Code.Cli.SmokeTests.csproj`
- `tests/QS3D.Code.Cli.SmokeTests/Program.cs`
- `.agent/skills/repository-lifecycle/skill.yaml`
- `.agent/skills/tdd-source/skill.yaml`
- `.agent/skills/ci-remediation/skill.yaml`
- `.agent/skills/github-lifecycle/skill.yaml`
- `.agent/skills/mcp-transport/skill.yaml`
- `.agent/skills/persistence-durability/skill.yaml`
- `.agent/skills/bricscad-host/skill.yaml`
- `.agent/skills/cad-safety/skill.yaml`
- `.agent/skills/release-local-only/skill.yaml`
- `.agent/claims/5602-gpt56sol-qs3d-code-cli-skills.md`

Scope: Child 2 only — strict repository-owned skill manifests and repo-local `qs3d` route/dry-run/trace client over merged Harness Core. No provider execution, GitHub/shell mutation, CAD mutation, host bridge/IPC, or embedded UI.

TDD RED expectation: the smoke project is committed before production files and references the intentionally absent `src/QS3D.Code.Cli/QS3D.Code.Cli.csproj` / CLI types. The focused smoke command must fail until the minimum GREEN CLI and strict manifest loader are implemented.
