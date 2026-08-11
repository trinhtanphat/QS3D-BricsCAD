# Work claim — RightPanel keyboard handler deduplication

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-right-panel-handler-dedup-20260811-2131`
- Registered: `2026-08-11T21:31:00+07:00`
- Baseline main SHA: `a70ca7ad759ee13442ba98af3e3de473aaea0f23`
- Priority: P0 compile correctness / coordination repair

## Confirmed defect

Current `main` contains both `RightPanel.SearchShortcuts.cs` and `RightPanel.Keyboard.cs`, and each partial defines the same `private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)` member. C# partial classes cannot contain duplicate members with identical signatures, so this is a deterministic source-level compile defect. The repository's existing `preflight-right-panel-layer-search.py` already requires exactly one handler across all `RightPanel*.cs`, which also proves the current duplicate violates an existing source contract.

## Reserved scope

- remove the redundant `src/QS3D.BricsCAD.V25/UI/RightPanel.Keyboard.cs` introduced by the later compact-interactions lane;
- reconcile `scripts/preflight-right-panel-compact-interactions.py` with the canonical pre-existing `RightPanel.SearchShortcuts.cs` route and the existing layer-search preflight;
- correct `docs/UI-RIGHT-PANEL-COMPACT-INTERACTIONS-2026-08-11.md` and the prior compact-interactions claim where they incorrectly stated the handler was absent before that lane;
- this claim file for close-out.

## Preserve

`RightPanel.SearchShortcuts.cs` remains the canonical keyboard callback owner because `scripts/preflight-right-panel-layer-search.py` explicitly guards that file and the single XAML route. Keep `RightPanel.Interactions.cs`, `RightPanel.xaml`, `RightPanel.xaml.cs`, all Xref/layer mutation handlers, and compact presentation behavior unchanged unless a direct integration blocker is proven.

## Exclusions

No changes to PaletteCoordinator, Quantity Insight, Workspace, Ribbon, Direct Draw, Core reporting/persistence/semantics, updater/release/signing, GitHub Actions, or LOCAL_ONLY runtime qualification.

## Validation plan

- Re-fetch latest `main` and both handler partials before implementation.
- Require exactly one `OnRightPanelPreviewKeyDown` implementation across all `RightPanel*.cs` after the repair.
- Keep the canonical layer-search preflight authoritative for Ctrl+F/F5/Escape behavior and update the compact preflight so it composes with rather than duplicates that ownership.
- Integrate through a branch/PR without force push and without dispatching GitHub Actions.
