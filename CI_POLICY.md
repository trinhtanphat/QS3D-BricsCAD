# GitHub Actions / CI Policy

This file is the repository-level source of truth for GitHub Actions behavior. `docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for who may change `main`.

## Main is read-only for normal agents

Normal AI agents/chat sessions must not push, write, update refs, or merge to `main`. This applies to source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores.

Requests such as `fix bug`, `update code`, `implement all`, `continue all`, `commit push git`, `update docs`, `chore`, `run CI`, or `fix CI` do not grant `main` write/merge permission.

Only a session explicitly authorized by the repository owner to merge/integrate may change `main`, and only for the named PR/batch/task.

## Three-stage CI model

QS3D separates validation from publishing. A branch must not need to land on `main` merely to learn whether remote-safe CI passes.

The approved stages are:

1. **automatic branch/PR validation** — `.github/workflows/ci.yml`;
2. **combined integration validation** — the same `ci.yml` on `integration/**` combined trees and PR merge candidates;
3. **exact-main release** — `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` dispatches `.github/workflows/release-v25-cloud.yml` only after an authorized integration-relevant landing on `main`.

Green branch CI proves only the tested branch/head or PR merge candidate. Green integration CI proves only that frozen combined tree. Neither is permission to merge and neither is a release. Exact-main release CI remains the final cloud evidence for the SHA that actually landed.

Licensed BricsCAD `NETLOAD`, native UI/runtime, private-DWG behavior, signing credentials, and other environment-gated evidence remain `LOCAL_ONLY` / `PENDING_LOCAL` unless actually executed in the required environment.

## Shared automatic branch/PR CI

`.github/workflows/ci.yml` is the single owner-approved automatic non-publishing validation workflow.

It may run automatically for integration-relevant changes on:

- pushes to `agent/**`;
- pushes to `integration/**`;
- pull requests targeting `main`;
- pull requests targeting `integration/**`.

It also remains manually invokable through `workflow_dispatch` for scoped recovery/testing.

Its automatic jobs are remote-safe only and must include the repository CI policy guard, generic source guard, all discovered feature guards, Core Release build, deterministic Core smoke, and the existing package-verifier contract checks. It may parse packaging scripts but must not tag, publish, release, sign, dispatch release workflows, mutate Issues, or write repository contents.

The workflow uses `contents: read`. Adding release/publish credentials or write permissions to this workflow is forbidden unless the owner explicitly changes this policy.

A push run on `agent/**` validates that branch SHA. A `pull_request` run validates GitHub's PR merge candidate against its target branch. An `integration/**` push validates the exact combined integration tree. These are complementary evidence classes; no one run substitutes for all three when the corresponding stage exists.

## Multi-agent integration

For a multi-agent owner request, an explicitly owner-authorized integration coordinator should assemble the participating branches/PRs on:

```text
integration/<batch-id>
```

Before an authorized landing to `main`, the coordinator must:

1. refresh `origin/main`;
2. identify the exact authorized participating Issues/PRs/branches;
3. integrate every required change without silently dropping commits;
4. resolve semantic/API/test conflicts deliberately;
5. verify no required work remains only on an unmerged branch/PR;
6. obtain green **combined-tree CI** on the frozen `integration/**` candidate when integration-relevant watched paths changed;
7. inspect for accidental reversions, duplicate implementations and contract mismatches;
8. freeze and record the integration candidate SHA;
9. merge to `main` only within explicit owner authorization;
10. fetch `main` again and record the exact final SHA.

Do not independently merge every agent PR to `main` merely to assemble the batch unless the owner explicitly requests that strategy.

A branch CI PASS is not sufficient combined-stack evidence when multiple lanes are being assembled; the integration candidate itself must be validated.

## Exact-main automatic V25 cloud CI

The owner-approved dispatcher is `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`.

Its contract is:

- automatic trigger: integration-relevant `push` to `main` only;
- `workflow_dispatch` remains available for operator recovery/testing;
- documentation-only landings outside watched paths do not trigger it;
- `github-actions[bot]` pushes do not execute the dispatch job;
- adjacent integration landings are debounced/cancelled so the newest relevant candidate wins before dispatch;
- it dispatches only `release-v25-cloud.yml` from `main`;
- it pins the triggering exact source SHA;
- it derives the next canonical preview tag in the repository's reserved series;
- it supplies `confirm_release=RELEASE` because this dispatcher is the owner's standing approval for the automatic post-integration preview path;
- the release workflow retains exact-source, source-guard, Core smoke, BricsCAD V25 compile-reference, packaging and release-integrity gates.

The automatic exact-main cloud run does not prove licensed local BricsCAD runtime behavior.

## Documentation/Markdown/chore-only changes

All documentation and Markdown changes still use a branch/PR like every other task.

The shared `ci.yml` intentionally watches canonical CI/governance documents such as `CI_POLICY.md`, `AGENTS.md`, `README.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, and `docs/AGENT-WORK-REGISTRATION.md`, because regressions there can invalidate repository safety contracts.

Other ordinary docs/claim/handoff-only changes need not run heavy branch CI unless they also touch a watched integration-relevant path.

After an authorized `main` merge, documentation-only landings outside the main dispatcher's watched set must not trigger the V25 release path.

**Changed paths are authoritative, not commit-message prefixes.** A commit named `docs:` or `chore:` that modifies `scripts/**`, workflows, build props, solutions, source or tests is integration-relevant.

## Manual workflow authorization

Workflows other than `ci.yml` and the single automatic main dispatcher remain owner-controlled manual lanes.

Release workflows remain manually invokable tools except when `release-v25-cloud.yml` is launched by the approved main dispatcher:

- `.github/workflows/release-v25.yml`
- `.github/workflows/release-v25-cloud.yml`
- `.github/workflows/release-v26.yml`

A normal `continue all`, `fix bug`, `update code`, `commit`, review, docs or handoff assignment does not authorize manually dispatching/re-running/cancelling unrelated release workflows.

Automatic validation authorization does not imply merge authorization. CI authorization does not imply `main` authorization. `main` merge authorization does not imply unrelated manual release authorization.

## Local worker boundary

The local workers (`agent/local002`, `agent/local003`, and successor sessions acting in those roles) remain LOCAL_ONLY by default. They must not treat GitHub Actions failures as their general coding backlog unless the owner assigns that lane. Automatic shared CI may validate a pushed branch without changing this ownership boundary.

A local runtime finding may be handed to a remote/source agent for a repository-safe fix; reproduction location and source-fix ownership are separate.

## Definition of `ALL MERGED TO MAIN`

Report **ALL MERGED TO MAIN** only when an authorized integration reviewer freshly verifies:

- every required Issue/reservation is terminal or explicitly excluded/superseded;
- every required implementation/docs commit is represented in current `main`;
- no required work remains only on an agent branch, local worktree, stash, draft patch or unmerged PR;
- required branch/PR evidence is green for participating lanes where applicable;
- the frozen combined integration tree has green combined-tree CI when applicable;
- exact-main release/cloud validation is green for the landing SHA when required;
- the combined tree has no unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- environment-gated evidence is explicitly handed off rather than falsely reported as PASS;
- the exact current `main` SHA after the authorized landing is recorded.

A branch existing/deleted, Issue state, PR `Merged` UI state, or green CI for an older SHA is not sufficient proof by itself.

## Enforcement

- `scripts/preflight.py` retains broad repository/source checks and its legacy broad automatic-trigger tripwire.
- Automatic event keys for the two approved workflows are deliberately quoted in YAML; `scripts/preflight-ci-manual-only.py` is the strict allowlist that validates their exact trigger scopes, permissions, jobs and publishing boundaries.
- `scripts/preflight-ci-manual-only.py` must reject any third automatic workflow, automatic release workflow, branch validation on direct `main` pushes, broadened publishing permission, or dispatcher target other than `release-v25-cloud.yml`.
- Release workflows must retain explicit `RELEASE` confirmation.
- GitHub branch protection/rulesets should protect `main`, require the intended PR/integration path for normal writers, prevent force-push/deletion, and require stable CI checks once their names are finalized. Track hard enforcement in the repository governance issue.

Related canonical documents: `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/GITHUB-MAIN-PROTECTION.md` when present, and the manual/local qualification runbooks.
