# Work claim — Quantity Summary Follow3D parity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:40:00+07:00`
- Baseline main SHA: `c30797c28d323a08d13d0ac32bfe7b186367fbd5`
- Priority: P1 Quantity Summary UX parity during owner-requested `continue all`
- Task Key: `V25-QUANTITY-SUMMARY-FOLLOW3D-PARITY`

## Confirmed defect

`QuantitySummaryWindow` exposes `Bám 3D` (`AutoRevealCheck`) but `UpdateModePresentation()` disables it outside detail mode and `OnQuantityGridSelectionChanged(...)` explicitly requires `_detailMode`. Summary rows already carry canonical `ElementIds` / `SourceHandles`, while the existing `LocateCurrent()` path can safely resolve and reveal either a summary group or a detail item with stale-row/handle validation.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`
- focused deterministic regression/preflight coverage for Follow3D summary/detail parity
- this claim file for close-out

## Contract

- Follow3D ON + summary row selection reveals the whole current group through the existing safe locate path;
- Follow3D ON + detail row selection continues to reveal the corresponding current item;
- Follow3D OFF leaves row selection read-only with no automatic 3D locate;
- keep explicit Locate, double-click, resolver/stale-handle checks, selection status, zoom/highlight and failure handling intact;
- do not weaken project/document identity validation or add a second locate implementation.

## Validation plan

Add a focused source/preflight regression that locks the mode-independent Follow3D selection contract and enabled state while preserving explicit locate/double-click behavior. Re-fetch source and claim before writes and inspect exact pushed diffs. No GitHub Actions dispatch, executable .NET build/preflight PASS, or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.