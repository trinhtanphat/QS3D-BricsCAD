# Project Information palette — licensed-local qualification

Status: `LOCAL_ONLY / NO_RESULT`

This package completes the native `QS3D — Thông tin dự án` palette as a read-only view over the QS3D project belonging to the active BricsCAD drawing. Remote source/CI validation must not be reported as licensed BricsCAD runtime evidence.

## Invariants

- The surface is read-only: it must not create, mutate, regenerate, save, or touch `ProjectState`.
- Every show and active-document transition resolves the current project again through canonical read-only project access.
- A missing, unreadable, stale, or foreign project clears previous project values and reports stable product copy without raw host exception detail.
- Hidden/disposed palettes release `DocumentActivated` ownership; repeated show/hide cycles must not stack callbacks.
- Document A information must never remain visible after switching to document B.

## V25 licensed-local matrix

| Cell | Action | Required result |
|---|---|---|
| PI01 | Open a DWG with an existing QS3D project and run the Project Information command. | Palette shows project name/id, drawing identity, schema/change version, active Zone/Floor and counts matching the current project. |
| PI02 | Open a DWG with no existing QS3D project and show Project Information. | Stable “no project” state; no `.qsdb` is created and no semantic state is mutated. |
| PI03 | Keep palette visible in project A, then activate project B. | All displayed identity/count values switch to B; no A identity remains. |
| PI04 | Activate project A again. | Values resolve back to A from current project state rather than a retained panel reference. |
| PI05 | With palette visible, activate a drawing whose project cannot be read safely. | Previous values are cleared and a stable failure message is shown; no raw exception text is exposed. |
| PI06 | Change active Zone/Floor through normal QS3D workflow, then hide/show Project Information. | Active Zone/Floor labels reflect the current canonical project. |
| PI07 | Add/remove families/elements/rules through supported workflows, then re-show palette. | Summary counts reflect current project data without triggering regeneration. |
| PI08 | Repeat Show → Hide → Show at least five times, then switch drawings once. | One logical refresh occurs; no stacked activation callbacks, duplicate UI effects, or resource leak symptoms. |
| PI09 | Leave Project Information visible while switching rapidly A → B → A. | Final displayed identity belongs only to the final active drawing; palette remains responsive. |
| PI10 | Exercise an unsaved drawing and a saved drawing. | Drawing label is stable, and the read-only surface does not force SAVE/SAVEAS or sidecar creation. |
| PI11 | Close/reopen the palette and unload/reload the plugin according to the normal local harness. | Native palette/event ownership is released cleanly; a fresh load opens one working Project Information surface. |
| PI12 | Run PI01–PI11 on the prepared V25 licensed host with two DWGs having deliberately different project IDs/counts. | Matrix is PASS only if all document-affinity/read-only/resource invariants hold; otherwise capture exact cell/result and keep `NO_RESULT`/FAIL as appropriate. |

## Evidence boundary

A remote deterministic preflight, Core smoke, V25-reference compile, or protected GitHub check is source evidence only. Set `LOCAL_PASS` only from an authorized licensed BricsCAD host run that records the exact source SHA/artifact identity and PI01–PI12 outcomes.
