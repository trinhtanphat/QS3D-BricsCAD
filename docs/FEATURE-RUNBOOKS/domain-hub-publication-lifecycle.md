# Domain Hub transactional publication lifecycle

## Scope

`QS3DDOMAIN` is intentionally a host-global modeless command hub. `DomainHubWindow` does not retain a BricsCAD `Document`; each button resolves `Application.DocumentManager.MdiActiveDocument` at click time and dispatches to that active drawing. This package hardens only the launch/publication lifecycle and does not introduce document ownership or change any underlying domain command semantics.

## Proven defects

The historical launcher assigned a freshly constructed `DomainHubWindow` into static `_window` before `Application.ShowModelessWindow(...)` succeeded and attached a `Closed` callback that unconditionally cleared `_window`. A show exception or non-loaded return could therefore leave an abandoned candidate with a live callback. If a later invocation published a newer window, a delayed `Closed` from the abandoned candidate could clear the newer owner.

Issue #4761 corrected authoritative publication and exact-owner release, but the successor source still returned on a non-loaded candidate or entered the outer exception handler after a show exception without terminally closing that unpublished candidate. Static ownership stayed correct, yet the failed WPF/native-host candidate and callback/resources were not source-proven to be released. Issue #4878 closes that resource-lifetime gap by keeping the candidate locally cleanup-owned until publication succeeds.

## Source-ready contract

- An already loaded published Domain Hub is reused/activated; repeated invocation does not construct a duplicate.
- A stale unloaded publication is cleared only through an exact-owner release helper.
- A new candidate is held in a local cleanup slot before construction/host show and attaches an exact-window `Closed` callback before host show.
- The candidate is not published until `Application.ShowModelessWindow` returns successfully and `IsLoaded` is still true.
- A show exception or non-loaded return leaves static publication unchanged and the `finally` path best-effort terminal-closes only the still-unpublished candidate.
- Cleanup ownership transfers only after `_window = window` authoritative publication. The cleanup helper refuses to close the exact currently published owner.
- Terminal `Closed` clears publication only when the closing window is still the exact published owner; stale callbacks cannot clear a newer owner.
- The window remains host-global and retains no managed `Document`, database, `ObjectId`, `DBObject`, or native entity ownership. Command buttons continue resolving the active document at click time.

## Hosted validation

Run Shared CI on one exact pushed candidate. Reservation/Lane-Key/path-collision admission, generic guard, all discovered feature guards, deterministic Core smoke, trusted BricsCAD V25 reference validation, V25 plugin compilation, and final build must remain green. The focused `scripts/preflight-domain-hub-publication-lifecycle.py` pins local candidate cleanup ownership, candidate publication ordering, cleanup transfer after publication, exact-owner terminal release, and the host-global active-document dispatch boundary.

## LOCAL_ONLY licensed V25 matrix

On one exact source/plugin identity in licensed BricsCAD V25, using disposable drawings only:

1. Invoke `QS3DDOMAIN` repeatedly and prove exactly one Domain Hub remains live and subsequent invocations activate/reuse it.
2. With drawing A active, click representative hub commands and prove dispatch targets A. Switch to drawing B without recreating the hub and prove subsequent clicks target B.
3. Close the hub normally, reopen it, and repeat rapid close/reopen sequencing; prove a terminal callback from an older window never clears the newer live owner.
4. With an authorized harness/probe, exercise a modeless-show exception; prove the unpublished candidate is terminally closed, no static owner is published, and the next normal invocation opens exactly one hub.
5. With an authorized harness/probe, exercise a host-show path that returns with `IsLoaded == false`; prove the unpublished candidate is terminally closed and no window/callback resource residue remains.
6. Prove cleanup ownership transfers after successful publication: a successfully loaded/published hub must remain live and must not be closed by the launch `finally` path.
7. Close all drawings while the hub remains available; command clicks must fail softly with the existing no-active-drawing status and must not create or retain a document implicitly.
8. Reopen a disposable drawing and prove the same host-global hub can dispatch again to the newly active document.
9. Close the hub and host; verify zero owned modeless/process/private-state residue remains.

Hosted source/static/build evidence is never `LOCAL_PASS`. Record only sanitized exact-SHA/ProductVersion licensed evidence through the canonical local qualification flow.
