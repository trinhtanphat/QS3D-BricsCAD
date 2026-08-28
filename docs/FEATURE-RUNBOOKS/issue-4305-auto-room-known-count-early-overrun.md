# Issue #4305 — Auto Room known-count processing boundary

Lane-Key: `issue-4305`

## Problem

`AutoRoomLifecycle.MarkStaleForSelection` consumes two counted `ISet<string>` inputs: active Room ids and selected source handles. The method already captured each set's advertised `Count` and rejected final Count/traversal disagreement. Before #4305, an over-yielding counted set could let entries beyond the accepted Count reach trimming, handle normalization, and temporary semantic-set processing before the mismatch was reported.

The selected-source path also has an independent hard security/resource bound of 5,000 entries. Existing regression `AutoRoomStaleSelectionBoundSmoke.DishonestCountStopsAtFirstDisallowedEntry` requires a lying small Count not to mask that bound: a source that reports Count 1 but yields more than 5,000 entries must still fail on entry 5,001 with the established capacity error.

## Contract

For active Room ids:

1. capture the advertised Count before traversal;
2. before processing each yielded item, reject when `observedCount >= knownCount`;
3. retain the final equality check so under-yield stays fail-closed.

For selected source handles:

1. capture and preflight the advertised Count; Count > 5,000 still fails before enumeration;
2. during traversal, enforce the independent 5,000-entry streaming bound first;
3. once traversal exceeds the advertised Count, keep counting entries but `continue` before whitespace handling, `GeneratedHandleIdentity.Normalize`, or insertion into the selected semantic set;
4. if traversal stays within the hard bound, the final equality check rejects the known-Count drift;
5. if traversal reaches entry 5,001, the hard-bound error wins exactly as before.

This ordering means dishonest Count metadata can neither authorize semantic processing beyond its own claim nor weaken the independent 5,000-entry resource bound.

`NormalizeSourceHandles(IEnumerable<string>)` remains a general streaming helper governed by its independent 5,000-entry cap; this lane does not invent a Count contract for arbitrary `IEnumerable<string>` sources.

## Regression

`AutoRoomLifecycleKnownCountTraversalSmoke` now proves:

- active-room over-yield stops at the first unexpected item (`MoveNextCalls == 2` for Count 1);
- selected-source in-bound over-yield is fully traversed only for cardinality accounting, then rejected by the final known-Count check;
- exact-count inputs preserve existing no-op semantics;
- advertised selected-source Count > 5,000 still fails before `GetEnumerator()`.

The pre-existing `AutoRoomStaleSelectionBoundSmoke` remains unchanged and authoritative for a dishonest Count that actually crosses the hard bound: it must stop on entry 5,001 and must not consume entry 5,002.

`scripts/preflight-auto-room-known-count-early-overrun.py` locks the active early guard, selected hard-bound-before-drift ordering, the selected `continue` before handle normalization, both final under-yield checks, and both regression suites.

## Validation boundary

This is Core Auto Room lifecycle/data-integrity hardening only. No BricsCAD host, private DWG, signing evidence, package publication, or `LOCAL_PASS` applies. Landing requires deterministic Core smoke and the repository's protected current-candidate `preflight` and `core` checks.
