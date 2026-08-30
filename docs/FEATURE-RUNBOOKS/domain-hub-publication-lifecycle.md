# Domain Hub transactional publication lifecycle

## Scope

`QS3DDOMAIN` is intentionally a host-global modeless command hub. `DomainHubWindow` does not retain a BricsCAD `Document`; each button resolves `Application.DocumentManager.MdiActiveDocument` at click time and dispatches to that active drawing. This package hardens only the launch/publication lifecycle and does not introduce document ownership or change any underlying domain command semantics.

## Proven defect

The historical launcher assigned a freshly constructed `DomainHubWindow` into static `_window` before `Application.ShowModelessWindow(...)` succeeded and attached a `Closed` callback that unconditionally cleared `_window`. A show exception or non-loaded return could therefore leave an abandoned candidate with a live callback. If a later invocation published a newer window, a delayed `Closed` from the abandoned candidate could clear the newer owner.

## Source-ready contract

- An already loaded published Domain Hub is reused/activated; repeated invocation does not construct a duplicate.
- A stale unloaded publication is cleared only through an exact-owner release helper.
- A new candidate attaches an exact-window `Closed` callback before host show.
- The candidate is not published until `Application.ShowModelessWindow` returns successfully and `IsLoaded` is still true.
- A show exception or non-loaded return leaves static publication unchanged/unowned.
- Terminal `Closed` clears publication only when the closing window is still the exact published owner; stale callbacks cannot clear a newer owner.
- The window remains host-global and retains no managed `Document`, database, `ObjectId`, `DBObject`, or native entity ownership. Command buttons continue resolving the active document at click time.

## Hosted validation

Run Shared CI on one exact pushed candidate. Reservation/Lane-Key/path-collision admission, generic guard, all discovered feature guards, deterministic Core smoke, trusted BricsCAD V25 reference validation, V25 plugin compilation, and final build must remain green. The focused `scripts/preflight-domain-hub-publication-lifecycle.py` pins candidate publication ordering, exact-owner terminal release, and the host-global active-document dispatch boundary.

## LOCAL_ONLY licensed V25 matrix

On one exact source/plugin identity in licensed BricsCAD V25, using disposable drawings only:

1. Invoke `QS3DDOMAIN` repeatedly and prove exactly one Domain Hub remains live and subsequent invocations activate/reuse it.
2. With drawing A active, click representative hub commands and prove dispatch targets A. Switch to drawing B without recreating the hub and prove subsequent clicks target B.
3. Close the hub normally, reopen it, and repeat rapid close/reopen sequencing; prove a terminal callback from an older window never clears the newer live owner.
4. With an authorized harness/probe, exercise a modeless-show exception or a candidate that returns non-loaded; prove no failed candidate is published and the next normal invocation can open exactly one hub.
5. Close all drawings while the hub remains available; command clicks must fail softly with the existing no-active-drawing status and must not create or retain a document implicitly.
6. Reopen a disposable drawing and prove the same host-global hub can dispatch again to the newly active document.
7. Close the hub and host; verify zero owned modeless/process/private-state residue remains.

Hosted source/static/build evidence is never `LOCAL_PASS`. Record only sanitized exact-SHA/ProductVersion licensed evidence through the canonical local qualification flow.
