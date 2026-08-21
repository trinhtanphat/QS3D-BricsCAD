# Agent identity, Issue/Lane and branch namespace policy

This document is the canonical policy for **numeric GitHub identity, leaf-task Lane-Keys, agent/session identity, and branch naming** in concurrent AI/chat/scheduled-agent work.

It supplements `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`, `docs/CHATGPT-SCHEDULE-BOUNDARY.md`, and `docs/AGENT-LANE-LOCK.md`.

When older examples are ambiguous about `#<number>`, use a parent/umbrella Issue number as an implementation lane, allow a guessed future number, or use a generic agent label as a globally unique branch owner, **this file wins for identity/namespace questions**.

## Why this policy exists

QS3D is worked on by many independent AI/chat sessions and scheduled tasks. Several different identifiers exist at once:

- GitHub **Issue number**;
- GitHub **PR number**;
- repository **Lane-Key**;
- agent/session identity;
- Git branch name;
- optional parent/umbrella/control Issue;
- ChatGPT schedule labels such as `C0`, `W1`, `W2`, `W3`, `W4`.

These are not interchangeable. Treating them as if they were the same identity creates duplicate carriers, accidental branch reuse, stale ownership, or two sessions implementing the same task.

## Critical clarification: branches do not have `#numbers`

A Git branch has a **ref name**, for example:

```text
agent/interactive-20260821-ns/issue-3304-agent-namespace-policy
```

A branch does **not** have a GitHub `#3304` number.

`#3304` refers to an Issue or Pull Request item in the repository. GitHub Issues and Pull Requests share the repository's numbered item sequence, so a PR creation may consume a number that another session incorrectly expected to become the "next Issue".

Therefore:

- never say or assume "branch #123";
- never calculate or guess the next Issue/PR number from the latest visible number;
- never reserve a number in chat, Markdown, a branch name, or a local variable before GitHub has created the Issue/PR;
- always use the exact `issue_number` / `pr_number` returned by GitHub.

## Canonical identity model

For every **new concrete issue-backed implementation task**:

```text
Leaf Issue:      #N              <- allocated by GitHub
Lane-Key:        issue-N         <- derived only from the real leaf Issue
Owner token:     <session token> <- identifies the owning execution identity
Canonical branch: agent/<owner-token>/issue-N-<short-scope>
Canonical PR:    #P              <- allocated later by GitHub; never guessed
Parent/Umbrella: #U or none      <- relationship only; never the child Lane-Key
```

The leaf Issue number and PR number may be numerically close, but they are different objects with different roles.

## Leaf Issue rule — no parent/umbrella number reuse

A program, audit, control-board, epic, roadmap, or umbrella Issue may coordinate many children. It is **not** the implementation identity for every child.

If `#3142` is a parent program and an agent finds a concrete defect beneath it, the agent must create/reuse a **leaf task Issue** for that concrete defect before mutation.

Correct:

```text
Parent: #3142
Leaf Issue: #3304
Lane-Key: issue-3304
Branch: agent/<owner-token>/issue-3304-<scope>
```

Incorrect:

```text
Parent: #3142
Lane-Key: issue-3142
Branch A: agent/w1/3142-fix-a
Branch B: agent/w2/3142-fix-b
```

Two unrelated concrete tasks must not share a parent/umbrella Issue number as their Lane-Key or branch task identity.

## New Lane-Key rule

For every **new ordinary issue-backed concrete task**, the Lane-Key is exactly:

```text
issue-<actual GitHub leaf Issue number>
```

Examples:

```text
Issue #3304 -> Lane-Key: issue-3304
Issue #3307 -> Lane-Key: issue-3307
```

Do not create new issue-backed tasks with:

```text
Lane-Key: issue-pending
Lane-Key: issue-next
Lane-Key: issue-<guessed-number>
Lane-Key: <umbrella-issue-number>
Lane-Key: w1-foo
Lane-Key: chatgpt-foo
Lane-Key: <free-form semantic alias>
```

Legacy active carriers that already use an older valid custom Lane-Key are **not retroactively renamed** merely to satisfy this policy. Renaming a live carrier would itself create risk. This rule applies to new leaf tasks and explicitly superseding replacement carriers created after this policy lands.

## Mandatory two-phase reservation sequence

A new agent/session must not create a task branch first and fill in the Issue number later.

Use this order:

