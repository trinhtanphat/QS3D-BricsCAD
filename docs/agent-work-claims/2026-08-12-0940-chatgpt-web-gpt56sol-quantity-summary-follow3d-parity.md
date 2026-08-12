# Work claim — Quantity Summary Follow3D parity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:40:00+07:00`
- Completed: `2026-08-12T09:49:00+07:00`
- Baseline main SHA: `c30797c28d323a08d13d0ac32bfe7b186367fbd5`
- Priority: P1 Quantity Summary UX parity during owner-requested `continue all`
- Task Key: `V25-QUANTITY-SUMMARY-FOLLOW3D-PARITY`

## Confirmed defect

`QuantitySummaryWindow` exposed `Bám 3D` (`AutoRevealCheck`) but `UpdateModePresentation()` disabled it outside detail mode and `OnQuantityGridSelectionChanged(...)` explicitly required `_detailMode`. Summary rows already carry canonical `ElementIds` / `SourceHandles`, while the existing `LocateCurrent()` path can safely resolve and reveal either a summary group or a detail item with stale-row/handle validation.

The class-level locate-failure guard mirrored the same detail-only condition. Leaving that restriction in place while enabling summary Follow3D would allow a failed summary auto-locate to retain an older CAD selection, so the parity change keeps the guard in sync with the authoritative selection handler.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.LocateSelectionFailureGuard.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml` for Follow3D wording/tooltips only
- `scripts/preflight-quantity-summary-follow3d-parity.py`
- this claim file for close-out

## Delivered contract

- Follow3D ON + summary row selection reveals the whole current group through the existing safe locate path;
- Follow3D ON + detail row selection continues to reveal the corresponding current item;
- Follow3D OFF leaves row selection read-only with no automatic 3D locate;
- a failed automatic locate clears stale CAD selection in both modes before authoritative validation runs;
- explicit Locate, resolver/stale-handle checks, selection status and `QS3DZOOMSELECTED` remain on the existing locate path;
- double-click remains the Follow3D-OFF fallback and does not duplicate auto-locate while Follow3D is enabled;
- Follow3D stays enabled in both modes and the UI guidance now describes summary-group and detail-item behavior.

## Commits

- Source parity: `b6ea359b46588dcf68f4bce3468f098990505408`
- Failure guard parity: `3abb18755978024e29851ea7d7a7f4a72c8a3939`
- UI wording merged by PR #715: `f68a2252eea40fd5e1270e6a7af42f6d0f07ba50`
- Static regression/preflight: `0773c70848f5bf5bdd48123e6031dd21d1c03454`

## Validation

Readback at `main` SHA `0773c70848f5bf5bdd48123e6031dd21d1c03454` confirmed the mode-independent selection handler, mode-independent stale-selection guard, Follow3D-OFF double-click fallback, and updated XAML guidance are all present after concurrent merges. An ancestry check confirmed the source parity commit is an ancestor of that `main` snapshot. The deterministic preflight script was committed but not executed in this remote connector session. No GitHub Actions dispatch, executable Python/.NET PASS, or licensed BricsCAD V25/V26 runtime qualification is claimed.