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

1. **automatic branch/PR validation** — `.github/workflows/ci.yml`; every push to `agent/**` and `integration/**` produces an exact-head branch run for early isolated validation, while every PR receives protected current-candidate validation; branch-run completion timing does not poison or permanently invalidate the canonical PR;
2. **combined integration validation** — the same `ci.yml` on PR merge candidates and `integration/**` combined trees when applicable;
3. **exact-main release** — `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` dispatches `.github/workflows/release-v25-cloud.yml` only after an authorized integration-relevant landing on `main`.

Green branch CI proves only the tested branch SHA. Green PR CI proves only the tested PR merge candidate. Green integration CI proves only that frozen combined tree. None of those results is permission to merge and none is a release. Exact-main release CI remains the final cloud evidence for the SHA that actually landed.

Licensed BricsCAD `NETLOAD`, native UI/runtime, private-DWG behavior, signing credentials, and other environment-gated evidence remain `LOCAL_ONLY` / `PENDING_LOCAL` unless actually executed in the required environment.

## Automatic branch CI and canonical PR lifecycle

Every human/AI task branch under `agent/**` or `integration/**` receives automatic branch-push CI on the exact pushed SHA. Branch CI is the early isolated validation layer and agents should inspect and remediate a red exact-head branch run when it is observable.

The preferred low-churn sequence is:

```text
implement on agent/** or integration/**
  -> commit/push canonical branch
  -> automatic shared branch CI starts on exact branch SHA
  -> fix any observed red branch-CI defect on the same branch
  -> open/continue the one canonical PR when the task is ready for protected review
  -> protected current-candidate preflight + core
  -> refresh/reconcile current main when required
  -> fresh protected candidate checks
  -> merge only when current/green/mergeable
```

Automatic branch CI remains valuable evidence, but **its completion timestamp is not a permanent PR-admission identity**. A canonical PR may already exist while the matching branch run is queued, running, or completes later. Do not close/recreate or supersede a correct PR merely to make a branch-run completion timestamp precede PR creation.

A PR must not be used to hide or ignore a known red branch failure. If branch CI is red on the current canonical branch, inspect the exact failure and remediate the same branch. The existing PR remains the canonical review/merge carrier and its synchronized protected checks validate the resulting current candidate.

The authoritative merge gate is GitHub's protected-main current PR candidate: required `preflight` and `core` must be terminal `SUCCESS`, strict freshness must be satisfied, the PR must be mergeable/collision-clean, and the expected-head SHA guard must match immediately before merge. A stale green branch run or stale green PR candidate is never sufficient.

Every push to `agent/**` and `integration/**` is intentionally eligible for shared CI even when the pushed commit is docs-only or ancestry-only. This is required because a safe reconciliation can create a new exact head SHA with no watched first-parent file delta. The workflow's internal diff classifier, not the event trigger, decides whether source guards and the full Core/V25 build are needed. Ordinary documentation/claim/handoff-only candidates therefore receive lightweight validation rather than no branch run.

`docs/PR-CI-LIFECYCLE.md` records the owner-approved timing correction in more detail. Older wording that treats branch-CI completion-before-PR-creation as a permanent admission requirement is superseded by this section. Branch CI remains automatic and actionable; protected current-candidate PR checks remain mandatory before merge.

### Dependabot generated-PR boundary

GitHub Dependabot may create dependency-update PRs directly from the configuration in `.github/dependabot.yml` because GitHub owns that generated branch/PR lifecycle.

This boundary is deliberately narrow:

- it applies only to PRs authored by the GitHub Dependabot service from the repository's committed Dependabot configuration;
- it does **not** authorize Dependabot to merge, enable auto-merge, bypass protection, write `main`, publish a release, or dispatch a release workflow;
- every Dependabot PR must still produce and pass the protected-main `preflight` and `core` contexts on the current merge candidate before any authorized merge;
- dependency PRs that touch source/build/workflow inputs receive the same source/build/V25 validation tier as equivalent human changes;
- a human or AI agent must not imitate Dependabot to bypass normal task registration, Lane-Key ownership, or protected PR validation.

Repository-wide blind auto-merge remains intentionally disabled. A green PR is merge-eligible evidence, not merge authorization.

## Shared automatic branch/PR CI

`.github/workflows/ci.yml` is the single owner-approved automatic non-publishing validation workflow.

It may run automatically on:

