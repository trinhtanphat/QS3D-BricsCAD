# Agent Collaboration Policy

This repository is expected to have multiple agents working concurrently. Every agent must protect other agents' work, avoid overlapping lanes, and choose tasks that match its actual execution environment.

## Highest-priority Git/Main rule

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for who may change `main`.

**Default:** every normal AI agent/chat session treats `origin/main` as read-only. Source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores all go through a dedicated task branch/PR.

Requests such as `fix bug`, `update code`, `implement all`, `continue all`, `commit`, `commit push git`, `review and fix`, `update docs`, `chore`, `run CI`, or `fix CI` do not by themselves grant permission to push or merge to `main`.

A session may change `main` only when the repository owner explicitly grants a merge/integration role for the named PR/batch/task. Authorization is scope-specific and does not automatically carry forward.

## Locked product form: BricsCAD plugin

QS3D is a **BricsCAD V25 + V26 Windows x64 hosted plugin**, not a standalone CAD desktop executable. A matching licensed BricsCAD host is required at runtime; the native BricsCAD viewport/database/editor remain the CAD host.

V25 loads `QS3D.BricsCAD.V25` built for `net48`; V26 loads `QS3D.BricsCAD.V26` built for `net8.0-windows`. Each host-major assembly is loaded only by the matching BricsCAD host through DemandLoad or `NETLOAD` and must never be relabeled across majors.

`docs/PRODUCT-BOUNDARY.md` is authoritative unless the owner explicitly changes the product boundary.

## Mandatory reading order

Before substantive work, read:

1. `AGENTS.md`;
2. `docs/MAIN-WRITE-AUTHORIZATION.md`;
3. `docs/PRODUCT-BOUNDARY.md`;
4. `CI_POLICY.md`;
5. latest `origin/main` and record its exact SHA;
6. `docs/AGENT-WORK-REGISTRATION.md`;
7. relevant open Issues/PRs plus `ACTIVE`/`BLOCKED` claims;
8. `docs/REMOTE-AGENT-SCOPE.md`;
9. current handoff/status documents relevant to the task;
10. `docs/LOCAL-AGENT-INBOX.md` for LOCAL_ONLY work;
11. exact feature/runbook documents required by the lane.

Current source wins over stale historical handoffs for implementation truth.

## Mandatory task registration and branch discipline

Before implementation, every normal agent must refresh `origin/main`, inspect overlapping Issues/PRs/claims, register the lane with an Issue when practical, and create a dedicated branch normally named `agent/<agent-id>/<scope>`.

Use `recovery/<agent-id>/<scope>` for a dedicated CI-repair lane and `integration/<batch-id>` for an owner-authorized multi-agent combined candidate.

All task changes remain on the task branch. Never force-push/reset `main`, silently overwrite concurrent work, use direct contents/ref writes to bypass the PR path, or merge your own PR without explicit owner authorization.

Refresh `main` before final task-branch push/PR handoff and reconcile relevant concurrent changes deliberately.

## Mandatory CI-before-stop rule

Implementation-relevant tasks must receive automatic remote validation for their **exact current branch/PR head SHA** through `.github/workflows/ci.yml`.

The workflow runs automatically for implementation-relevant pushes to `agent/**`, `recovery/**`, `integration/**`, pull requests targeting `main`, and pushes to `main`.

A task is **CI-neutral-only** only when every changed path is in the ignore set documented by `CI_POLICY.md` and encoded in `.github/workflows/ci.yml`: docs/Markdown plus non-executable housekeeping such as `.gitignore`, `.gitattributes`, `.editorconfig`, `LICENSE*`, `NOTICE*`, and Issue/PR templates. Such tasks do not run the full Core/build workflow and do not require an artificial CI result before completion; perform relevant lightweight checks instead.

The exemption is path-based, never label-based. A commit named `chore: ...` still requires full CI if it touches source, tests, project/build files, dependencies, scripts, workflows, packaging, runtime-affecting configuration, or any other non-ignored file. Any mixed change requires CI. `.github/workflows/**` is intentionally not ignored.

For tasks that are not CI-neutral-only, a normal agent **must not report the task completed, mark the reservation `COMPLETED`, or stop as completed** until the required CI run for the exact current head SHA is `success`. A green run for an older SHA, another branch, another PR, or `main` does not count.

If required CI fails, the task remains active: diagnose the exact failing run, fix the real defect on the same task branch, push a new SHA, and wait for the replacement exact-SHA CI result. Do not weaken guards/tests merely to make CI green.

If native/licensed BricsCAD, UI, private-DWG, signing or other LOCAL_ONLY evidence is required, remote CI success is necessary but not sufficient. The task remains `BLOCKED`/handed off until the required environment-specific evidence exists; remote agents must never claim native/runtime PASS from source/Core CI.