```text
1. refresh current main
2. semantic collision search
3. create/reuse the concrete leaf Issue
4. receive the actual GitHub Issue number
5. derive Lane-Key = issue-N
6. immediately re-run the semantic/ownership collision check
7. stabilize the winning leaf Issue/owner
8. create exactly one canonical branch using issue-N
9. update the Issue with the exact branch name
10. only then begin substantive repository mutation
```

A provisional Issue may briefly say `Lane-Key: pending GitHub-assigned issue number` **only during the API create -> immediate update transition**. No branch or implementation mutation may be based on that placeholder.

## Post-create stabilization — required race check

The pre-create search is necessary but not sufficient. Two sessions can both search at nearly the same time, both find nothing, and both create semantically equivalent Issues.

Immediately after a session creates a leaf Issue, and again before its first branch push, it must search current open Issues/PRs for equivalent semantic scope.

If an equivalent earlier valid leaf reservation is now visible:

- the later session must not create/push a competing branch;
- if the later session created the duplicate Issue itself and no canonical work depends on it, it should mark/close **its own** Issue as duplicate of the winning Issue;
- it must not close or rewrite the other session's Issue;
- it must continue only if the owner/coordinator explicitly reassigns the winning lane.

Deterministic tie-break when two otherwise equivalent provisional Issues are visible and neither has a prior canonical branch/PR:

1. earlier GitHub `created_at` wins;
2. if timestamps are indistinguishable for practical purposes, lower GitHub Issue number wins;
3. once a valid canonical branch/PR exists, the ordinary first-visible canonical-carrier rule controls.

If ownership is still ambiguous, freeze overlapping mutation and use the ambiguity procedure in `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`.

## Canonical branch naming

For a new ordinary agent task use:

```text
agent/<owner-token>/issue-<N>-<short-scope>
```

Examples:

```text
agent/interactive-20260821-ns/issue-3304-agent-namespace-policy
agent/schedule-6a815c60/issue-3302-actions-reparse-guard
```

For an owner-authorized integration batch, continue using the repository's integration convention, but the batch identity must still be explicit and collision-checked:

```text
integration/<batch-id>
```

### Owner token requirements

The owner token identifies the execution identity that owns the leaf task. It must be specific enough to distinguish independent sessions/accounts.

Good sources include:

- an automation/task identifier or stable prefix supplied by the account tooling;
- an interactive session token supplied by the execution environment;
- a deliberately generated short opaque token combined with an interactive date/context label.

The following labels **alone are not globally unique owner identities**:

```text
chatgpt
gpt56sol
claude
codex
C0
W1
W2
W3
W4
worker
controller
agent-a
```

They may appear as descriptive prefixes only when paired with a session/account/automation-specific token.

The real leaf Issue number remains part of every new ordinary task branch, so two distinct leaf tasks cannot accidentally use the same branch ref merely because their scope slug is similar.

## Existing branch rule — do not "invent a cleaner branch"

When the leaf Issue already records a canonical branch:

- fetch/use that exact branch if this session is the recorded owner and continuation is allowed;
- if another owner owns it, stop as `DUPLICATE_CARRIER / NO MUTATION`;
- do not generate another branch name from the same Issue number;
- do not append `-r2`, `-r3`, `-rebased`, `-final`, `-new`, or a timestamp merely because `main` moved, CI failed, or the branch is inconvenient;
- reconcile the same canonical branch non-force whenever possible.

A replacement branch is allowed only under the repository's explicit supersession/reassignment procedure. Record the old branch/PR as superseded before the replacement becomes canonical.

## Branch creation preflight

Immediately before creating a new branch:

1. fetch the exact current `main` baseline;
2. fetch the leaf Issue and verify it is the intended concrete task;
3. verify `Lane-Key: issue-N` matches that Issue number;
4. verify the Issue is not an umbrella/control Issue being reused for a child fix;
5. re-check equivalent Issues/PRs and active ownership;
6. search the exact proposed branch name;
7. if that branch already exists, do **not** overwrite/update its ref as a shortcut; determine whether it is the canonical branch for the same owner/lane;
8. create the branch only when the reservation is stabilized and no competing canonical carrier exists.

A branch-create collision is an ownership signal, not permission to move the existing ref.

## Schedule / multi-account rule

`C0` / `W1`-`W4` are account-local scheduling labels only. Multiple ChatGPT accounts can all have a `W1`.

Therefore a branch such as:

