# BricsCAD V25 palette visibility rollback

## Scope

Lane C03 owns the BricsCAD V25 Workspace/modeless palette lifecycle in `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`. This carrier fixes partial palette visibility state when a native `PaletteSet.Visible` setter fails during a multi-palette transition.

## Invariant

A visibility transition over Workspace, Properties, Drawing/Layer, and Quantity Insight is one owner-facing operation. Before the first mutation, capture the exact currently-published `PaletteSet` instances and each prior `Visible` value. Apply only to the same still-current instances. If any native setter throws, best-effort restore prior visibility in reverse order, again only when the captured instance is still the current published instance, then rethrow the original failure.

Rollback must not dispose/recreate palettes, mutate project/CAD state, acquire document locks/transactions, dispatch commands, persist layout, or schedule deferred work. A rollback setter failure is contained so restoration of the remaining palettes continues.

## Deterministic verification

Run:

```text
python scripts/preflight-v25-palette-visibility-rollback.py
python scripts/preflight-all.py
```

The dedicated preflight checks snapshot-before-mutation ordering, exact-instance/ABA fences, reverse best-effort rollback, original-failure rethrow, and the absence of project/CAD mutation couplings.

Build BricsCAD V25 with the repository's admitted/locked reference workflow and run the deterministic smoke suite required by CI.

## Runtime classification

- `REMOTE_SAFE`: source preflight, aggregate source guards, deterministic smoke, admitted-reference V25 compile.
- `LOCAL_ONLY`: licensed BricsCAD V25 fault injection where a native `PaletteSet.Visible` setter throws after one or more earlier setters succeed.

Do not report `LOCAL_PASS` unless that licensed scenario is actually exercised.

## Manual licensed scenario

With the four QS3D palettes created, inject or reproduce a failure in a later native `Visible` setter during a transition. Verify earlier palettes return to their exact prior visibility where their captured instances remain current; verify later rollback failures do not stop other rollback attempts; verify the original transition reports failure and no project/document geometry state changes.
