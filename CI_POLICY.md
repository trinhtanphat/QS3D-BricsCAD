# GitHub Actions / CI Policy

This file is the repository-level source of truth for when GitHub Actions may run and how multi-agent work is integrated before final CI.

## Default policy: manual-only, with one owner-approved post-integration exception

GitHub Actions remain **manual-only by default**. The only automatic trigger approved by the repository owner is:

- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`

That dispatcher may run on an integration-relevant `push` to `main` and may dispatch exactly:

- `.github/workflows/release-v25-cloud.yml`

All other workflows remain `workflow_dispatch`-only unless the owner explicitly changes this policy again.

The automatic dispatcher is intentionally narrow. It exists to validate the single combined `main` landing after a multi-agent batch has been integrated. It is not permission for agents to run arbitrary CI, publish unrelated releases, or add more automatic triggers.

## Canonical multi-agent landing model

For implementation work, agents must **not independently land source/test/script changes directly onto `main`**.

The canonical model is:

1. publish the required claim-only Markdown reservation to `origin/main` so every agent can see lane ownership;
2. create or use a dedicated implementation branch, normally `agent/<agent-id>/<scope>`;
3. implement, test and commit the reserved source work on that branch;
4. keep the claim `ACTIVE` while the implementation is not yet integrated;
5. when the participating lanes are ready, merge/cherry-pick/rebase those implementation branches into one shared batch branch, normally `integration/<batch-id>`;
6. resolve conflicts and run remote-safe preflights/tests against that **combined integration branch**, not only against each agent branch in isolation;
7. perform one final integration review;
8. merge the integration branch into `main` **once**;
9. after that one integration-relevant landing reaches `main`, the automatic dispatcher starts the V25 cloud CI/release workflow for current `main`.

Claim/status documentation commits may still be pushed directly to `main`; the automatic dispatcher ignores documentation-only landings by path filter. Release-preparation commits pushed by `github-actions[bot]` are also ignored so the release workflow cannot recursively trigger itself.

This section supersedes older repository wording that told implementation agents to push their completed source batch directly to `main`. Claim publication still uses `main`; implementation landing now uses agent branches plus a single integration branch.

## Why the integration branch exists

Merging every agent PR separately into `main` would cause repeated final-CI runs and would test intermediate trees where only part of the owner request is integrated. The repository owner instead wants one combined landing.

Therefore:

- agent implementation branches are staging inputs, not final release candidates;
- the integration branch is the combined candidate;
- `main` receives one final integration landing for the batch;
- the automatic V25 cloud CI is evidence for the combined landing, not for a partially merged sequence.

If `main` changes again with integration-relevant source after that landing, the new current tree is a new candidate and another automatic run is expected. A green workflow for an older SHA does not prove a newer `main` SHA.

## Definition of `ALL MERGED TO MAIN`

For a specific owner request, agents may report **ALL MERGED TO MAIN** only when an integration reviewer has freshly verified all of the following:

- every participating required claim is terminal or explicitly excluded from the batch;
- every required implementation change is present in the integration result and then reachable from current `main`;
- no required code exists only on an agent branch, local worktree, draft patch or unmerged PR;
- the final combined tree has no unresolved merge markers, accidental reversions, duplicate competing implementations, or known semantic/API/test collisions;
- remote-safe build/tests/smoke/preflights for the combined tree have passed, or any environment-gated evidence is explicitly handed off;
- the exact current `main` SHA after the single integration landing is recorded.

A branch existing or being deleted is not proof of integration. A PR showing `Merged` is not enough by itself. Commit/tree reachability and the combined current `main` tree are authoritative.

## Automatic post-integration V25 cloud CI

The owner-approved automatic dispatcher is `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`.

Its contract is:

- automatic trigger: integration-relevant `push` to `main` only;
- manual `workflow_dispatch` remains available for operator recovery/testing;
- documentation-only claim/handoff updates do not trigger it;
- `github-actions[bot]` pushes do not execute the dispatch job;
- concurrent adjacent integration landings are debounced/cancelled so the newest batch wins before dispatch;
- it dispatches only `release-v25-cloud.yml` from `main`;
- it generates a preview tag in the reserved automatic range starting at `v0.1.0-preview.10001` and skips an already-existing tag;
- it passes `confirm_release=RELEASE` because this automatic path is itself the repository owner's standing approval for the post-integration V25 cloud preview release;
- `release-v25-cloud.yml` keeps its own exact-source, source-guard, Core smoke, BricsCAD V25 compile-reference, packaging and release-integrity gates.

The automatic cloud run does **not** prove licensed local BricsCAD `NETLOAD`, native UI/runtime, private-DWG behavior, signing credentials, or other `LOCAL_ONLY` gates. Those evidence classes remain separate.

## Manual workflows remain manual

Except for the single dispatcher above, workflows under `.github/workflows/` remain owner-controlled `workflow_dispatch` lanes. In particular, the following release workflows remain manually invoked release tools:

- `.github/workflows/release-v25.yml`;
- `.github/workflows/release-v25-cloud.yml` itself;
- `.github/workflows/release-v26.yml`.

`release-v25-cloud.yml` is automatically started only **through the approved post-integration dispatcher**. It must retain explicit `confirm_release=RELEASE`, exact-source preparation and its release-integrity guards.

Do not add `push`, `pull_request`, `pull_request_target`, `schedule`, `workflow_run`, `repository_dispatch`, release/deployment events, or other automatic triggers to any other workflow without another explicit owner policy change.

## Agent execution roles and CI authorization

Normal coding agents concentrate on finding/fixing bugs, updating source, adding deterministic regressions/static guards, reviewing diffs and committing coherent implementation work to their implementation branches.

- Claim publication to `main` does not authorize arbitrary Actions operations.
- A normal `continue all`, `fix bug`, `update code`, `commit`, review or handoff assignment does not authorize manually dispatching/re-running/cancelling unrelated workflows.
- The automatic post-integration dispatcher requires no per-run agent approval after a valid integration landing; it is standing owner policy.
- Manual CI operations outside that automatic path still require explicit owner authorization and remain agent/scope-specific.
- The local workers (`agent/local002`, `agent/local003`, and successor sessions acting in those roles) remain LOCAL_ONLY by default and must not treat GitHub Actions failures as their general coding backlog unless the owner separately assigns that exact work.

Coding agents and CI-designated agents may work concurrently. A red cloud workflow should be diagnosed/fixed by the appropriate remote/source agent unless the failure genuinely requires a LOCAL_ONLY environment.

## Integration freeze before the single main landing

Before merging `integration/<batch-id>` into `main`, the integration reviewer must establish a final integration freeze:

1. identify the owner request/batch and the participating claims;
2. stop participating agents from adding more source changes to that batch candidate;
3. verify all required agent branches/PRs are integrated into the integration branch or explicitly excluded/superseded;
4. verify every required implementation commit is represented in the combined integration tree;
5. run the relevant remote-safe preflights/tests on that combined tree;
6. inspect the combined diff for semantic conflicts, duplicate implementations and accidental reversions;
7. record the integration branch candidate SHA;
8. merge the integration branch to `main` once;
9. refresh `main` and record the resulting exact final SHA;
10. let the automatic dispatcher run `release-v25-cloud.yml` for the current integrated tree.

Canonical state progression:

```text
AGENTS_WORKING
    -> AGENT_BRANCHES_READY
    -> INTEGRATION_BRANCH
    -> INTEGRATION_REVIEW
    -> ONE_FINAL_MERGE_TO_MAIN
    -> ALL_MERGED_TO_MAIN
    -> AUTO_V25_CLOUD_CI
    -> CI_GREEN
    -> ALL_DONE
