# Agent Collaboration Policy

This repository is expected to have multiple agents working concurrently. Every agent must protect other agents' work and choose tasks that match its actual execution environment.

## Mandatory handoff reading order

Before starting substantive work, read in this order:

1. `AGENTS.md` (this file);
2. `CI_POLICY.md`;
3. fetch the latest `main`;
4. `docs/AGENT-HANDOFF-LATEST-2026-08-10.md` — **canonical current source/status handoff**;
5. `docs/IMPLEMENTATION-STATUS.md`;
6. `docs/PLAN.md` and `docs/COMMANDS.md`;
7. `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md` only when deeper session chronology, old branch/gate history, screenshot requirements or early implementation evidence is needed.

The session-history handoff is intentionally retained as an audit trail, but it contains historical source-status statements that can become stale as `main` evolves. When it conflicts with the latest handoff or current source, current `main` wins.

## Mandatory sync discipline

Before starting a code change:

1. refresh/fetch the latest `main`;
2. inspect recent commits and changed files relevant to the task;
3. base the work on the current branch head, not on an older snapshot from the beginning of the conversation/session.

Before every commit/push to `main`:

1. refresh the current `main` again;
2. verify whether another agent has pushed since the last sync;
3. if `main` moved, rebase/reapply/merge the intended patch onto the latest head without discarding newer work;
4. review the final diff so the commit contains only the intended changes.

For longer tasks, repeat this sync periodically instead of waiting until the end. Assume another agent can commit at any time.

Never force-push over concurrent work, reset `main` backwards, or silently revert another agent's changes unless the repository owner explicitly requests that exact operation.

## Divide work by execution capability

### Agents with local-machine access

If an agent has permission and tooling to operate a real/local machine, that agent should prioritize work that genuinely requires that local environment, especially:

- BricsCAD V25 installation/runtime access;
- real `NETLOAD` / DemandLoad and interactive plugin validation;
- Windows desktop/UI interaction and screenshots;
- local licensed/proprietary dependencies that cannot be stored in GitHub;
- private DWG fixtures or files that exist only on the local machine;
- runner registration, environment variables, installed SDK/runtime inspection;
- reproducing machine-specific crashes, DPI/layout issues, file-lock behavior, or native CAD behavior.

Do not spend scarce local-machine access on ordinary repository editing, documentation cleanup, broad source review, or other tasks that remote agents can perform equally well unless those tasks directly unblock local validation.

### Remote / hybrid online agents

Remote or hybrid agents should handle work that does not require the real local BricsCAD machine, including:

- GitHub source review and implementation;
- core/domain/persistence/reporting/test code;
- static analysis and code-quality fixes;
- Markdown/documentation/planning;
- workflow/policy review without dispatching Actions;
- Git history inspection and multi-agent integration;
- preparing scripts, tests, patches, and runtime probes for a local agent to execute later.

Remote agents must not claim local BricsCAD runtime verification merely because source/static/Core checks look correct.

## Handoff rule

When a remote agent reaches a task that requires local-only access, leave the repository in a runnable/testable state and document the exact local validation needed. When a local agent finishes that validation, commit only reusable source/scripts/docs/evidence that are safe for the repository; never commit proprietary BricsCAD DLLs or private fixtures.

When adding major source capability, update `docs/AGENT-HANDOFF-LATEST-2026-08-10.md` or create a newer canonical handoff and update this reading-order pointer. Do not make agents infer current status from an old session transcript alone.

## GitHub Actions

Follow `CI_POLICY.md`: GitHub Actions are manual-only and may run only when the repository owner explicitly requests them. Multi-agent activity, source changes, documentation changes, commits, pushes, merges, reviews, or handoffs are not implicit permission to run CI.