Issues themselves do not run builds because they have no source tree. When CI is required, the Issue must reference the branch/PR SHA whose CI evidence proves the task.

## Normal agent stopping point

The successful endpoint for a normal implementation task is:

```text
latest main read
  -> issue/reservation checked
  -> agent/<agent-id>/<scope>
  -> implementation commits
  -> local/static validation
  -> branch pushed
  -> PR opened/updated
  -> exact head SHA recorded
  -> automatic task CI SUCCESS for that exact SHA
  -> STOP BEFORE MERGE
```

For a CI-neutral-only docs/Markdown/housekeeping task, replace the CI step with path-classification evidence plus relevant lightweight validation. A pushed implementation branch or open implementation PR without exact-head green CI is not a completed task. Passing CI never grants merge permission.

## Owner-authorized integration coordinator

Only a session explicitly authorized by the owner may integrate/merge a named batch into `main`.

For multi-agent implementation work, prefer `integration/<batch-id>`. The coordinator must refresh current `origin/main`, identify exact participating Issues/PRs/branches, integrate all required commits without silently dropping work, resolve semantic/API/test conflicts deliberately, verify no required task remains only off-candidate, require automatic CI `success` for the exact integration head SHA when implementation-relevant paths changed, inspect for accidental reversions/duplicate implementations, merge to `main` only within explicit authorization, then fetch `main` again and record the exact resulting SHA.

After the landing, exact-main CI must also be green for the final current `main` SHA when implementation-relevant paths changed before reporting `ALL MERGED TO MAIN`.

## Definition of ALL MERGED TO MAIN

State **ALL MERGED TO MAIN** only after an authorized integration reviewer verifies against current `main` that every required Issue/reservation is terminal or explicitly excluded/superseded, every required change is represented in current `main`, no required work exists only off-main, the combined tree has no known merge/semantic collisions, required remote-safe CI passed for the exact current `main` SHA when applicable, environment-gated evidence is explicitly classified, and the exact current `main` SHA is recorded.

Branch deletion, Issue/PR UI state or stale CI is not sufficient proof.

## Local-machine agents

Agents with real/local access should prioritize work that genuinely requires licensed BricsCAD V25/V26 runtime access, real `NETLOAD`/DemandLoad, Windows desktop/UI interaction, proprietary dependencies, private DWG fixtures, signing credentials or machine-specific behavior.

`agent/local002`, `agent/local003` and successor sessions in those roles are restricted to LOCAL_ONLY/local-agent-only work unless the owner explicitly assigns otherwise. They must not take broad remote-safe source/CI work from ordinary agents, must not fabricate source fixes solely to satisfy local failures, and must not promote local evidence across host majors.

If local validation exposes a repository-safe source bug, hand it to a normal source agent with the smallest sanitized evidence; the local worker may later resume qualification against the corrected exact SHA.

## Remote / hybrid online agents

Remote/hybrid agents handle repository-safe source review, implementation, Core/domain/persistence/reporting/test code, deterministic regressions, static analysis, docs/planning, workflow/policy maintenance, CI repair and integration preparation.

Remote agents may strengthen source contracts and probes around LOCAL_ONLY areas but must never report remote/static evidence as licensed BricsCAD runtime PASS.

## Unavailable-work handoff

If required proof depends on unavailable licensed runtime, private fixtures, Windows UI, signing credentials, hardware or another non-repository resource, classify the gate accurately, update the relevant LOCAL_ONLY handoff/runbook on the task branch/PR, leave repository-safe implementation/tests ready where possible, and keep the task `BLOCKED` rather than falsely completing it.

## GitHub Actions / release

Follow `CI_POLICY.md` strictly.

- `.github/workflows/ci.yml` is the automatic per-agent/task validation workflow for implementation-relevant changes and must stay read-only; documented CI-neutral-only paths are excluded from the full build.
- `.github/workflows/release-v25-cloud.yml` is **not** agent CI. It is a confirmed main-only preview release workflow with write permission.
- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` is the sole approved automatic release dispatcher and runs only after eligible `main` integration landings.
- BricsCAD V25/V26 native/runtime and other release workflows remain capability-specific/manual lanes unless the owner explicitly changes policy.
- CI success does not imply merge authorization; merge authorization does not imply unrelated release authorization.

## GitHub hard protection

Repository policy should be backed by GitHub branch protection/rulesets where available: protect `main` from force-push/deletion, require PR-based changes for normal writers, require the stable automatic task-CI status on implementation PRs to `main`, and keep owner/admin bypass narrow and deliberate. If a future ruleset requires a status on docs-only PRs, use a lightweight status/ruleset condition rather than forcing the full build workflow for CI-neutral changes.
