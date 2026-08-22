# Mandatory AI agent / chat-session workflow

**Owner policy — 2026-08-14.** This policy applies to every AI agent and chat session working in this repository. It supersedes older wording that allowed or required an ordinary agent/session to publish claim/status/implementation commits directly to `main`.

## 1. Register the work before substantive implementation

The first write for a new prompt/lane must be a visible coordination record that does **not** modify `main` directly.

Preferred registration:

1. refresh current `main` and inspect open claims/issues/PRs plus relevant recent commits;
2. create or reuse a visible GitHub issue for the lane; alternatively use a dedicated claim PR when that is the repository convention;
3. record a stable agent/session identifier, baseline `main` SHA, exact scope, expected files/symbols/tests, exclusions, acceptance criteria, validation/CI plan and intended branch;
4. resolve overlap before implementation;
5. create a dedicated branch such as `agent/<agent-id>/<scope>` (or `recovery/<agent-id>/<scope>` for CI repair).

A chat message, local patch or unpushed branch is not sufficient registration. A claim issue/PR is coordination only and is not permission to merge implementation to `main`.

## 2. Main authorization boundary

Ordinary work prompts do **not** authorize direct writes or merges to `main`.

The following phrases, by themselves, never grant `main` authority: `fix bug`, `update code`, `commit push git`, `continue all`, `implement all`, `run CI`, `fix CI`, `loop until success`, or equivalent wording.

An agent/session may change `main` only when the owner explicitly grants integration authority for that operation, for example: `merge all to main`, `you are the integration coordinator`, or `allow merge PR #... to main`.

CI ownership is separate from integration authority. A CI-fixing session still works on its own branch/PR and must not bypass the integration path.

## 3. Planning gate

Before implementation, publish a concrete plan in the claim issue/PR or equivalent visible coordination record. The plan must include:

- current problem and acceptance criteria;
- reserved scope and explicit exclusions;
- files/symbols/interfaces likely to change;
- regression and compatibility risks;
- tests/preflights/runtime evidence required;
- applicable CI workflows/checks and success criteria;
- known local-only or external prerequisites.

Do not begin broad implementation first and invent the plan afterward.

## 4. Implementation and bug-fix loop

For the reserved lane:

1. refresh latest `main` before material writes;
2. implement the complete requested scope on the dedicated branch;
3. add or update deterministic regression coverage where applicable;
4. run relevant local/static/unit/smoke/preflight checks;
5. review the diff for accidental reversions, overlap and unrelated edits;
6. commit coherently and push the branch;
7. open or update the PR/handoff with exact commit SHA and evidence;
8. if defects remain, continue the same loop instead of reporting completion.

Never weaken tests, architecture guards, security checks, release gates or expected behavior merely to obtain a green result.

## 5. CI loop — continue until applicable checks are green

For task-scoped, non-destructive CI that the session is permitted to operate, CI is part of the normal completion loop:

1. run/observe the applicable CI for the branch/PR/integration candidate;
2. bind every diagnosis to the exact run and exact tested SHA;
3. when red, inspect the failing job/step/log and identify the root cause against current source;
4. fix on the same dedicated branch/recovery branch, add regression coverage when appropriate, commit and push;
5. run/observe a fresh relevant CI attempt;
6. repeat from the newest failure until all required/applicable checks for that lane are green.

This standing task-scoped CI policy does **not** authorize publishing a release, changing `main`, operating unrelated workflows, bypassing release confirmations, or manufacturing native/local evidence.

If the repository has no applicable branch/PR CI for a docs-only change, or path filters intentionally skip code CI, record that fact; do not manufacture a release run solely to make a documentation PR look tested. Required documentation/preflight checks must still pass when they exist.

If required CI or runtime evidence cannot be executed because of missing permissions, proprietary/local environment, credentials or another external prerequisite, the lane is not fully proven. Register the blocker/handoff precisely and do not claim the unavailable evidence as PASS.

## 6. Completion gate and session close/delete verdict

Every agent/chat session must end with an explicit verdict for the user's prompt/lane. The final report must state all of the following:

- `PROMPT/LANE STATUS: 100% COMPLETE` or `NOT 100% COMPLETE`;
- `SESSION CAN BE CLOSED/DELETED: YES` or `NO`;
- branch and PR/issue references;
- exact implementation commit SHA(s);
- tests/preflights/CI actually executed and their result;
- known remaining bugs, blockers, local-only gates or review items;
- separate `MERGED TO MAIN: YES/NO` status.

`100% COMPLETE` means the assigned scope and acceptance criteria are implemented, no known in-scope defect remains, the implementation is pushed and reviewable, and all required/applicable validation that this lane is responsible for is green.

If the prompt did **not** explicitly authorize integration to `main`, a lane may be `100% COMPLETE` and the session may be closed when its branch/PR is fully implemented, validated and handed off. In that case `MERGED TO MAIN` must remain `NO`; final integration is a separate coordinator responsibility.

If the prompt explicitly includes merging/integration to `main`, then `100% COMPLETE` additionally requires verified integration into current `main` and the exact-main validation required by repository policy.

If the verdict is `NOT 100% COMPLETE`, the session must continue the implement/fix/CI loop while there is actionable work within its tools, permissions and reserved scope. It must not stop merely because it reached a checkpoint. It may stop only when an external/local blocker makes further progress impossible in that session, and that blocker must be recorded precisely for handoff.

## 7. Multi-agent handoff discipline

Keep the claim/issue/PR updated enough that another agent can continue without relying on chat history. Before ending a completed lane, ensure the repository-side handoff contains the exact branch, SHA, scope, validation result and remaining integration responsibility.

Do not close or delete the coordination record until the repository's claim lifecycle says it is safe to do so. Closing the chat session and closing the GitHub claim are separate decisions.
