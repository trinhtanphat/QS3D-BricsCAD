# SelectionState final Count reentrancy integrity

## Scope

This runbook covers deterministic Core validation for `SelectionState.Replace(IEnumerable<string>)` when a supported known-Count source invokes caller code from its final Count getter. Licensed BricsCAD runtime qualification is not applicable.

## Integrity contract

`Replace` is an optimistic transaction over the current semantic selection. It captures the current selection version before enumerating caller input. Every caller-controlled traversal/Count callback must complete without allowing a newer selection mutation to be overwritten by the stale candidate snapshot.

The transaction therefore requires stable known Count around traversal and after `Current`, checks the captured selection version after traversal, performs the final known-Count observation, and then checks the same captured selection version again before cardinality validation/no-op comparison/publication. If the final Count callback re-enters the same `SelectionState` and changes it, the nested mutation remains authoritative and the outer stale replacement fails closed.

Existing contracts remain unchanged: supported Count interfaces must agree and remain non-negative and at most 10,000; overrun is rejected before `Current`; under-yield and Count drift fail closed; whitespace is normalized; IDs deduplicate case-insensitively; streaming inputs remain supported; no-op replacement emits no event; version increments are checked.

## Deterministic regression

`SelectionStateFinalCountReentrancySmoke` uses a one-item counted collection whose seventh Count observation is the final post-traversal Count read. That getter re-enters the target state and publishes `INNER`. The outer replacement must throw the existing selection-changed error, emit no outer publication, and leave `INNER` authoritative. A stable one-item counted control pins the seven-read observation budget, single `Current`, two `MoveNext` calls, normalization, and one `Changed` event.

## Source guard

Run:

```text
python scripts/preflight-selection-final-count-reentrancy.py
```

The auto-discovered guard pins final Count -> version revalidation -> publication ordering and the hostile/stable regression tokens. It supplements, rather than weakens, the existing SelectionState Count and reentrancy guards.

## Hosted acceptance

Run the Shared CI on the exact candidate. Required terminal evidence is `preflight=SUCCESS` and `core=SUCCESS`, including deterministic smoke and the normal protected Core/V25 compile build chain. Reconcile latest protected main non-force if freshness requires it, then merge only through expected-head PR authorization and verify exact protected main.
