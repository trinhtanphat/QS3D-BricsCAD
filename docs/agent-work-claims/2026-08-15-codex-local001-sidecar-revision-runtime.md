# LOCAL-001 warm-cache sidecar revision runtime claim

Status: ACTIVE
Agent: codex-local003-sidecar-revision-20260815
Issue: #1574
Branch: `agent/local003/local001-sidecar-revision-20260815`
Baseline: `7b747ee26cb10b94ffad584dc256eaa19c6b65c8`

## Reserved scope

- Qualify the existing LOCAL-001 licensed BricsCAD V25 warm-cache sidecar revision matrix through `scripts/test-bricscad-v25-sidecar-revision.ps1`.
- Exercise backup appearance, primary byte replacement, and primary removal after cache warm-up.
- Prove fail-closed behavior at the read, bind, existing-mutation, Interchange-confirmation, and Save authority boundaries.
- Prove semantic state and DWG bytes remain unchanged on refusal and that the original sidecar bytes can be restored in the same canonical session.
- Publish only sanitized exact-SHA evidence, the minimum LOCAL_ONLY runner/probe correction if native V25 behavior makes one necessary, and the matching canonical LOCAL-001 handoff/status update.

Expected repository surfaces are limited to:

- `docs/agent-work-claims/2026-08-15-codex-local001-sidecar-revision-runtime.md`
- `scripts/test-bricscad-v25-sidecar-revision.ps1` only if a genuinely local-dependent harness defect is reproduced
- focused static guards for that runner only if the same harness correction requires them
- `docs/LOCAL-AGENT-INBOX.md` and an existing current/local handoff document only for sanitized exact-SHA results

Generated licensed-runtime evidence remains under ignored `artifacts/` or an external local evidence directory.

## Exclusions

- No production persistence, sidecar, Interchange, Save, adapter, or Core source changes.
- No general bug fixing, broad source audit, unrelated documentation cleanup, private DWG/assets, proprietary binaries, secrets, screenshots, or raw machine evidence.
- Any ordinary source defect discovered by this qualification is handed to a non-local agent with the smallest sanitized reproduction; this local worker stops at that bug boundary.
- No GitHub Actions dispatch/re-run/cancel, release, direct `main` commit, force-push, or merge to `main`.
- This bounded matrix does not qualify the full LOCAL-001 surface or customer release.

## Validation plan

1. Fetch and integrate current `origin/main`, then verify this claim commit is pushed and visible on the task branch/draft PR before qualification.
2. Use one clean committed and pushed exact SHA for every static gate, build, and licensed runtime run.
3. Run the focused sidecar revision preflights, PowerShell AST parse, strict manual-CI/handoff guards, full Core smoke, and the V25 x64 Release build with the repository's portable .NET SDK workflow.
4. Start from zero BricsCAD processes and run the warm-cache sidecar revision matrix against a disposable copy of the canonical sample fixture and a fresh external evidence root.
5. Verify exact process cleanup, fixture and source-worktree cleanliness, no committed/generated private artifacts, and restore original sidecar bytes in the canonical session.
6. Publish sanitized result documentation and issue/PR evidence; leave the PR draft and stop before merge.
