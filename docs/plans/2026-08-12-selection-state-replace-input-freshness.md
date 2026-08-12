# SelectionState replacement input freshness

## Problem

`SelectionState.Replace(IEnumerable<string>)` executes caller-controlled lazy enumeration before applying the materialized replacement. The enumerable can reentrantly mutate the same `SelectionState` through `Replace()` or `Clear()`. Without a freshness token, the outer replacement can overwrite that newer effective selection using stale input.

## Invariant

- Maintain a private monotonic revision representing effective selection changes.
- Capture the revision immediately before replacement target enumeration.
- After target materialization, reject revision drift before `SetEquals`, clearing, unioning, or firing the outer `Changed` event.
- Increment the revision only for effective `Replace` and `Clear` mutations.
- Compute the next revision before mutating `_ids`, so revision overflow fails atomically.
- Preserve the existing 10,000 target cap, blank/null skip behavior, trimming, case-insensitive de-duplication, stable-input behavior, and no-op event suppression.

## Regression

Add deterministic Core smoke coverage for:

1. stable lazy replacement;
2. reentrant effective replacement during lazy enumeration, preserving the newer inner selection;
3. reentrant effective mutation followed by an empty outer enumeration;
4. a reentrant no-op mutation, demonstrating that only effective mutations invalidate the outer replacement.

Register the smoke with `ModuleInitializer` and add a static preflight locking source ordering, effective revision advancement, smoke cases, and registration.

## Scope exclusions

No cross-thread synchronization contract, BricsCAD implied-selection behavior, Selection Inspector behavior, persistence format, or runtime host changes.
