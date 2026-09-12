# BricsCAD V25 repeated Direct Draw document-generation affinity

## Scope

This runbook covers `DirectDrawRepeatedCommands` for repeated Wall/Beam and Active Family repeated authoring. It is C03 UI/authoring scope. It does not change MCP, installer/release, Quantity, or Direct Draw canonical transaction ownership.

## Defect

Repeated Direct Draw keeps one managed `Document`, project/family preview, drawing units, UCS and segment geometry across `Editor.GetPoint` and `Editor.Drag`. Managed MDI identity and document-deactivation events are insufficient when BricsCAD replaces/reloads the native database while retaining the same managed `Document` wrapper. In that case stale preview/defaults/geometry could otherwise cross into `ExecuteDirect` or later UI/result publication.

## Production contract

At command admission capture the non-zero `document.Database.UnmanagedObject` identity. Carry it through `RunCore` and require both the same managed active `Document` and the same native database identity:

- after the first point prompt;
- before each preview/default-resolution cycle;
- after each `Editor.Drag` host-pumping boundary;
- immediately before `DirectDrawCommands.ExecuteDirect`;
- after command-level checkpoint publication and refreshed project preview;
- before status/result/error publication after accepted work.

The existing project/family, units, UCS, model-space, document-deactivation, semantic snapshot, command-level Undo checkpoint and rollback contracts remain authoritative. A stale generation must fail closed and must not publish through a stale editor/palette. Already committed/checkpointed segments are not reported as rolled back merely because later UI publication becomes stale.

## REMOTE_SAFE verification

Run the auto-discovered source preflight:

```text
python scripts/preflight-v25-repeated-directdraw-document-generation.py
```

Then run the repository aggregate feature preflights, deterministic smoke tests, trusted/admitted BricsCAD V25 reference admission, and locked-reference V25 plugin build. Fresh exact-head CI is required after any reconcile.

## LOCAL_ONLY qualification

A real licensed BricsCAD V25 runtime is required to qualify:

1. MDI A -> B -> A while a repeated command is suspended in `GetPoint` or `Drag`.
2. Same managed `Document` wrapper with native DB reload/replacement during a prompt/drag.
3. Enter/ESC/cancel before the first segment and after one or more accepted segments.
4. Invalid coincident endpoint retry.
5. Whole-command Undo/Redo after multiple accepted segments.
6. Active Family/project change between accepted segments.
7. Document close/deactivation while the lifecycle guard is subscribed.
8. Visible result/status/palette behavior after a committed segment when publication pumps/reenters the host.

Do not label these scenarios `LOCAL_PASS` without executing them in licensed BricsCAD. Remote source/preflight/build green is not native runtime qualification.

## Rollback and ownership self-review

`ExecuteDirect` remains the canonical per-segment native/semantic mutation owner. `SourceReconcileUndoCoordinator` remains the command-level checkpoint owner. The generation fence does not open an additional database transaction or take ownership of native rollback. `RepeatedDocumentLifecycleGuard` still owns exactly one `DocumentToBeDeactivated` subscription and compensating detach. Runtime qualification observers remain exception-isolated and non-authoritative.
