# Work Claim — Core smoke coordination and triage

- Agent: `chatgpt-web-gpt56sol`
- Started: `2026-08-14 07:08 +07:00`
- Status: `ACTIVE`
- Baseline observed before claim: `main` at `2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd` (must be refreshed after this claim lands)

## Scope

1. Record the current multi-agent coordination/blocker state in Markdown so other agents do not duplicate work.
2. Strengthen the claim-first rule for scope expansion: an agent must land and verify a follow-up claim-amendment commit on `origin/main` before touching any newly discovered source/test paths outside its reserved scope.
3. Triage the latest relevant `Run deterministic Core smoke` failure from GitHub Actions, obtain the exact exception/error text, and determine whether a remote-safe unclaimed source/test lane exists.
4. If a source/test fix is required, **do not edit it under this initial claim**. First amend this claim in a separate commit with the exact paths/symbols and verify that amendment on current `origin/main`; only then patch/test/commit/push.

## Initially reserved paths

- `docs/agent-work-claims/2026-08-14-0708-chatgpt-web-gpt56sol-core-smoke-coordination.md`
- `docs/AGENT-WORK-REGISTRATION.md`
- one new 2026-08-14 coordination/handoff Markdown file under `docs/`

Source/test paths are intentionally **not reserved yet**. They will be added by a claim-amendment commit only after exact CI evidence identifies the failing lane and collision checks show it is free.

## Collision rules for this claim

- Do not enter any source lane already marked `ACTIVE`/`BLOCKED` by another agent.
- In particular, re-check current ownership before touching Source Reconcile/#1005 or any LOCAL_ONLY BricsCAD-runtime lane; historical status is not sufficient.
- If `main` advances during investigation, refresh before any claim amendment or write.
- No speculative/no-op source commit and no CI-gate weakening merely to obtain green status.

## Evidence baseline

- `docs/AGENT-WORK-REGISTRATION.md` already requires a claim-only commit on `origin/main` before code diagnosis/editing/testing.
- Recent history showed heavy concurrent movement of `main`; the latest observed commit before this claim was a LOCAL-003 postflight documentation commit.
- An older V25 baseline had failed at `Run deterministic Core smoke`, but the exact current failure must be re-derived from a relevant latest run before any source fix.

## Completion criteria

- Coordination/handoff Markdown committed and pushed to `main`.
- Exact current/relevant Core smoke failure captured, or documented as stale/no-longer-reproducible from available CI evidence.
- Any required source/test lane separately claimed by amendment before edits.
- Final claim status changed to `DONE` or `BLOCKED` with exact evidence and commit SHAs.
