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
