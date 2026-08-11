# Agent work claim — Revert active Floor/Zone canonical rewrite regression

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: correct the just-merged PR #590 after audit found it contradicts the existing Floor/Zone mutation-integrity contract from #545: semantic aliases are intentionally canonical no-ops that preserve stored raw identity, and the repository preflight explicitly requires trimmed case-insensitive comparisons.
- Files reserved:
  - `src/QS3D.Core/Domain/ProjectFloorService.cs`
  - `src/QS3D.Core/Domain/ProjectZoneService.cs`
  - `tests/QS3D.Core.SmokeTests/ProjectFloorServiceSmoke.cs`
  - `tests/QS3D.Core.SmokeTests/ProjectZoneServiceSmoke.cs`
  - `docs/agent-work-claims/2026-08-12-chatgpt-web-gpt56sol-active-floor-zone-canonical-id.md`
  - this claim file
- Corrective contract:
  - restore trimmed `OrdinalIgnoreCase` active-id no-op comparisons exactly as required by `preflight-project-floor-zone-mutation-integrity.py`;
  - remove the PR #590 smoke assertions that incorrectly require alias rewriting and `ChangeVersion` increments;
  - preserve the prior #545 null-target and assignment semantics untouched;
  - document that PR #590 was reverted as a superseded/incorrect interpretation rather than leaving a misleading completed claim.
- Validation: final diff must be limited to undoing PR #590 source/test behavior plus claim bookkeeping. No GitHub Actions dispatch and no BricsCAD runtime PASS claimed from this web session.
