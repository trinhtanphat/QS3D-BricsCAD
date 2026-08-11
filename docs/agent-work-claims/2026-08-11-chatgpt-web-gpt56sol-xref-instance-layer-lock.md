# Work claim — Xref instance-layer lock controls

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xref-instance-layer-lock`
- Registered: `2026-08-11T21:40:00+07:00`
- Completed: `2026-08-11T21:48:00+07:00`
- Baseline main SHA: `5ed731f70dd1d03948b689dc5a524411ff87ae02`
- Priority: P1 screenshot/reference workflow parity

## Implemented

- `a7112cb241be6406e0be189d6df902cb6aae4c04` — added `XrefService.SetInstanceLayersLocked(...)`. It resolves the selected Xref definition, scans only live references in the current space, deduplicates their native layer IDs, updates `LayerTableRecord.IsLocked` under a BricsCAD document lock + write transaction, regenerates after real changes, and returns zero without touching unrelated layers when there are no current-space instances.
- `5d8f9c209e7f25b39510766f9c8f672ffd498679` — added drawing-toolbar `Khóa` / `Mở khóa` buttons and matching `Khóa layer Xref` / `Mở khóa layer Xref` context-menu actions while preserving Add/Reload/Move/Zoom/Detach and the existing live `Khóa` status column.
- `4a6611e821112584c1e5b36d131fd935ae9fa30d` — added isolated `RightPanel.XrefLock.cs` handlers. They reuse the existing `SelectedXref()` boundary, so the main DWG row is rejected, and use the existing `RefreshAfterXrefMutation(...)` path to refresh both drawing lock state and layer-manager data immediately.
- `3c4a6a4736d4f2ebc081b68f2ec602514615baae` — added `scripts/preflight-xref-instance-layer-lock.py`, guarding current-space/Xref filtering, layer-ID deduplication, document-lock/write-transaction ordering, native layer writes, zero-instance isolation, UI/context-menu wiring, preservation of prior Xref actions and absence of semantic/QSDB mutation.
- `131f7fe4cdd454823d4e9fee184bc9b1b0d48eed` — narrowed the claim to an isolated partial rather than replacing the large concurrent `RightPanel.xaml.cs` interaction surface.

## Source validation

- Re-fetched current `main` after substantial concurrent work. `XrefService.cs` still contains the exact current-space native-layer implementation, `RightPanel.XrefLock.cs` still reuses `SelectedXref()` and refreshes both drawing/layer state, and `RightPanel.xaml` still contains both lock/unlock toolbar buttons, both context-menu actions, the lock-state column and all original drawing actions.
- Re-fetched `scripts/preflight-xref-instance-layer-lock.py` from current `main`; the focused regression guard is intact.
- `compare_commits` from `a7112cb241be6406e0be189d6df902cb6aae4c04` to current `main` reports `main` ahead with the implementation as merge base, proving the lane remains in ancestry while preserving concurrent commits. No force push was used.
- GitHub exposes no combined status checks for `3c4a6a4736d4f2ebc081b68f2ec602514615baae`; no GitHub Actions were dispatched in this lane.

## LOCAL_ONLY disposition

- Physical BricsCAD V25 mouse interaction, native layer-lock visual behavior and viewport confirmation remain part of the existing local RightPanel/palette runtime qualification boundary. This source lane introduces no separate private-DWG-only scenario, so no duplicate local inbox item was added.
- No remote native runtime PASS is claimed.

## Completion evidence

The screenshot-inspired drawing manager's `Khóa` state is now actionable: users can lock or unlock the native layers carrying the selected Xref's current-space instances, with immediate drawing/layer UI refresh and without modifying Xref source files or QS3D semantic project state.
