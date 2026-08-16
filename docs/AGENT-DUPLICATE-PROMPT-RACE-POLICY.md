# Duplicate-prompt / simultaneous-agent race policy

This document supplements `AGENTS.md` and `docs/AGENT-WORK-REGISTRATION.md` for the specific case where two or more AI agents/chat sessions receive the same owner prompt, or materially overlapping prompts, at nearly the same time.

The goal is to prevent duplicate Issues, duplicate branches, duplicate implementations, competing PRs, accidental semantic duplication, and unsafe races to `main`.

## Core rule

Receiving the same prompt does **not** authorize multiple agents to implement the same lane independently.

For an overlapping scope, the canonical owner is the first **visible valid reservation** that can be verified on GitHub. A valid reservation should identify the agent/session, scope, baseline and intended task branch, normally through an Issue/claim plus a pushed dedicated branch when available.

A local-only chat message, unpushed worktree, stash, or unpublished patch is not a visible reservation and does not defeat an already-visible GitHub reservation.

## Simultaneous-start race

When two agents begin before either can see the other's reservation:

1. each agent must perform the normal minimal collision check before substantive implementation and repeat it before the first branch push/PR handoff;
2. once one valid reservation becomes visible, that reservation owns the overlapping lane;
3. any later agent that detects the reservation must stop changing the overlapping files/symbols/acceptance criteria;
4. the later agent may re-scope only to clearly non-overlapping work and must record the new scope/exclusions in its own Issue/claim;
5. agents must not race to finish first merely because both started from the same owner prompt.

## If both agents already produced commits

Do not destroy either branch and do not force-push shared history merely to hide the collision.

- The canonical owner continues the implementation lane.
- The non-canonical branch may remain as evidence/fallback until the coordinator decides it is no longer useful.
- The non-canonical agent must not merge its overlapping patch or copy its implementation into another lane.
- If useful ideas/tests exist only on the non-canonical branch, an owner-authorized coordinator may deliberately adopt those pieces after review; this is not automatic takeover permission.

## If both agents already opened PRs

Only one PR may be the canonical integration candidate for the same implementation.

The other PR must be treated as `SUPERSEDED` / `DUPLICATE` / fallback and must not be merged in parallel. Closing or marking the duplicate is done by its owner or an explicitly authorized coordinator; a normal unrelated agent does not manage another agent's PR.

A clean textual merge is not proof that the two PRs are compatible. Two patches can touch different lines/files and still implement competing stores, coordinators, commands, UI routes, ownership models, validation contracts or persistence paths.

## If one implementation already landed on current main

The other agent must refresh `origin/main` and treat current main as implementation truth.

If the landed implementation satisfies the overlapping acceptance criteria, the remaining agent releases/closes **its own** claim or re-scopes to a genuinely distinct gap. It must not recreate the same fix under a new Issue, branch or command name.

If the landed implementation only partially satisfies the original request, any follow-up lane must be narrowly defined around the remaining gap and collision-checked again.

## Partial overlap

If prompts overlap only in part, split by stable ownership boundaries such as:

- distinct files/modules/services;
- distinct symbols/APIs;
- distinct acceptance criteria;
- distinct runtime/environment boundary;
- distinct regression or documentation surface that can be integrated independently.

Do **not** split a lane by having two agents simultaneously edit different halves of the same function, state machine, persistence transaction or ownership contract unless an authorized coordinator explicitly defines that shared design.

## Takeover and stale-looking work

A normal agent may not take over another reservation because the other agent appears slow, idle, disconnected, red in CI, draft, stale, or incomplete.

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
5. resume only after the decision is recorded on a visible coordination surface.

## Integration-coordinator duplicate check

For a multi-agent batch, the authorized integration coordinator must check for **semantic duplication**, not only Git conflicts.

Before landing the batch, verify that the combined tree does not contain duplicate or competing:

- domain/services/calculators for the same authority;
- persistence stores or sidecars for the same data;
- commands/buttons/routes for the same workflow without an intentional single canonical route;
- ownership/XData/handle contracts for the same generated artifact;
- CI/preflight contracts that encode contradictory behavior;
- issue/claim records that still present two agents as ACTIVE owners of the same lane.

If duplicate implementations exist, choose/reconcile one canonical design deliberately and keep the rejected path out of the integrated tree.

## Required agent behavior summary

For same-prompt or overlapping-prompt races:

```text
same/overlapping owner prompt
  -> refresh current main
  -> minimal Issue/claim/branch collision check
  -> first visible valid reservation owns overlap
  -> later agent stops overlap or re-scopes
  -> no race-to-finish / no force-push-away
  -> one canonical PR/implementation only
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

This file adds a deterministic tie-break and recovery procedure for duplicate-prompt races; it does not weaken any existing branch, CI, LOCAL_ONLY, or protected-main boundary.
