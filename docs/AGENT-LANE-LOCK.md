# Agent Lane Lock and Canonical Carrier Contract

This document defines the collision-prevention contract for concurrent AI agents, scheduled workers, controller lanes, local agents and ordinary chat sessions.

It supplements `AGENTS.md` and `docs/AGENT-WORK-REGISTRATION.md`. When older coordination comments or handoffs allow broader replacement/fallback behavior, this stricter contract wins unless the repository owner explicitly overrides it for a named task.

## Core invariant

Every concrete task has one stable **Lane-Key** and at most one active implementation carrier.

For ordinary issue-backed work, use:

```text
Lane-Key: issue-123
```

For an explicitly authorized integration batch, use a stable batch key such as:

```text
Lane-Key: batch-release-2026-08-17
```

A Lane-Key owns one semantic task, not merely one branch name.

At any moment a Lane-Key may have:

- one ACTIVE owner/session;
- one canonical task branch;
- zero or one open canonical PR;
- explicitly recorded superseded/closed historical carriers.

It must not have two competing active implementations or two open PR carriers.

## Before mutation

Before editing code, creating a replacement branch, or opening a new task Issue:

1. refresh exact current `origin/main`;
2. search open Issues and PRs for the intended semantic behavior, not only title keywords;
3. inspect the expected production files and current first-visible reservations;
4. determine the Lane-Key before implementation starts;
5. find the current canonical carrier for that Lane-Key, if any;
6. stop without mutation if an equivalent active carrier already exists.

The required stop status is:

```text
DUPLICATE_CARRIER / NO MUTATION
```

A newer baseline, cleaner implementation, greener CI, different agent model, different chat session, or easier merge does not create a second ownership right.

## Stale, red, behind and superseded work

A branch or PR that is stale, behind `main`, red, queued, blocked, or inconvenient is still owned until explicitly released or superseded.

Do not create a competing carrier just to obtain a cleaner history or a new CI run.

If the canonical carrier needs current-main reconciliation, prefer one of these paths:

1. update the same task branch non-force and keep the same canonical PR; or
2. if rebuilding from current `main` is genuinely required, explicitly record that the old carrier is superseded, close the old PR if open, create exactly one replacement carrier, and preserve the same Lane-Key.

There must never be an intentional overlap window with two open canonical PRs for one Lane-Key.

## No transport or reconciliation PRs

Do not create a pull request whose only purpose is to merge/replay `main`, another task branch, or a temporary carrier into the canonical agent branch.

Use local/non-force Git reconciliation on the canonical task branch, or rebuild one fresh canonical carrier after explicit supersession.

Stacked PRs are allowed only when the task genuinely has an explicitly documented dependency stack; they must not be used as a synchronization mechanism.

## Controller and scheduled-worker rule

CONTROL is the only lane that may issue replacement assignments within a controller round.

If W01-W04 discovers that its assigned task is already landed, closed, stale, blocked, zero-defect, or otherwise non-executable, it must report:

```text
STALE_OR_BLOCKED / NO MUTATION
```

and stop. The worker must not invent or reserve a replacement package on its own.

CONTROL may issue a replacement only after refreshing current `main`, re-running the collision scan, assigning a fresh Lane-Key, and recording the replacement explicitly on the control board.

## Umbrella audit rule

Umbrella Issues such as a project-wide audit are not Lane-Keys for every discovered fix.

Each concrete implementation discovered under an umbrella must first receive its own unique Issue/Lane-Key. If another session already owns the equivalent concrete behavior, the discovering session stops instead of creating a second concrete issue or carrier.

## PR metadata

Every agent/integration PR must state these fields near the top of the body:

```text
Lane-Key: issue-123
Canonical owner/session: <agent-or-session-id>
Canonical carrier: agent/<agent-id>/<scope>
Supersedes: none
```

If a carrier is explicitly superseded, `Supersedes` must name the prior PR/branch and the old open PR must be closed before the new carrier is represented as canonical.

The shared preflight validates Lane-Key uniqueness for agent/integration PRs. A duplicate open Lane-Key is a CI failure, not a merge-conflict warning.

## File overlap is still a collision signal

Different Lane-Keys do not automatically make overlapping edits safe.

If another active lane owns the same production file or exact semantic boundary, stop and coordinate before mutation. Shared registration/index files may be touched only when required and must not be used to hide overlapping production ownership.

A clean textual merge does not prove semantic non-overlap.

## Main already contains the behavior

If current `main` already contains equivalent behavior, do not replay the historical branch or recreate the fix. Record the task as already integrated/stale and release the lane.

The goal is one winning implementation, not one implementation per agent/session.