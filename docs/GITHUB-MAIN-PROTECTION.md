# GitHub main protection and CI-recovery addendum

This addendum records the GitHub-settings side of the canonical multi-agent protocol in `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md` and `CI_POLICY.md`.

## No CI direct-main exception

Being the agent/chat session assigned to dispatch, monitor, diagnose, or repair `release-v25-cloud.yml` does **not** authorize implementation directly on `main`.

When V25 cloud CI is red, use this path:

```text
exact failing run/SHA
  -> reserve non-overlapping repair lane
  -> recovery/<agent>/<scope> or agent/<agent>/<scope>
  -> deterministic regression/guard
  -> integration/<batch-id> or dedicated recovery integration
  -> reviewed final landing to main
  -> fresh current-main V25 cloud CI
  -> repeat from newest relevant failure until green
```

Do not change a fixture/expectation merely to match an unexpected production result without proving the fixture is wrong. Do not re-use a green run from an older tree as evidence for newer `main`.

## Latest-main / latest-CI recovery loop

Treat V25 recovery as a monotonic loop that always converges on the newest `main`, not on a historical failed SHA.

1. After any human/agent merge, integration landing, source update, test update, script update, workflow update, packaging update or other repository update reaches `main`, refresh the current `main` HEAD and require a fresh `release-v25-cloud.yml` qualification for that newest state.
2. Read the newest V25 cloud run together with the newest `main` commit. The newest run is diagnostic evidence; it is final release evidence only when it qualifies the newest relevant `main` state/release tree.
3. If the newest run failed on an older dispatch SHA because `main` moved, keep the stale-dispatch/concurrency guard intact. Do not weaken or bypass the guard. Start a fresh run from the newest `main` instead.
4. If the newest run exposes a real source/test/preflight/build/package failure, reproduce or verify that failure against the newest `main` before patching. If it is still present, reserve the repair lane, fix it branch-first, verify it, integrate it, land it to `main`, then start the next fresh V25 run.
5. Repeat `latest main -> latest V25 run -> diagnose -> fix/integrate -> fresh run` until the newest relevant V25 run is green and no newer implementation landing has invalidated it.
6. Never create a no-op implementation commit merely to obtain a new SHA. A real landing/update already creates the next qualification point.

In compact form:

```text
latest main HEAD
  -> latest relevant V25 run
  -> SUCCESS on current release tree? -> done
  -> stale because main moved? -> dispatch current HEAD
  -> real failure? -> diagnose on current HEAD
  -> claim -> agent/recovery branch -> verify -> integration -> main
  -> fresh V25 run
  -> repeat until green
```

A release workflow may create its own `chore(release): prepare ...` commit as part of preparing the exact release source. That workflow-owned release-preparation commit is part of the same qualification transaction and must not recursively dispatch an infinite chain of release runs by itself. Any independent human/agent landing that advances `main` during the run still invalidates the stale dispatch and requires a fresh run from the newest HEAD.

This rule intentionally prefers a new current-HEAD run over re-running an old failed SHA. Old runs remain useful for diagnosis, but current `main` plus the newest relevant V25 run are the source of truth for recovery.

## Main branch protection target

Repository policy should be backed by GitHub branch protection/rulesets so accidental direct implementation pushes cannot bypass the integration protocol. The intended protection is:

- protect `main` from force-push and deletion;
- require the intended PR/integration path for normal implementation landings;
- require appropriate status checks when stable check names are available;
- keep administrator/owner bypass narrow and deliberate;
- do not treat bypass as permission for ordinary agents to land implementation directly.

The repository files cannot configure GitHub account/repository rulesets by themselves. Until hard protection is enabled, agents must follow the repository policy contract voluntarily and preflight guards must remain fail-closed.

## Claim publication under hard protection

The current canonical protocol intentionally permits a claim-only Markdown reservation to become visible on `main` before implementation. If GitHub is configured to require PRs for every update to `main`, publish the claim through a tiny `claim/<agent>/<scope>` PR instead. The visibility requirement remains the same: implementation must not begin until the claim is actually reachable from current `main`.

This is a coordination exception only; source, tests, scripts, workflows, packaging and release implementation remain branch-first.

## Final-state rule

`ALL MERGED TO MAIN` means the current combined tree has been freshly reviewed for claim completion, commit/tree reachability, missing off-main work, accidental reversions, duplicate implementations and semantic/API/test conflicts. Only then should final exact-tree CI be treated as release evidence.
