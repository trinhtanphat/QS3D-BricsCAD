# Project metadata persistence-source reentrancy integrity

## Scope

This runbook covers deterministic Core validation for `ProjectMetadataDictionary.ReplacePersistenceState` when caller-controlled persistence input callbacks mutate the same target metadata. Licensed BricsCAD runtime qualification is not applicable.

## Integrity contract

Persistence replacement stages caller input into a detached dictionary and publishes only after traversal/count/reserved-state validation. That detached staging must also be optimistic with respect to the existing metadata target: no Count, `MoveNext`, or `Current` callback may mutate the target and then be silently overwritten by the stale outer replacement.

`ProjectMetadataDictionary` therefore maintains an internal mutation generation for every actual metadata write, including public/owned/persistence set/remove/clear operations and persistence replacement. `ReplacePersistenceState` captures that generation at admission and revalidates it across caller-controlled Count/traversal boundaries and immediately before publication. If reentrancy changes the target, the newer mutation remains authoritative and the outer replacement fails closed.

Existing contracts remain unchanged: maximum 10,000 entries; negative/conflicting/transient Count rejection; overrun-before-Current and under-yield handling; duplicate/null key validation; reserved metadata validation; semantic dirty-state behavior; and detached publication after validation.

## Deterministic regression

`ProjectMetadataPersistenceSourceReentrancySmoke` proves two hostile boundaries:

- a counted one-item input mutates target metadata from its seventh/final Count observation; outer publication must fail and preserve both the original target entry and the nested mutation;
- a streaming input mutates target metadata from its first `MoveNext`; failure must occur before `Current` and preserve the nested mutation.

A stable counted control pins seven Count observations, two `MoveNext` calls, one `Current`, and normal replacement publication.

## Source guard

Run:

```text
python scripts/preflight-project-metadata-persistence-source-reentrancy.py
```

The auto-discovered guard pins the target mutation generation, final Count -> target validation -> publication ordering, and `MoveNext`/`Current` target checks. It supplements rather than weakens existing project-metadata Count guards.

## Hosted acceptance

Run Shared CI on the exact candidate. Required terminal evidence is protected `preflight=SUCCESS` and `core=SUCCESS`, including deterministic smoke and the normal Core/V25 compile build chain. Reconcile latest protected main non-force if freshness requires it, merge only through expected-head PR authorization, and verify exact protected main.
