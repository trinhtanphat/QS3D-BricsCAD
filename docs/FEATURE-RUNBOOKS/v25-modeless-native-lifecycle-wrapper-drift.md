# BricsCAD V25 modeless native lifecycle wrapper drift

## Scope

This runbook covers document-bound modeless windows whose BricsCAD managed `Document` wrapper can be replaced while the logical drawing and native database generation remain the same.

The production contract is intentionally stricter than native pointer equality:

- the native database identity must still match;
- the replacement wrapper must match the window's already-bound immutable project id and drawing fingerprint;
- wrapper ownership cannot move once document close has started or while host quiescence is active;
- semantic affinity proof runs outside the process-global native lifecycle gate, then entry/wrapper state is revalidated before subscription ownership moves;
- `BeginDocumentClose` and `CloseAborted` handlers are attached to the admitted replacement before lifecycle ownership is published;
- stale-wrapper handlers must detach before the replacement wrapper is published;
- if native unsubscribe cannot complete, the coordinator keeps the exact entry registered and records every wrapper whose detach is still pending; a retry must clear all pending native ownership before another attach/rebind can add handlers;
- a failed stale-wrapper detach rolls replacement subscriptions back; if that rollback itself cannot complete, both pending owners remain tracked rather than allowing a later retry to attach duplicates;
- initial native subscription failure follows the same rule: the entry is removed only after pending handler cleanup succeeds;
- both repeated `Attach(window, replacement)` and interaction-time live-document resolution move native lifecycle subscriptions immediately after affinity proof;
- the window lifetime updates its current managed-wrapper owner only after coordinator rebind succeeds, so a still-enumerated stale wrapper cannot regain priority;
- events arriving from a stale managed wrapper are ignored after ownership moves;
- unregister/detach targets current plus pending lifecycle wrappers and remains fail-closed/idempotent;
- when the current lifecycle document is terminally destroyed, pending stale wrappers receive a final best-effort native detach before dictionary ownership is removed, and the retired entry drops all remaining strong managed-wrapper cleanup roots even if the host refuses an unsubscribe.

## REMOTE_SAFE verification

Run the focused source contract:

```text
python scripts/preflight-v25-modeless-native-lifecycle-wrapper-drift.py
```

Then run the repository-discovered feature guards, deterministic smoke, and the V25 build with admitted/trusted BricsCAD references according to repository CI policy. Predecessor modeless guards must assert the semantic wrapper-rebind contract rather than the old readonly/same-wrapper implementation shape. A remote green result proves source/static/build compatibility only; it is not a licensed native-host PASS.

## LOCAL_ONLY licensed BricsCAD matrix

These scenarios require a real licensed BricsCAD V25 host and must remain `NO_RESULT` until executed there:

1. Open a project-bound modeless window, force/observe a legitimate managed `Document` wrapper replacement that preserves the same live native database and semantic project/drawing identity, then verify the window remains interactive and receives close lifecycle events from the replacement wrapper.
2. Repeat the window-attach path with that admitted replacement before any mouse/key/activation callback and verify lifecycle ownership moves immediately, without duplicate callbacks.
3. Cause a candidate wrapper with the same/reused native pointer but a different project id or drawing fingerprint; verify rebind fails closed and stale UI cannot mutate the different project.
4. Start ordinary document close after a successful wrapper rebind; verify `BeginDocumentClose` invalidates/closes the window exactly once.
5. Abort/veto that close; verify `CloseAborted` is delivered only from the current wrapper and cleanup does not resurrect stale subscriptions.
6. Begin application quit during/after wrapper drift; verify no subscription mutation crosses the host-quiescence barrier and quit-abort recovery remains fail-closed.
7. Close the window after rebind; verify current-wrapper handlers are removed exactly once, no duplicate handler remains on the stale wrapper, and reopening a new window does not duplicate callbacks.
8. Inject or reproduce a native event-unsubscribe failure during stale-wrapper rebind; verify replacement ownership is not published, pending wrappers remain tracked, and a retry cannot subscribe duplicates until cleanup succeeds.
9. Exercise a double-failure path where stale-wrapper detach and replacement rollback both fail; verify both wrapper owners remain tracked and no later attach loses either cleanup obligation.
10. Destroy the current lifecycle wrapper while an older wrapper still has a pending native-detach obligation; verify the stale wrapper gets a final detach attempt and the terminal coordinator entry no longer strongly retains that stale managed wrapper even if BricsCAD rejects the final unsubscribe.
11. Exercise MDI switching before and after wrapper replacement; verify project/document affinity remains pinned to the bound drawing and no selection/palette action leaks to another active document.

Record native-host evidence separately from remote CI. Do not label any of these scenarios `LOCAL_PASS` without a licensed BricsCAD V25 execution artifact.