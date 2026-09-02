# Start Center document-affinity lifecycle — licensed-local qualification

Status: `LOCAL_ONLY / NO_RESULT`

This package hardens the native BricsCAD V25 Start Center modeless lifecycle. Remote source/preflight/compile evidence is not licensed-host runtime evidence and must not be reported as `LOCAL_PASS`.

## Invariants

- `DocumentActivated` refresh is bound to the event document; `MdiActiveDocument` is only a null fallback.
- `Hide()` releases `DocumentActivated` ownership even when native `PaletteSet.Visible = false` throws.
- Repeated Show/Hide cycles must not stack duplicate activation handlers.
- `Dispose()` releases event ownership before dropping panel/palette references.
- Start Center refresh remains display-only: it does not create project state, save drawings, regenerate geometry, or mutate semantic project state.
- Customer-visible Start Center failure text must remain stable and must not include raw exception messages, paths leaked through exception text, stack traces, or host implementation detail.
- A document activation must not record/render a different DWG because another global active-document transition occurred around the callback.

## V25 licensed-local matrix

| Cell | Action | Required result |
|---|---|---|
| SC01 | Open DWG A with a distinct active floor and show Start Center. | Floor/elevation and recent-project state belong to A. |
| SC02 | Keep Start Center visible and switch A → B where B has a different active floor/project. | UI refreshes to B only; no A floor/elevation remains. |
| SC03 | Switch rapidly A → B → A with Start Center visible. | Final UI and recorded recent-project entry correspond to the final activation document; no stale intermediate document wins. |
| SC04 | Repeat Show → Hide → Show at least five times, then activate one other DWG once. | One logical activation refresh occurs; no duplicate callback effects or stacked event ownership. |
| SC05 | Exercise Hide during a host state where PaletteSet visibility change fails or during controlled teardown if the local harness can reproduce it. | Cleanup remains fail-closed: activation ownership is released/recoverable and a hidden callback root is not stranded. Capture exact host evidence; do not mark PASS if the failure path cannot be exercised. |
| SC06 | Hide Start Center, then switch drawings. | Hidden Start Center performs no document refresh and does not update recent-project state from activation callbacks. |
| SC07 | Close/unload the plugin with Start Center previously shown, then reload and show it again. | No duplicate callback, stale panel, or disposed palette behavior; one fresh surface works. |
| SC08 | Trigger a recent-project open failure with a missing/unreadable file. | Stable product copy is shown; no raw exception message/stack trace is exposed. |
| SC09 | Trigger a Start Center quick action failure through an allowed local failure case. | Stable product copy is shown; no raw host exception detail is exposed. |
| SC10 | Show/refresh Start Center on a DWG without a QS3D project. | Refresh remains read-only; no project/sidecar is created and no SAVE/SAVEAS is forced. |
| SC11 | Switch between saved and unsaved drawings while Start Center is visible. | No wrong-DWG recent-project record is created; unsaved/non-normalizable paths are not persisted as recent projects. |
| SC12 | Run SC01–SC11 on the admitted licensed BricsCAD V25 host and record exact source/artifact SHA. | `LOCAL_PASS` is allowed only if every applicable cell passes against that exact candidate; otherwise keep `NO_RESULT`/FAIL with captured evidence. |

## Remote/static evidence

The auto-discovered source guard `scripts/preflight-v25-start-center-document-affinity.py` verifies the document-bound refresh contract, `Hide()` cleanup relationship, and raw-exception-message prohibition. Protected CI/V25-reference compilation proves source/build compatibility only.

## Runtime boundary

Do not infer native host safety from remote CI. Native PaletteSet teardown, activation ordering, UI thread behavior, and actual BricsCAD event delivery remain `LOCAL_ONLY` until SC01–SC12 are executed on an authorized licensed V25 host with exact SHA/artifact identity.
