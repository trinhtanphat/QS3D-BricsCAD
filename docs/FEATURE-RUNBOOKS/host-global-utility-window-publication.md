# Host-global utility window publication — LOCAL_ONLY BricsCAD V25 qualification

This matrix qualifies issue #4852 for the host-global `QS3DPROJECTPROPERTIES` and `QS3DGEOMETRYEXT` surfaces. Hosted source checks and compile validation are not licensed BricsCAD runtime evidence.

## Source contract

- Project Properties and Geometry Extensions each retain one independent authoritative host-global window publication.
- Repeated invocation with a loaded owner activates/reuses the same instance instead of allocating another WPF surface.
- A stale/unloaded owner is released only when it is still the exact published instance.
- The exact-instance `Closed` callback is attached before host show and cannot release a later successor.
- `Application.ShowModelessWindow(...)` must return with `window.IsLoaded == true` before authoritative publication.
- Until publication succeeds, local candidate ownership is retained so catch cleanup can close failed/unloaded candidates.
- Project Properties remains the dedicated BLT3D read-only placeholder and must not acquire project/document persistence semantics.
- Geometry Extensions remains host-global: each button click resolves the current `MdiActiveDocument` and sends the chosen BricsCAD command to that active drawing. Publication lifecycle must not pin the drawing active when the launcher was first opened.

## Hosted/source validation

Run:

```text
python scripts/preflight-blt3d-project-properties.py
python scripts/preflight-geometry-extension-ui.py
python scripts/preflight-host-global-utility-window-publication.py
```

Then require repository Shared CI and locked-reference V25 compile validation. Green hosted validation is still not `LOCAL_PASS`.

## LOCAL_ONLY licensed V25 matrix

Use one compatible licensed BricsCAD V25 Windows x64 host and one exact integrated plugin SHA/product identity.

1. **Project Properties first show** — with drawing A active, run `QS3DPROJECTPROPERTIES`; require one loaded read-only placeholder with the expected BLT3D text.
2. **Project Properties repeated invoke** — invoke repeatedly; require the existing loaded surface to activate and require no second Project Properties window.
3. **Project Properties close/reopen** — close it normally, invoke again, and require exactly one fresh loaded successor with no stale owner effect.
4. **Geometry Extensions first show** — run `QS3DGEOMETRYEXT`; require exactly one loaded launcher.
5. **Geometry Extensions repeated invoke** — invoke repeatedly while loaded; require activate/reuse and no duplicate launcher.
6. **Active-DWG dispatch** — open Geometry Extensions while drawing A is active, switch to drawing B, then invoke a safe review/non-mutating button from the launcher. Require dispatch to drawing B, proving the host-global publisher did not pin drawing A. Choose a command whose local qualification side effects are understood and reversible.
7. **Geometry close/reopen** — close the launcher and reopen; require exactly one fresh successor and no stale Closed callback clearing the new owner.
8. **Host show returns without Loaded** — only if an evidence-backed local harness or real host condition can make modeless show return without leaving the WPF window Loaded. Require no success publication and cleanup of the candidate. If this cannot be reproduced safely, record `NO_RESULT`; do not simulate runtime evidence from source structure.
9. **Host show exception** — if an evidence-backed harness can induce a show exception, require candidate cleanup and no ghost owner; otherwise `NO_RESULT`.
10. **Resource/lifetime cleanup** — after repeated open/close cycles, verify there are no extra visible utility windows, stale usable callbacks, or unexpected owned helper processes/private-state residue.
11. **Project Properties semantic regression** — verify the placeholder remains read-only and does not open broad Project Tools or create/mutate QS3D project state.
12. **Geometry command inventory regression** — verify the existing extension buttons remain available and command dispatch behavior is unchanged apart from launcher publication lifecycle.

## Evidence and verdict

Record exact source SHA, plugin ProductVersion/hash, BricsCAD V25 version, sanitized drawing identities and an observed `PASS`, `FAIL`, or `NO_RESULT` for every executed row. Do not call source/preflight/compile success licensed runtime evidence and never fabricate `LOCAL_PASS` for rows not actually observed in the licensed host.