```text
agent/w1/fix-foo
```

is not sufficiently namespaced for new scheduled work.

Use the actual automation/account-local task identity when available, for example:

```text
agent/schedule-6a815c50/issue-3001-section-plan-invariants
```

Every scheduled invocation still performs the ordinary GitHub collision/stabilization process. A schedule label never reserves an Issue number, Lane-Key, or branch.

## PR identity rule

A PR number is assigned only after the PR is created. Do not predict it.

Required metadata for new ordinary task PRs:

```text
Leaf Issue: #N
Lane-Key: issue-N
Canonical owner/session: <owner-token or full stable identity>
Canonical carrier: agent/<owner-token>/issue-N-<scope>
Parent/Umbrella: #U or none
Supersedes: none | <explicit old carrier>
```

The PR number `#P` is separate from Issue `#N` even though GitHub Issues and PRs share the repository numbering sequence.

## Never infer task identity from number proximity

Forbidden assumptions include:

- "PR #3305 probably belongs to Issue #3304";
- "the latest item is #3304, so my new Issue will be #3305";
- "branch 3304 is mine because my previous chat mentioned 3304";
- "W1 owns every branch beginning with `agent/w1/`";
- "the parent Issue number is safe for every child branch".

Always resolve relationships from live GitHub metadata: Issue body, Lane-Key, canonical owner/session, canonical branch, PR body, and current repository state.

## No cross-session branch mutation

A normal session must never:

- push commits to another session's canonical branch;
- move another session's branch ref;
- force-update another session's branch;
- reuse another lane's Issue number in a new branch;
- close another session's Issue merely to free the number;
- create a competing branch under the same leaf Issue without explicit reassignment;
- rename/recreate a landed historical branch just to normalize old naming.

An owner-authorized integration coordinator may coordinate named lanes under the existing integration policy, but does not erase their identity history.

## Practical examples

### Correct new interactive task

```text
collision check -> no equivalent lane
create Issue -> GitHub returns #3304
Lane-Key -> issue-3304
post-create collision check -> #3304 wins/stabilizes
branch -> agent/interactive-20260821-ns/issue-3304-agent-namespace-policy
push -> same branch
PR -> GitHub later assigns its own #P
```

### Correct child of an umbrella program

```text
Program: #3142
Concrete new task Issue: #3304
Parent: #3142
Lane-Key: issue-3304
Branch: agent/<owner-token>/issue-3304-<scope>
```

### Wrong: guessed Issue number

```text
latest visible item is #3304
agent assumes next Issue will be #3305
agent creates branch .../issue-3305-foo
another PR is created first and consumes #3305
```

The branch now encodes a false identity. This is forbidden.

### Wrong: generic schedule branch

```text
account A W1 -> agent/w1/foo
account B W1 -> agent/w1/foo
```

The schedule label is not globally unique. Use the concrete leaf Issue plus a schedule/session-specific owner token.

## Legacy migration rule

Do not mass-rename historical branches, Issues, Lane-Keys, or merged PR metadata. That would generate more churn and collision risk.

For existing active carriers:

- keep the current canonical identity unless an explicit supersession is already required;
- apply the new rule when a genuinely new leaf task is registered;
- apply the new branch namespace to an explicitly authorized replacement carrier;
- document parent/leaf distinction during the next normal metadata update when useful.

## Required stop conditions

Stop with `DUPLICATE_CARRIER / NO MUTATION` when:

- the same leaf Issue/Lane-Key is owned by another current canonical session;
- an equivalent earlier valid leaf Issue/carrier is discovered during stabilization;
- the proposed branch already exists and is not proven to be this session's canonical carrier;
- the only available number is guessed rather than returned by GitHub;
- the intended Issue is actually an umbrella/control Issue and no concrete leaf Issue exists yet.

Resolve/register identity first. Do not "work around" the collision by changing a slug or adding a branch suffix.

## Summary invariant

For new concrete work:

```text
GitHub allocates leaf Issue #N
        ↓
Lane-Key is exactly issue-N
        ↓
post-create semantic claim stabilization
        ↓
one owner/session
        ↓
one canonical branch containing issue-N
        ↓
one canonical PR with separately allocated PR #P
```

**Numbers are obtained, never guessed. Parent numbers are relationships, never child lane identities. Branches are names, not `#numbers`. Generic AI/schedule labels are not globally unique session identities.**