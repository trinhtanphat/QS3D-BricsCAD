# Progress snapshot transient known-Count stability

## Scope

This runbook validates the remote-safe Core contract for `ProgressDomainContract.Snapshot<T>` when a caller-controlled enumerable exposes deterministic Count metadata through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`.

The accepted Count contract is immutable for the full traversal. A source that changes, conflicts, becomes negative, or exceeds the supported ceiling at any traversal boundary must fail closed before the item under changed metadata is observed through `IEnumerator.Current`.

## Required traversal ordering

For counted inputs, the implementation must preserve this order for every iteration:

1. bind all supported Count surfaces before caller-controlled `MoveNext`;
2. execute `MoveNext`;
3. after a successful `MoveNext`, bind all supported Count surfaces again;
4. reject known-Count overrun and the 10,000-entry ceiling;
5. only then read `Current`, validate null semantics, and retain the item;
6. after traversal, retain the under-yield check and perform a final Count rebound before publication.

Streaming inputs without supported Count metadata remain supported.

## Deterministic regression evidence

`ProgressSnapshotCountStabilitySmoke` includes hostile counted enumerables that transiently grow, shrink, or report a negative Count immediately after a successful `MoveNext`. Each case must fail before `Current` and asserts `CurrentReads == 0`. Existing coverage remains authoritative for N+1 no-Current behavior, under-yield rejection, post-traversal uniform drift/conflict, stable counted inputs, and streaming inputs.

## Source guards

Run:

```text
python scripts/preflight-progress-snapshot-count-stability.py
python scripts/preflight-progress-snapshot-transient-count-stability.py
```

Both scripts are auto-discovered by aggregate feature preflight. They lock the boundary ordering and reject regression to `while (enumerator.MoveNext())`, which cannot interpose the pre-`MoveNext` Count stability check.

## Acceptance

This lane is `REMOTE_SAFE` / runtime `NOT_APPLICABLE`. Acceptance requires deterministic Core smoke plus repository-selected branch/protected CI. Do not claim licensed BricsCAD `LOCAL_PASS` for this feature.
