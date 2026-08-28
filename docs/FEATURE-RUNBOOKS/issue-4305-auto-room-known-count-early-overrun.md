# Issue #4305 — Auto Room known-count early overrun

Lane-Key: `issue-4305`

## Problem

`AutoRoomLifecycle.MarkStaleForSelection` consumes two counted `ISet<string>` inputs: active Room ids and selected source handles. The method already captures each set's advertised `Count` and rejects final Count/traversal disagreement. Before #4305, an over-yielding counted set could still let item `knownCount + 1` reach trim/normalization/temporary-set processing before the final mismatch was reported.

## Contract

For both counted inputs:

1. capture the advertised Count before traversal;
2. before processing each yielded item, reject when `observedCount >= knownCount`;
3. only then increment the observed count and perform semantic processing;
4. retain the selected-source hard cap of 5,000 inputs, including pre-enumeration rejection when advertised Count already exceeds the cap;
5. after enumeration, keep `RequireKnownCountMatchesTraversal(...)` so under-yield remains fail-closed;
6. preserve the existing project `ChangeVersion` freshness check before stale-room mutation;
7. exact-count inputs retain current canonical trimming/handle normalization and no-op behavior.

The early guard intentionally operates on the counted stale-selection API only. `NormalizeSourceHandles(IEnumerable<string>)` remains a general streaming helper governed by its independent 5,000-entry cap; this lane does not invent a Count contract for arbitrary `IEnumerable<string>` sources.

## Regression

`AutoRoomLifecycleKnownCountTraversalSmoke` now includes `OverYieldFailsAtFirstUnexpectedItem`. Its counted sets advertise Count 1 but contain three items. The assertion requires exactly two `MoveNext` calls: one valid item plus the first unexpected item. The old post-enumeration-only behavior would continue traversing the remaining items before failing.

`scripts/preflight-auto-room-known-count-early-overrun.py` locks both call sites, ordering before semantic processing, the final under-yield checks, the immediate-enumeration smoke, exact-count control, and the >5,000 pre-enumeration control.

## Validation boundary

This is Core Auto Room lifecycle/data-integrity hardening only. No BricsCAD host, private DWG, signing evidence, package publication, or `LOCAL_PASS` applies. Landing requires deterministic Core smoke and the repository's protected current-candidate `preflight` and `core` checks.
