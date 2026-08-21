# Agent Lane Lock (compatibility alias)

The canonical duplicate-agent ownership, Lane-Key and single-carrier policy is:

`docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`

The canonical policy for **GitHub `#<number>` identity, leaf Issue/Lane-Key derivation, session ownership tokens and collision-safe branch naming** is:

`docs/AGENT-IDENTITY-AND-BRANCH-NAMESPACE.md`

The canonical boundary between ChatGPT account scheduled tasks and repository ownership/lane semantics is:

`docs/CHATGPT-SCHEDULE-BOUNDARY.md`

The owner-approved PR-CI timing correction is:

`docs/PR-CI-LIFECYCLE.md`

## Mandatory numeric/branch clarification

A Git branch has a ref name; it does **not** have a GitHub `#number`. GitHub Issues and Pull Requests share the repository's numbered item sequence, so agents must never guess a future Issue/PR number or derive it from the latest visible number.

For every new ordinary issue-backed concrete task:

1. create/reuse the concrete **leaf Issue** first;
2. use the exact GitHub-returned number `N` as `Lane-Key: issue-N`;
3. immediately repeat the semantic collision check after Issue creation to stabilize simultaneous-start claims;
4. only then create the one canonical branch, normally `agent/<owner-token>/issue-N-<short-scope>`;
5. never use a parent/umbrella/control Issue number as the child implementation Lane-Key or branch task identity;
6. never use generic labels such as `chatgpt`, `gpt56sol`, `C0`, `W1`, `worker`, or `controller` alone as a globally unique owner token;
7. if the leaf Issue already records another session's canonical branch, stop as `DUPLICATE_CARRIER / NO MUTATION` instead of creating `-r2`, `-final`, `-rebased`, or another competing carrier.

`docs/AGENT-IDENTITY-AND-BRANCH-NAMESPACE.md` is authoritative when older examples or historical claims are ambiguous about these identity/namespace points. Existing legacy active carriers are not retroactively renamed merely to normalize old naming.

A ChatGPT scheduled task is only an external account-side prompt/task trigger. Labels such as `C0`, `W1-W4`, `controller`, `worker`, or `Task 0-4` do not create repository Lane-Keys, GitHub ownership, canonical carriers, CI authority, or merge authority by themselves.

Any older wording elsewhere that refers to `scheduled/controller lanes`, `scheduled workers`, hourly controller pools, or similar concepts must be read through `docs/CHATGPT-SCHEDULE-BOUNDARY.md`: the schedule is only the invocation source; the resulting chat/session still follows the ordinary current GitHub Lane-Key / Issue / branch / PR ownership rules.

Any older wording that treats the timestamp ordering of automatic branch-CI completion versus PR creation as a permanent admission boundary must be read through `docs/PR-CI-LIFECYCLE.md`: timing alone does not poison a canonical PR or require replacement. Protected current-candidate `preflight`/`core`, freshness, Lane-Key uniqueness and expected-head merge safety remain mandatory.

This file contains no independent duplicate-carrier policy. It exists as a compatibility pointer for branch/claim/handoff references while making the numeric identity, external-scheduler and PR-CI timing boundaries unambiguous.

Read and follow the canonical policies above together with `AGENTS.md`, `CI_POLICY.md`, and `docs/AGENT-WORK-REGISTRATION.md`.