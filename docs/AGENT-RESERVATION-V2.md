# Agent reservation protocol v2

Reservation v2 prevents concurrent agents from silently implementing overlapping work before a PR exists.

## Activation

`docs/agent-reservation-v2.marker` defines the activation boundary used by the machine gate.

New post-activation `agent/**` lanes must satisfy Reservation v2. Legacy carriers remain grandfathered unless explicitly migrated.

## Required reservation

Before mutating repository files for a new current-protocol lane, create one GitHub Issue containing:

```text
Lane-Key: issue-123
Reservation-Protocol: v2
Canonical owner/session: account:<github-login>|session:<globally-distinct-id>
Canonical carrier: agent/<globally-distinct-session-token>/issue-123-<scope>
Ownership-Key: <stable-semantic-key>
Expected-Paths: path/file.cs; path/other.py; src/OwnedDirectory/
```

The Issue remains open while the carrier is active. The branch must contain the same Issue number and match the declared canonical carrier.

## Identity

Scheduler labels, model names and generic roles such as `worker`, `controller`, `ChatGPT`, `Codex`, `W1` or `C01` are not sufficient repository owner identities by themselves.

Use a stable account/session identity and a globally distinguishable repository-safe branch token.

## Ownership-Key

`Ownership-Key` describes the semantic authority being changed, not merely the Issue number or worker name.

When multiple open v2 Issues claim the same semantic Ownership-Key, first visible valid ownership wins until released/reassigned/superseded.

Do not evade a collision by renaming the same work into another semantic key.

## Expected-Paths

`Expected-Paths` is a semicolon-separated set of repository-relative literal files or directory prefixes.

Use the narrowest truthful claim. If required scope expands, update the same Issue and re-check collision ownership before mutating the new path.

The current machine gate may fail closed when actual changed files fall outside the declared paths or overlap an earlier active reservation/PR.

## One active carrier

One Lane-Key has at most:

- one active owner/session;
- one canonical task branch;
- one open canonical PR.

Red, stale, queued, draft, behind-main or slow does not automatically release ownership.

A replacement carrier requires explicit release/reassignment/supersession under current repository policy.

## Machine enforcement

The shared preflight collision gate uses read-only Issue/PR metadata to enforce applicable current-protocol invariants for `agent/**` carriers, including:

- Issue binding from branch identity;
- valid Reservation v2 metadata;
- non-generic owner/session identity;
- exact Lane-Key/canonical carrier;
- semantic Ownership-Key uniqueness;
- Expected-Paths ownership and actual changed-path containment;
- overlap checks with earlier active carrier/PR metadata;
- PR Lane-Key uniqueness.

The gate never grants merge permission and never mutates repository state.

## Collision recovery

When an earlier valid owner exists:

- continue it only if it is your canonical carrier;
- otherwise do not perform overlapping mutation;
- choose genuinely non-overlapping work, or wait for explicit reassignment/supersession;
- never create a duplicate carrier solely because the earlier one is red/stale/slow.

## Terminal cleanup — mandatory

After the canonical PR merges and current `main` is verified:

1. close/complete the task Issue if it is still open;
2. change any task status that would otherwise still present the reservation as ACTIVE;
3. treat the reservation as released;
4. delete the merged task branch when practical.

A merged task must not leave an open ACTIVE reservation indefinitely.

If a task is abandoned without merging, ownership remains active until it is explicitly released, closed as not planned, reassigned or superseded. Do not infer an automatic timeout that the current machine gate does not implement.

This explicit cleanup rule prevents ghost ownership while keeping active concurrent work fail-closed.