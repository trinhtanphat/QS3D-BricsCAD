# QS3D agent work registration and integration

**Owner rule — 2026-08-14:** every AI agent/chat session must register its lane before substantive implementation, but ordinary agents/sessions must keep **all claim/status/implementation writes off `main`** unless the owner explicitly grants integration authority.

This file is the canonical reservation/integration contract. It supersedes older wording in `AGENTS.md`, older claim files, handoffs or CI docs that required a claim-only/status-only Markdown commit to `origin/main` before work. Read `docs/AI-SESSION-WORKFLOW.md` together with this file.

## Source of truth for active ownership

Existing files under `docs/agent-work-claims/` remain valid coordination history. Any existing `ACTIVE` or `BLOCKED` claim must still be respected until it becomes terminal or the owner coordinates a takeover.

For **new** work after this owner policy, a visible GitHub coordination record is sufficient reservation and is preferred because it avoids unnecessary writes to `main`:

- preferred: a GitHub issue dedicated to the lane;
- acceptable: a dedicated claim PR when a PR is a better coordination surface;
- optional: a claim Markdown file committed on the agent/claim branch and included in its PR.

A chat message, local patch, private note or unpushed branch is not a reservation.

Recommended claim states:

- `ACTIVE` — lane is reserved and work is in progress or ready for handoff;
- `BLOCKED` — lane remains reserved but needs an external/local prerequisite;
- `READY_FOR_INTEGRATION` — implementation/validation is complete on the branch/PR and waits for an authorized integrator;
- `COMPLETED` — the lane's requested scope is fully implemented/validated and repository-side handoff is complete; whether it is merged to `main` must be reported separately;
- `RELEASED` — intentionally abandoned/superseded without claiming completion.

## Mandatory sequence before implementation

1. Fetch/refresh current `origin/main` and inspect relevant recent commits.
2. Read `AGENTS.md`, `CI_POLICY.md`, `docs/AI-SESSION-WORKFLOW.md`, this file, all existing `ACTIVE` / `BLOCKED` claim files, and open claim/issues/PRs relevant to the same surfaces.
3. Choose a non-overlapping lane.
4. Create/update the visible claim issue/PR **before substantive implementation**.
5. Record at minimum:
   - stable agent/session identifier;
   - registration timestamp/timezone;
   - exact baseline `main` SHA;
   - reserved feature/runtime scope;
   - expected files/symbols/commands/tests;
   - explicit exclusions and neighboring lanes;
   - acceptance criteria;
   - validation/CI plan;
   - intended implementation branch;
   - known local-only/external prerequisites.
6. Resolve any detected overlap before material writes.
7. Create the implementation branch, normally `agent/<agent-id>/<scope>`; CI repair may use `recovery/<agent-id>/<scope>`.
8. Publish a concrete implementation plan in the claim issue/PR.
9. Implement only the reserved lane on that branch.

**There is no claim-only/status-only direct-`main` exception anymore.** Registration visibility comes from GitHub issues/PRs/branches rather than a coordination commit to `main`.

## Main authorization boundary

Ordinary prompts such as `fix bug`, `update code`, `commit push git`, `continue all`, `implement all`, `run CI`, `fix CI` or `loop until success` do not authorize any direct write or merge to `main`.

Only explicit owner integration authorization permits changing `main`, for example:

- `merge all to main`;
- `you are the integration coordinator`;
- `allow merge PR #... to main`;
- another instruction that unambiguously grants that exact integration operation.

CI ownership never implies integration authority.

## Implementation branch discipline

Every implementation agent/session must:

1. start from or refresh against the latest appropriate `main` baseline;
2. periodically re-check relevant concurrent work and active claims;
3. keep changes inside the reserved scope;
4. use coherent request/lane-level commits rather than file-by-file noise;
5. add/retain deterministic regression coverage for behavioral changes where applicable;
6. run relevant local/static/unit/smoke/preflight checks;
7. push the implementation branch and open/update its PR/handoff;
8. record exact implementation commit SHA(s) and executed evidence in the coordination record;
9. never force-push `main`, reset it backwards or silently overwrite another agent's work.

A pushed branch or open PR is not proof of integration to `main`.

## Mandatory CI/fix loop

Task-scoped, non-destructive CI that the session is permitted to operate is part of the normal completion loop.

