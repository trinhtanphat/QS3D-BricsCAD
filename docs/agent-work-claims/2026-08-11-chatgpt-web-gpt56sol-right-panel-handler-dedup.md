# Work claim — RightPanel keyboard handler deduplication

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-right-panel-handler-dedup-20260811-2131`
- Registered: `2026-08-11T21:31:00+07:00`
- Completed: `2026-08-11T21:47:00+07:00`
- Baseline main SHA: `a70ca7ad759ee13442ba98af3e3de473aaea0f23`
- Registration commit: `17ecd294e8e613e6f6673c676c1378d7c3c21def`
- Priority: P0 compile correctness / coordination repair

## Confirmed defect

The audited `main` contained both `RightPanel.SearchShortcuts.cs` and `RightPanel.Keyboard.cs`, and each partial defined the same `private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)` member. Because partial declarations compile into one C# class, the duplicate signature was a deterministic compile defect. The older `scripts/preflight-right-panel-layer-search.py` already required exactly one handler across all `RightPanel*.cs`, proving the duplicate also violated an existing repository source contract.

## Implemented repair

- `6f174106012ac47d313430d3d5c0ac5a70816e0e` — removed the redundant `src/QS3D.BricsCAD.V25/UI/RightPanel.Keyboard.cs` partial.
- `0323e21642ab25202313b699c6f1bb7ab665ec16` — reconciled `scripts/preflight-right-panel-compact-interactions.py` with the canonical `RightPanel.SearchShortcuts.cs` owner. The compact gate now scans all `RightPanel*.cs`, requires exactly one callback, and fails if `RightPanel.Keyboard.cs` returns.
- `dec87574eeb36d1fec820f24c2e90eb9bd28d4d1` — corrected `docs/UI-RIGHT-PANEL-COMPACT-INTERACTIONS-2026-08-11.md` so it records the real pre-existing keyboard ownership and canonical Ctrl+F/F5/Escape behavior.
- `c1e08e1183edc1fe27137752b9cbfcd58a8bfd68` — corrected the prior compact-interactions completed claim, explicitly marking its keyboard addition as superseded while preserving the valid compact presentation work.

## Canonical ownership preserved

`RightPanel.SearchShortcuts.cs` remains the single keyboard callback owner guarded by `scripts/preflight-right-panel-layer-search.py`. `RightPanel.Interactions.cs`, `RightPanel.xaml`, `RightPanel.xaml.cs`, the Xref/layer mutation handlers, and `RightPanel.CompactShell.cs` were not modified by this repair lane.

## Integration

- Branch: `agent/right-panel-handler-dedup-20260811`.
- PR: `#483` — `fix(ui): deduplicate RightPanel keyboard handler`.
- PR head: `c1e08e1183edc1fe27137752b9cbfcd58a8bfd68`.
- Squash merge: `f0e8474d14da96407ec9f484794640a8e97c47ce`.
- PR changed exactly four paths: removed `RightPanel.Keyboard.cs`, updated the compact preflight, corrected the compact UI note, and corrected the prior completed claim.
- After integration, moving `main` reached `1b923a1bbaf1d15665ed69b8c3da7ef32943257a`; compare from the squash merge reported `ahead`, `behind_by=0`, so the repair remained in current ancestry while unrelated agents continued committing.

## Validation evidence

- Re-fetched the canonical branch `RightPanel.SearchShortcuts.cs` and confirmed the single callback implementation remains there with Ctrl+F, F5 -> `Refresh()`, and focused-search Escape behavior.
- A branch fetch of `RightPanel.Keyboard.cs` returned `404 Not Found` after the deletion.
- Re-fetched the reconciled compact preflight; it now scans every `RightPanel*.cs` and rejects both a duplicate callback count and reintroduction of `RightPanel.Keyboard.cs`.
- Reviewed the PR changed-file list and confirmed only the four reserved repair paths were included.
- PR #483 was squash-merged with expected head `c1e08e1183edc1fe27137752b9cbfcd58a8bfd68`.
- No GitHub Actions were dispatched, consistent with repository policy.
- No licensed BricsCAD V25 build/NETLOAD/WPF runtime PASS is claimed from this remote connector lane; runtime qualification remains LOCAL_ONLY.

## Completion

The deterministic duplicate-member defect is removed from `main`, the older canonical layer-search keyboard owner is preserved, the compact regression guard now covers all partials, and the inaccurate historical compact-interactions record has been corrected without reverting the valid compact UI presentation work.