```

If integration-relevant `main` changes after CI starts, the old run remains evidence only for its own source/release commit. The newest current tree requires new current-head evidence.

## Manual build/release sequence outside the automatic path

When the owner explicitly requests another manual release lane:

1. resolve the exact candidate commit/tag;
2. choose the requested host-major workflow;
3. dispatch manually with the required inputs/confirmation;
4. run repository preflights and deterministic Core smoke tests;
5. compile the matching host adapter against the required BricsCAD environment/references;
6. run the host-major runtime/signing gates required by that release type;
7. package only the matching host-major assets;
8. publish only after release-integrity checks succeed.

See `docs/MANUAL-BUILD-RELEASE.md` and `docs/MANUAL-BUILD-RELEASE-V26.md` for operator details.

## Local/static validation

Repository-local/static validation may be run before integration without starting GitHub Actions. Passing static review is not equivalent to licensed BricsCAD runtime evidence.

V25 and V26 runtime proof are independent. A V25 PASS cannot be reported as V26 evidence, and vice versa.

## Enforcement

- `scripts/preflight.py` retains the broad repository/source policy and legacy workflow safety checks.
- `scripts/preflight-ci-manual-only.py` is the strict Actions-policy gate. Despite its historical filename, it now enforces **manual-only by default plus exactly one approved automatic post-integration dispatcher**.
- That strict gate must reject any second automatic workflow, any broadened automatic event, any automatic dispatcher that can target a workflow other than `release-v25-cloud.yml`, and any release workflow that loses explicit `RELEASE` confirmation.
- `scripts/preflight-all.py` auto-discovers the strict CI policy gate with the other feature preflights.

Keep these guards in place unless the repository owner explicitly changes the policy again.

Related documentation: `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, `README.md`, `docs/CI.md`, `docs/CI-READINESS.md`, `docs/MANUAL-BUILD-RELEASE.md`, `docs/MANUAL-BUILD-RELEASE-V26.md`, `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-V26-QUALIFICATION.md`.