When CI/checks are red:

1. identify the exact run and exact tested SHA;
2. inspect the failing job/step/log and diagnose the root cause against current source;
3. fix on the same agent/recovery branch, not on `main`;
4. add/retain regression coverage when appropriate;
5. commit and push the repair;
6. run/observe a fresh relevant attempt;
7. repeat from the newest relevant failure until all required/applicable checks for that lane are green.

Do not weaken tests, product contracts, architecture guards, security/release checks or expected behavior merely to obtain green CI.

For docs-only changes, intentionally skipped code/release jobs are not failures when the repository has no applicable docs CI. Record what did and did not run; do not manufacture a release run solely for a documentation PR.

## Multi-agent batch integration

For a multi-agent owner request, participating branches remain staging inputs. An authorized integration coordinator may use `integration/<batch-id>` as the combined candidate.

The coordinator must:

1. refresh current `origin/main`;
2. enumerate the exact participating claims/branches/PRs/implementation SHAs;
3. integrate every required lane without silently dropping work;
4. resolve semantic/API/test conflicts deliberately rather than blindly choosing `ours` / `theirs`;
5. verify no required implementation remains only on an unintegrated branch/local patch;
6. run relevant combined-tree preflights/builds/smoke/tests;
7. inspect the combined diff for accidental reversions and duplicate competing implementations;
8. freeze the batch;
9. perform the explicitly authorized final PR/landing to `main`;
10. refresh `main`, record the exact final SHA and follow `CI_POLICY.md` for exact-main/final V25 cloud evidence.

Do not assemble the batch by having ordinary agents independently merge themselves into `main`.

## Definition of `ALL MERGED TO MAIN`

Report `ALL MERGED TO MAIN` only when an authorized integration reviewer has freshly verified all of the following against current `origin/main`:

- every required participating lane is integrated or explicitly excluded/superseded;
- every required implementation is represented in the combined result and reachable from current `main`;
- no required code exists only on an agent branch, local worktree, stash, draft patch or unmerged PR;
- the current combined tree has no unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- required combined-tree validation has passed or any unavoidable LOCAL_ONLY gate is explicitly classified;
- the exact current `main` SHA is recorded.

Branch deletion, issue state, PR UI state or an old green CI run is not sufficient proof.

## Prompt/lane completion vs integration

Every AI/chat session must follow `docs/AI-SESSION-WORKFLOW.md` and finish with a repository-side and chat-side verdict:

- `PROMPT/LANE STATUS: 100% COMPLETE` or `NOT 100% COMPLETE`;
- `SESSION CAN BE CLOSED/DELETED: YES` or `NO`;
- `MERGED TO MAIN: YES` or `NO`;
- branch/PR/issue references, exact implementation SHA(s), validation/CI results and remaining blockers.

If the prompt did not authorize integration, a lane may be `100% COMPLETE` while `MERGED TO MAIN: NO` when its branch/PR is fully implemented, all lane-responsible validation is green, no known in-scope bug remains and the handoff is self-contained.

If the prompt explicitly includes integration to `main`, `100% COMPLETE` additionally requires verified final integration plus the exact-main evidence required by `CI_POLICY.md`.

If the lane is not 100% complete and actionable work remains within the session's tools/permissions/scope, continue the plan -> implement/fix -> validate/CI -> diagnose -> repair loop rather than stopping at a checkpoint.

## Scope changes and handoff

If scope expands, stop before touching the added surface, refresh current claims, amend the visible issue/PR with the new reserved scope and plan, resolve overlap, then continue on the same or a newly appropriate branch.

If the current environment lacks a required licensed BricsCAD runtime, private DWG, signing credential, local UI, hardware or other external resource, record the smallest exact blocker and required evidence in the canonical local/handoff mechanism. Do not manufacture PASS evidence and do not repeatedly rediscover the same local-only gate from equivalent remote sessions.

## Evidence boundaries

- Never force-push or reset `main` backwards.
- Never silently overwrite concurrent work.
- Task-scoped CI authorization is not release authorization and not `main` authorization.
- Final V25 cloud/release behavior is governed by `CI_POLICY.md`.
- LOCAL_ONLY BricsCAD runtime/native UI/private-DWG/signing/performance evidence remains separate from remote/static CI.
