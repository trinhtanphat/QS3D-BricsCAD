# Duplicate-prompt / simultaneous-agent race policy

This document supplements `AGENTS.md` and `docs/AGENT-WORK-REGISTRATION.md` for the specific case where two or more AI agents/chat sessions receive the same owner prompt, materially overlapping prompts, or independently discover the same implementation lane at nearly the same time.

The goal is to prevent duplicate Issues, duplicate branches, duplicate implementations, competing PRs, accidental semantic duplication, and unsafe races to `main`.

## Core rule

Receiving the same prompt does **not** authorize multiple agents to implement the same lane independently.

Every concrete implementation lane has one stable **Lane-Key**. For ordinary Issue-backed work use:

```text
Lane-Key: issue-123
```

An umbrella audit/control Issue is not a shared Lane-Key for every concrete fix discovered beneath it. Each concrete implementation must have its own unique task Issue/Lane-Key before mutation.

For an overlapping scope, the canonical owner is the first **visible valid reservation** that can be verified on GitHub. A valid reservation should identify the Lane-Key, agent/session, scope, baseline and intended task branch, normally through an Issue/claim plus a pushed dedicated branch when available.

For one Lane-Key there may be at most:

- one ACTIVE owner/session;
- one canonical task branch;
- one open canonical PR.

A local-only chat message, unpushed worktree, stash, or unpublished patch is not a visible reservation and does not defeat an already-visible GitHub reservation.

A second session finding an equivalent visible carrier must stop the overlap as:

```text
DUPLICATE_CARRIER / NO MUTATION
```

A newer baseline, cleaner history, greener CI, different agent/model/session, or easier merge path does not create a second ownership right.

## Mandatory collision check

Before substantive implementation, every agent must perform the smallest sufficient GitHub collision check for the intended lane:

1. refresh exact current `origin/main`;
2. search current open Issues/claims for the semantic behavior;
3. search current open PR metadata for the Lane-Key and equivalent behavior;
4. inspect only the minimum expected production-file/symbol ownership metadata needed to detect overlap;
5. determine the stable Lane-Key before mutation;
6. identify the current canonical carrier, if one exists.

Do not rely only on PR titles, branch names, textual mergeability, or whether another carrier currently has green CI.

Repeat the collision check immediately before the first branch push, before opening a PR, and after any material scope expansion.

## Simultaneous-start race

When two agents begin before either can see the other's reservation:

1. each agent performs the normal collision check before substantive implementation and repeats it before first branch push/PR handoff;
2. once one valid reservation becomes visible, that reservation owns the overlapping Lane-Key;
3. any later agent that detects the reservation stops changing the overlapping files/symbols/acceptance criteria;
4. the later agent may re-scope only to a **genuinely distinct** task after a fresh collision check, a separate dedicated Issue/Lane-Key, and explicit non-overlapping scope/exclusions;
5. the later agent must not reuse the original Lane-Key, carrier, acceptance criteria, or expected production ownership for the re-scoped task;
6. agents must not race to finish first merely because both started from the same owner prompt.

Re-scope is not a loophole for renaming the same fix or splitting one authority into competing implementations.

## Canonical carrier and supersession

A stale, red, queued, behind-`main`, draft, blocked, slow or inconvenient branch/PR is still owned until it is explicitly released or superseded. None of those states makes the lane free for takeover.

If the canonical carrier needs current-main reconciliation:

1. prefer updating the same task branch non-force and keeping the same canonical PR; or
2. when rebuilding from current `main` is genuinely required, explicitly record supersession first, close the old open PR, create exactly one replacement carrier, and preserve the same Lane-Key.

There must not be an intentional overlap window with two open canonical PRs for one Lane-Key.

Do not create branch-to-branch/internal PRs whose only purpose is to merge/replay `main`, another task branch, or a temporary carrier into the canonical branch. Reconcile locally/non-force on the canonical task branch, or use one explicitly superseding replacement carrier.

Stacked PRs are allowed only for a real documented dependency stack; they are not synchronization/transport PRs.

## If both agents already produced commits

Do not force-push shared history merely to hide the collision.

- The canonical owner continues the implementation lane.
- The non-canonical branch may remain temporarily as evidence/fallback until its owner or an authorized coordinator closes/releases it, but it is not an active implementation carrier.
- The non-canonical agent must not merge its overlapping patch, open another equivalent carrier, or copy the implementation into a renamed lane.
- If useful ideas/tests exist only on the non-canonical branch, an owner-authorized coordinator may deliberately adopt those pieces after review; this is not automatic takeover permission.

## If both agents already opened PRs

Only one PR may be the canonical integration candidate for the same Lane-Key/implementation.

The other PR must be treated as `SUPERSEDED` / `DUPLICATE` / fallback and must not be merged in parallel. Closing or marking the duplicate is done by its owner or an explicitly authorized coordinator; a normal unrelated agent does not manage another agent's PR.

Once this policy's CI gate is present on `main`, same-repository `agent/**` and `integration/**` PRs must expose Lane-Key metadata and PR preflight fails when another open PR already claims the same Lane-Key.

A clean textual merge is not proof that the two PRs are compatible. Two patches can touch different lines/files and still implement competing stores, coordinators, commands, UI routes, ownership models, validation contracts or persistence paths.

## Required PR metadata

Every same-repository agent/integration PR must state near the top of the body:

