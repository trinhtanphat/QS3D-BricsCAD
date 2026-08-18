# GitHub Actions / CI Policy

This file is the repository-level source of truth for GitHub Actions behavior. `docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for who may change `main`.

## Main is read-only for normal agents

Normal AI agents/chat sessions must not push, write, update refs, or merge to `main`. This applies to source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores.

Requests such as `fix bug`, `update code`, `implement all`, `continue all`, `commit push git`, `update docs`, `chore`, `run CI`, or `fix CI` do not grant `main` write/merge permission.

Only a session explicitly authorized by the repository owner to merge/integrate may change `main`, and only for the named PR/batch/task.

## Protected-main hard enforcement

The repository has an active GitHub repository ruleset named `protectedMain` (ruleset `20890901`) targeting the default branch (`main`). This is hard enforcement in addition to the repository Markdown policy.

The verified protected-main contract is:

- require a pull request before `main` can be updated;
- require successful status checks named `preflight` and `core`;
- use strict required-status freshness, so an out-of-date merge candidate must be refreshed/revalidated before merge;
- block deletion of `main`;
- block non-fast-forward / force-push updates to `main`;
- keep the ruleset bypass list empty unless the repository owner explicitly changes that governance decision.

The GitHub ruleset is an external repository setting rather than committed source. Agents must not infer protection from Markdown alone: when protection state matters, verify the effective GitHub rules for `main`. If the ruleset is missing, disabled, no longer targets `main`, loses the required checks, or gains an unexpected bypass, treat that as a governance defect and do not claim hard protection is active.

Hard protection does not grant an agent permission to merge. It only constrains what GitHub will accept. `docs/MAIN-WRITE-AUTHORIZATION.md` still controls which session may perform an authorized merge.

## Three-stage CI model

QS3D separates validation from publishing. A branch must not need to land on `main` merely to learn whether remote-safe CI passes.

The approved stages are:

1. **automatic branch/PR validation** — `.github/workflows/ci.yml`; for watched human/AI task branches, the branch-push run must be green before a new PR is opened;
2. **combined integration validation** — the same `ci.yml` on PR merge candidates and `integration/**` combined trees when applicable;
3. **exact-main release** — `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` dispatches `.github/workflows/release-v25-cloud.yml` only after an authorized integration-relevant landing on `main`.

Green branch CI proves only the tested branch SHA. Green PR CI proves only the tested PR merge candidate. Green integration CI proves only that frozen combined tree. None of those results is permission to merge and none is a release. Exact-main release CI remains the final cloud evidence for the SHA that actually landed.

Licensed BricsCAD `NETLOAD`, native UI/runtime, private-DWG behavior, signing credentials, and other environment-gated evidence remain `LOCAL_ONLY` / `PENDING_LOCAL` unless actually executed in the required environment.

## Mandatory branch-CI-before-PR gate

For every human/AI task that changes a path watched by shared CI, the task branch must obtain a terminal green automatic branch-push CI run on the exact current branch SHA **before a new PR is opened**.

The required sequence is:

```text
implement on agent/**
  -> commit/push task branch
  -> automatic shared branch CI on exact branch SHA
  -> fix on the branch until terminal SUCCESS
  -> refresh main and verify evidence is still fresh
  -> only then open the PR
```

A PR must not be used as the first CI attempt for watched human/AI work. Do not open a draft PR merely to obtain the first CI run. If branch CI fails, fix the failure on the task branch and obtain a new green run before PR creation.

This rule avoids filling the PR queue with candidates that have never passed their own isolated validation. It does **not** mean that two independent full CI passes are always logically required for an unchanged tree. After PR creation, GitHub's protected-main ruleset controls the merge gate. If `main` moves, the branch is reconciled, or GitHub produces a different/fresher merge candidate, the applicable required checks must be fresh again before merge.

For ordinary documentation/claim/handoff-only paths that are intentionally outside the shared CI watch set, the repository may omit heavy pre-PR branch CI. Those changes still require a branch and PR and must satisfy whatever protected-main required checks GitHub applies at merge time. Never bypass or weaken protection merely to land an unwatched docs-only change.

### Narrow maintenance-bot exception

GitHub Dependabot is the only standing exception to branch-CI-before-PR. Dependabot is allowed to create dependency-update PRs directly from the configuration in `.github/dependabot.yml` because GitHub owns the bot branch/PR lifecycle and there is no meaningful pre-PR reservation step for that generated work.

This exception is deliberately narrow:

- it applies only to PRs authored by the GitHub Dependabot service from the repository's committed Dependabot configuration;
- it does **not** authorize Dependabot to merge, enable auto-merge, bypass protection, write `main`, publish a release, or dispatch a release workflow;
- every Dependabot PR must still produce and pass the protected-main `preflight` and `core` contexts on the current merge candidate before any authorized merge;
- dependency PRs that touch source/build/workflow inputs receive the same source/build/V25 validation tier as equivalent human changes;
- a human or AI agent must not use or imitate this exception to skip normal task registration or branch CI.

Repository-wide blind auto-merge remains intentionally disabled. A green PR is merge-eligible evidence, not merge authorization.

## Shared automatic branch/PR CI

`.github/workflows/ci.yml` is the single owner-approved automatic non-publishing validation workflow.

It may run automatically on:

- watched pushes to `agent/**`;
- watched pushes to `integration/**`;
- **every** pull request targeting `main`, so the stable required `preflight` and `core` contexts can never disappear because of a path filter;
- **every** pull request targeting `integration/**` for the same reason.

It also remains manually invokable through `workflow_dispatch` for scoped recovery/testing.

The workflow is intentionally tiered by changed-path impact while preserving the same two required status contexts:

1. **repository-metadata tier** — PR templates, issue forms, security/contribution metadata and ordinary docs receive the repository-professionalism/policy boundary and a lightweight `core` success without redundant Core/V25 compilation;
2. **policy/source-guard tier** — canonical governance files such as `AGENTS.md`, `CI_POLICY.md`, `README.md`, and the main-write/agent-registration policies also run generic/source/feature guards, but skip Core/V25 build when no build-relevant input changed;
3. **full build tier** — `src/**`, `tests/**`, `scripts/**`, `samples/generated/**`, `.github/workflows/**`, build props and solution files run all source guards, package-verifier checks, deterministic Core build/smoke, trusted pinned BricsCAD V25 reference acquisition and V25 plugin compilation.

Branch pushes remain path-bounded to avoid wasting CI on ordinary unreviewed docs. Pull requests deliberately have no `paths`/`paths-ignore` filter because protected `main` requires stable `preflight` and `core` contexts for every legitimate PR.

The workflow uses `contents: read`; validation checkouts set `persist-credentials: false`. Adding release/publish credentials or write permissions to this workflow is forbidden unless the owner explicitly changes that policy.

The workflow must not tag, publish, release, sign, dispatch release workflows, mutate Issues, merge PRs, or write repository contents. Repository policy guards must reject autonomous PR-to-main merge primitives in committed workflows.

A push run on `agent/**` validates that branch SHA. A `pull_request` run validates GitHub's PR merge candidate against its target branch. An `integration/**` push validates the exact combined integration tree. These are complementary evidence classes. Branch CI is the mandatory pre-PR gate for watched human/AI task branches; PR/integration checks are freshness and compatibility evidence for the candidate GitHub may actually merge.

The protected-main ruleset currently requires stable contexts `preflight` and `core`. Renaming or removing those job contexts, or changing workflow event behavior so the required contexts can no longer be produced for legitimate protected-main PRs, is a protection-compatibility change and must be reviewed as governance-sensitive work.

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
9. merge to `main` only within explicit owner authorization and only when GitHub protected-main requirements are satisfied;
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
- release preparation must remain workspace-only and must never commit/push a version-preparation change back to protected `main`;
- the release workflow retains exact-source, source-guard, Core smoke, BricsCAD V25 compile-reference, packaging and release-integrity gates.

The automatic exact-main cloud run does not prove licensed local BricsCAD runtime behavior.

## Documentation/Markdown/chore-only changes

All documentation and Markdown changes still use a branch/PR like every other human/AI task.

The shared `ci.yml` intentionally watches canonical CI/governance documents such as `CI_POLICY.md`, `AGENTS.md`, `README.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, and `docs/AGENT-WORK-REGISTRATION.md`, because regressions there can invalidate repository safety contracts. For those watched governance documents, human/AI branch CI must be green before the PR is opened.

Other ordinary docs/claim/handoff-only changes may skip heavy pre-PR branch CI when they are outside the shared workflow's push watch set. Their PR still receives stable required `preflight` and `core` contexts through the lightweight tier, and they must satisfy effective protected-main merge rules.

After an authorized `main` merge, documentation-only landings outside the main dispatcher's watched set must not trigger the V25 release path.

**Changed paths are authoritative, not commit-message prefixes.** A commit named `docs:` or `chore:` that modifies `scripts/**`, workflows, build props, solutions, source, tests or synthetic generated fixtures is integration-relevant.

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
- current `main` still reports the intended effective protected-main rules or any deliberate owner-approved replacement;
- exact-main release/cloud validation is green for the landing SHA when required;
- the combined tree has no unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- environment-gated evidence is explicitly handed off rather than falsely reported as PASS;
- the exact current `main` SHA after the authorized landing is recorded.

A branch existing/deleted, Issue state, PR `Merged` UI state, or green CI for an older SHA is not sufficient proof by itself.

## Enforcement

- GitHub ruleset `protectedMain` (`20890901`) is the current hard-enforcement layer for the default branch and must target `main` effectively.
- The ruleset must require a PR, stable status checks `preflight` and `core`, strict freshness, deletion protection and non-fast-forward/force-push protection unless the repository owner explicitly changes the governance contract.
- The ruleset bypass list is expected to remain empty unless the owner explicitly approves a narrow exception.
- `scripts/preflight.py` retains broad repository/source checks and its legacy broad automatic-trigger tripwire.
- Automatic event keys for the two approved workflows are deliberately quoted in YAML; `scripts/preflight-ci-manual-only.py` is the strict allowlist that validates their exact trigger scopes, permissions, jobs and publishing boundaries.
- `scripts/preflight-ci-manual-only.py` must reject any third automatic workflow, automatic release workflow, branch validation on direct `main` pushes, broadened publishing permission, PR path filters that can suppress required contexts, or dispatcher target other than `release-v25-cloud.yml`.
- `scripts/preflight-repository-professionalism.py` must retain the professional repository surfaces, bounded maintenance configuration, stable required PR contexts and prohibition on autonomous PR-to-main merging.
- Dependabot is the only standing branch-CI-before-PR exception and must never gain autonomous merge/release permission through committed configuration.
- Release workflows must retain explicit `RELEASE` confirmation.
- Markdown policy and GitHub hard protection are complementary. Do not claim the repository is hard-protected solely because the Markdown says so; verify the effective GitHub rules when that claim matters.

Related canonical documents: `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/GITHUB-MAIN-PROTECTION.md` when present, and the manual/local qualification runbooks.
