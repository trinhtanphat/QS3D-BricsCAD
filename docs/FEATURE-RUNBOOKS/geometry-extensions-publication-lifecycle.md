# Geometry Extensions publication lifecycle

## Source contract

`QS3DGEOMETRYEXT` owns at most one published `GeometryExtensionsWindow` and at most one failed-publication pending candidate. A live published owner is reused and activated. A stale published owner is released only by exact reference identity.

Before constructing a new candidate, the command drains any exact pending candidate. If that candidate remains loaded after a best-effort terminal close, the command fails closed and does not create another window. This prevents a host-show failure or close veto from turning into duplicate modeless surfaces.

A fresh candidate becomes `_pending` before its `Closed` handler is attached and before `Application.ShowModelessWindow`. Publication occurs only after the host call returns and `IsLoaded` is true. Ownership then transfers to `_published`, pending ownership is released, and local cleanup ownership is cleared. The `finally` path attempts cleanup only while the local candidate remains unpublished. Exact `Closed` release clears only matching pending/published ownership.

The window continues to resolve `Application.DocumentManager.MdiActiveDocument` at button-click time so its launcher remains active-document dispatched rather than pinned to the drawing that originally opened the surface. User-visible status does not echo raw host exception messages; command-line diagnostics are limited to the exception type.

## Remote validation

Run `python scripts/preflight-geometry-extension-ui.py` and `python scripts/preflight-host-global-utility-window-publication.py`. The historical host-global guard retains the Project Properties publication contract while validating Geometry Extensions against its stronger pending-owner failure-clean lifecycle. Shared CI also runs the aggregate feature guard suite and compiles the V25 plugin against trusted locked references. These checks are source/static/compile evidence only.

## LOCAL_ONLY licensed V25 matrix

Use the exact pushed candidate in licensed BricsCAD V25 Windows x64:

1. Open `QS3DGEOMETRYEXT`; confirm one loaded surface.
2. Invoke again while loaded; confirm the same surface activates and no duplicate appears.
3. Close the surface and reopen; confirm a fresh single owner appears.
4. With two drawings open, open the surface in A, activate B, click representative Geometry Extensions actions, and confirm command dispatch targets B at click time.
5. Exercise normal wall/opening/rebar launcher buttons and verify command submission/status behavior.
6. Using an evidence-backed harness that can induce `ShowModelessWindow` failure after a candidate becomes loaded, verify best-effort cleanup. If the candidate cannot terminally close, invoke again and verify no second candidate is created. If the failure shape cannot be induced, record `NO_RESULT`, not PASS.
7. Using an evidence-backed close-veto/failure probe for a failed pending candidate, verify reinvocation remains fail-closed until that exact candidate reaches terminal close.
8. Verify raw host exception text/path details are not reflected into the modeless status surface; command-line diagnostic may report only exception type.
9. Repeat close/reopen and active-document dispatch after save/cold-reopen where practical; verify no stale duplicate surface remains.

Do not infer or report `LOCAL_PASS` from hosted CI, source inspection, or the locked-reference V25 build.
