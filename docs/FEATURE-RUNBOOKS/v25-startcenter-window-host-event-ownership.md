# V25 Start Center window host-event ownership

## Scope

This runbook covers `BltStartCenterWindow` ownership of BricsCAD `DocumentActivated` and `DocumentToBeDestroyed` event handlers. It does not cover the Start Center palette coordinator, Workspace palette, MCP runtime, updater/installer, or Quantity engine.

## Defect contract

Native event add/remove accessors are treated as fallible and potentially partially successful. A modeless window must not infer native delegate ownership from a single fully-subscribed boolean. Otherwise a partial `+=` or failed `-=` can leave BricsCAD retaining a closed WPF window while managed state falsely reports no subscription.

The production contract is:

1. `DocumentActivated` and `DocumentToBeDestroyed` have independent durable may-be-subscribed ownership bits.
2. Each ownership bit is published before its fallible native `+=`.
3. Active callback authority is separate from durable ownership and is granted only after both adds succeed.
4. Partial attach failure revokes callback authority before best-effort compensation.
5. Detach is reentrancy-fenced; ownership is cleared only after the exact matching native `-=` returns successfully.
6. Failed detach leaves ownership published. A retained callback after close or after an invalid partial generation is cleanup-only: retry detach and return before reading event document wrappers, active-document state, or queueing WPF refresh work.
7. `Closed` revokes callback authority before native teardown.
8. A later WPF `Activated` may retry subscription only when no conservative native ownership remains, so a transient fully-compensated attach failure can recover without creating duplicate handler generations.
9. Existing active-window coalescing and Record/Preserve/Suppress semantics remain unchanged.

## REMOTE_SAFE evidence

The following evidence is repository-safe and may be produced in hosted CI:

- `scripts/preflight-v25-startcenter-window-host-event-ownership.py` validates conservative ownership, active-authority separation, ordering around native add/remove, close cleanup, stale callback behavior and reentrancy fencing.
- aggregate auto-discovered feature guards remain green;
- deterministic Core smoke remains green;
- BricsCAD V25 plugin compilation uses the repository's admitted/locked trusted V25 reference generation.

These results prove source and compile contracts only. They are not licensed native runtime evidence.

## LOCAL_ONLY V25 qualification

A licensed BricsCAD V25 host is required to qualify native timing/fault scenarios. A valid local matrix should use a disposable test profile/drawing and bind evidence to one exact pushed candidate SHA/product identity:

- force or instrument a `DocumentActivated +=` failure after possible native registration; verify no UI refresh from the invalid generation and eventual exact detach;
- repeat for `DocumentToBeDestroyed +=` after the first event has attached;
- force each matching `-=` to fail once, close the Start Center window, then deliver a retained callback and prove cleanup retry without touching closed WPF/document state;
- reopen Start Center and prove one effective callback per event with no duplicate handler generation;
- exercise active-document switch, background-document close, active-document close and no-document transition; preserve Record/Preserve/Suppress behavior;
- close BricsCAD while the Start Center window is open and verify exception containment and zero QS3D-owned handler/window residue where the host permits observation.

Until that exact licensed matrix is executed, classify these native cells as `LOCAL_ONLY / NO_RESULT`. Hosted CI or admitted-reference V25 compilation must never be reported as `LOCAL_PASS`.

## Rollback / compatibility review

The change does not mutate CAD geometry, project state, sidecars, recent-project persistence contracts, updater state, or Quantity calculations. On native subscription failure it fails soft for UI availability while retaining enough ownership to clean up later. Existing Start Center UI actions, coalesced dispatcher refresh, recent-project recording, and post-native-open warning semantics remain authoritative.