```text
Lane-Key: issue-123
Canonical owner/session: <stable-agent-or-session-id>
Canonical carrier: agent/<agent-id>/<scope>
Supersedes: none
```

If a replacement carrier is explicitly authorized, `Supersedes` names the prior PR/branch and the prior open PR must be closed before the replacement is represented as canonical.

For legacy PR bodies, the gate may infer `issue-<number>` from a single unambiguous `Issue: #123` or closing reference such as `Fixes #123`; new PRs should use explicit `Lane-Key` metadata.

## Machine-enforced PR collision gate

The shared `.github/workflows/ci.yml` runs an authenticated, read-only Lane-Key collision gate on PR events.

The gate:

- applies to same-repository `agent/**` and `integration/**` carriers;
- requires stable Lane-Key metadata for those PRs;
- reads open PR metadata only;
- fails closed when another open PR has the same Lane-Key;
- does not mutate Issues, branches, PRs or `main`;
- runs its network lookup only in the dedicated PR step with read-only `pull-requests: read` permission and `GITHUB_TOKEN`;
- keeps aggregate `preflight-all.py` hermetic by running only deterministic regression coverage there.

This gate prevents duplicate **PR carriers**. It does not replace the mandatory pre-mutation semantic/file collision check because two different Lane-Keys can still overlap semantically.

## If one implementation already landed on current main

The other agent must refresh `origin/main` and treat current main as implementation truth.

If the landed implementation satisfies the overlapping acceptance criteria, the remaining agent releases/closes **its own** claim or re-scopes to a genuinely distinct gap with a new Lane-Key. It must not recreate the same fix under a new Issue, branch, command name or Lane-Key.

If the landed implementation only partially satisfies the original request, any follow-up lane must be narrowly defined around the remaining gap, assigned a distinct Lane-Key, and collision-checked again.

## Partial overlap

If prompts overlap only in part, split by stable ownership boundaries such as:

- distinct files/modules/services;
- distinct symbols/APIs;
- distinct acceptance criteria;
- distinct runtime/environment boundary;
- distinct regression or documentation surface that can be integrated independently.

Do **not** split a lane by having two agents simultaneously edit different halves of the same function, state machine, persistence transaction or ownership contract unless an authorized coordinator explicitly defines that shared design.

Different Lane-Keys do not automatically make overlapping edits safe. Same production-file ownership or equivalent semantic authority remains a collision signal.

## Takeover and stale-looking work

A normal agent may not take over another reservation because the other agent appears slow, idle, disconnected, red in CI, draft, stale, behind `main`, blocked or incomplete.

Takeover/reassignment requires one of:

- explicit repository-owner reassignment; or
- an owner-authorized integration coordinator explicitly resolving ownership for the named lane/batch.

Until reassigned, `ACTIVE` / `BLOCKED` ownership remains in force under the canonical work-registration policy.

## Ambiguous ownership

If two visible reservations are so close in time or wording that canonical ownership cannot be determined safely:

1. freeze both overlapping merge paths;
2. do not merge either implementation merely because one is currently green;
3. preserve both branches/PRs without force-pushing away evidence;
4. ask the repository owner or authorized integration coordinator to designate the canonical lane or define a non-overlapping split;
5. record the winning Lane-Key/carrier and explicit supersession before resuming.

## Integration-coordinator duplicate check

For a multi-agent batch, the authorized integration coordinator must check for **semantic duplication**, not only Git conflicts or Lane-Key equality.

Before landing the batch, verify that the combined tree does not contain duplicate or competing:

- domain/services/calculators for the same authority;
- persistence stores or sidecars for the same data;
- commands/buttons/routes for the same workflow without an intentional single canonical route;
- ownership/XData/handle contracts for the same generated artifact;
- CI/preflight contracts that encode contradictory behavior;
- issue/claim records that still present two agents as ACTIVE owners of the same lane;
- canonical PR carriers for the same Lane-Key.

If duplicate implementations exist, choose/reconcile one canonical design deliberately and keep the rejected path out of the integrated tree.

## Required agent behavior summary

For same-prompt or overlapping-prompt races:

```text
same/overlapping owner prompt
  -> refresh current main
  -> determine Lane-Key
  -> minimal Issue/claim/PR/file-ownership collision check
  -> first visible valid reservation owns overlap
  -> later agent stops overlap or creates a truly distinct new Lane-Key
  -> one canonical branch + one open PR carrier per Lane-Key
  -> stale/red/behind does not release ownership
  -> no transport/reconciliation PRs
  -> PR CI rejects duplicate open Lane-Key carriers
  -> unresolved ownership freezes merge
  -> takeover only by explicit owner/coordinator reassignment
  -> integration checks semantic duplicates
```

## Relationship to existing policy

All existing repository rules remain in force, especially:

- normal sessions treat `main` as read-only;
- every task change belongs on a dedicated task branch/PR;
- `AGENTS.md` and `docs/AGENT-WORK-REGISTRATION.md` remain authoritative for general multi-agent behavior;
- `docs/MAIN-WRITE-AUTHORIZATION.md` remains authoritative for merge/write permission;
- automatic CI and mergeability never grant ownership or merge authorization;
- no agent may silently overwrite another agent's work.

This file is the canonical tie-break, carrier-uniqueness and recovery procedure for duplicate/overlapping-agent races. It does not weaken any existing branch, CI, LOCAL_ONLY, or protected-main boundary.
