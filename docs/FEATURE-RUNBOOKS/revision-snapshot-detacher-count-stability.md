# Revision snapshot detacher map Count stability

## Scope

`RevisionSnapshotDetacher.CopyMap` snapshots mutable revision property/quantity maps before public comparison. A supported map Count is an admitted integrity boundary and must remain stable across caller-controlled enumeration callbacks.

## Fail-closed traversal contract

For a counted map, the detacher reads the admitted Count, validates the 100,000-entry ceiling, acquires `GetEnumerator`, and immediately revalidates Count before the first traversal call. It then revalidates Count immediately after every `MoveNext` before reading `Current`, and immediately after `Current` before publication into the detached map. Final observed cardinality and Count must still match.

A hostile collection that changes Count during `GetEnumerator` therefore receives zero `MoveNext` calls. Drift during `MoveNext` is rejected before `Current`. Drift during `Current` is rejected before publication. No retry loop is used.

## Preserved behavior

Stable dictionaries remain accepted. Existing null handling, duplicate destination-key behavior, deterministic detached values, element/list capture behavior, 100,000-entry resource ceiling, and downstream revision semantic validation remain unchanged.

## Evidence boundary

This package is deterministic Core revision-integrity work. Licensed BricsCAD runtime is not applicable and no `LOCAL_PASS` claim is required.