- **every** push to `agent/**`, including docs-only and ancestry-only reconciliation heads;
- **every** push to `integration/**`, including ancestry-only combined-tree reconciliation heads;
- **every** pull request targeting `main`, so the stable required `preflight` and `core` contexts can never disappear because of a path filter;
- **every** pull request targeting `integration/**` for the same reason.

It also remains manually invokable through `workflow_dispatch` for scoped recovery/testing.

The workflow is intentionally tiered by changed-path impact while preserving the same two required status contexts:

1. **repository-metadata tier** — PR templates, issue forms, security/contribution metadata and ordinary docs receive the repository-professionalism/policy boundary and a lightweight `core` success without redundant Core/V25 compilation;
2. **policy/source-guard tier** — canonical governance files such as `AGENTS.md`, `CI_POLICY.md`, `README.md`, and the main-write/agent-registration policies also run generic/source/feature guards, but skip Core/V25 build when no build-relevant input changed;
3. **full build tier** — `src/**`, `tests/**`, `scripts/**`, `samples/generated/**`, `.github/workflows/**`, build props and solution files run all source guards, package-verifier checks, deterministic Core build/smoke, trusted pinned BricsCAD V25 reference acquisition and V25 plugin compilation.

Branch pushes deliberately have no `paths`/`paths-ignore` filter. The workflow compares the full candidate against `main` after checkout and uses that internal scope classification to avoid redundant heavy builds. This guarantees that a new exact branch SHA produced by docs-only or zero-tree/ancestry-only reconciliation still receives automatic CI evidence. Pull requests likewise have no path filter because protected `main` requires stable `preflight` and `core` contexts for every legitimate PR.

The workflow uses `contents: read`; validation checkouts set `persist-credentials: false`. Adding release/publish credentials or write permissions to this workflow is forbidden unless the owner explicitly changes this policy.

The workflow must not tag, publish, release, sign, dispatch release workflows, mutate Issues, merge PRs, or write repository contents. Repository policy guards must reject autonomous PR-to-main merge primitives in committed workflows.

A push run on `agent/**` validates that branch SHA. A `pull_request` run validates GitHub's PR merge candidate against its target branch. An `integration/**` push validates the exact combined integration tree. These are complementary evidence classes. Branch CI provides early isolated defect evidence; a known red branch run must be remediated on the same canonical branch. Protected current-candidate `preflight` and `core` are the mandatory merge gate, and branch/PR timing order alone must not force a replacement PR.

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

The shared `ci.yml` now runs automatically for every push to `agent/**` and `integration/**`, including ordinary docs/claim/handoff-only commits. Its internal scope classifier keeps those candidates lightweight: ordinary docs receive repository/policy validation and a lightweight `core` success without redundant Core/V25 compilation, while canonical governance files can additionally enable source guards when appropriate.

This all-push branch trigger is intentionally separate from the exact-main release dispatcher. After an authorized `main` merge, documentation-only landings outside the dispatcher's watched set must still not trigger the V25 release path.

**Changed paths are authoritative for validation tier and release-dispatch eligibility, not commit-message prefixes.** A commit named `docs:`, `chore:` or `md:` that modifies `scripts/**`, workflow files, solution files, build props, production source, tests, or synthetic generated fixtures is integration-relevant.

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
- `scripts/preflight-ci-manual-only.py` must reject any third automatic workflow, automatic release workflow, branch validation on direct `main` pushes, branch-push `paths`/`paths-ignore` filters that can suppress exact-head reconciliation evidence, broadened publishing permission, PR path filters that can suppress required contexts, or dispatcher target other than `release-v25-cloud.yml`.
- `scripts/preflight-repository-professionalism.py` must retain the professional repository surfaces, bounded maintenance configuration, stable required PR contexts and prohibition on autonomous PR-to-main merging.
- Dependabot's generated branch/PR lifecycle is a narrow repository-maintenance boundary and must never gain autonomous merge/release permission through committed configuration.
- Release workflows must retain explicit `RELEASE` confirmation.
- Markdown policy and GitHub hard protection are complementary. Do not claim the repository is hard-protected solely because the Markdown says so; verify the effective GitHub rules when that claim matters.

Related canonical documents: `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/PR-CI-LIFECYCLE.md`, `docs/GITHUB-MAIN-PROTECTION.md` when present, and the manual/local qualification runbooks.
