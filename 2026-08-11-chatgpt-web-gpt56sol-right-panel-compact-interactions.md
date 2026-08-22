# Work claim — Right Panel compact interactions

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-right-panel-compact-interactions-20260811-2118`
- Registered: `2026-08-11T21:18:00+07:00`
- Completed: `2026-08-11T21:26:00+07:00`
- Baseline main SHA: `d032fbb0699d6f03ec17f55304a59af88fd1af39`
- Claim registration commit: `586ff531f50a9ca71162fe7c9622e6329995dfbc`
- Priority: P1 screenshot parity; keyboard portion later corrected by a dedicated P0 repair lane

## Correction — 2026-08-11 21:31+07

A later full-partial audit found that the repository already had the canonical `RightPanel.SearchShortcuts.cs` implementation of `OnRightPanelPreviewKeyDown` before this lane. The original compact-interactions audit looked only at `RightPanel.xaml.cs` and therefore incorrectly concluded that the XAML callback was missing.

As a result, commit `9db576528f640ccc5d5e9654f146ae57f6424b56` added a redundant `RightPanel.Keyboard.cs` method with the same signature. That creates a deterministic duplicate-member compile defect because all `RightPanel` partials form one C# class. The keyboard addition from this lane is therefore superseded and removed by the later claim `2026-08-11-chatgpt-web-gpt56sol-right-panel-handler-dedup.md`.

The compact presentation work itself remains valid and is not reverted.

## Delivered presentation behavior that remains valid

- `41396dedfafdee7ac073b4c01e9498bc27068d63` — added `RightPanel.CompactShell.cs`, an idempotent presentation-only layer that:
  - compacts the drawing region to a 238-DIP preferred / 145-DIP minimum height;
  - preserves a larger flexible layer region;
  - gives drawing/layer lists explicit minimum working areas;
  - enables preview resizing on the existing splitter;
  - strengthens section-title hierarchy and surfaces Ctrl+F/F5/Esc hints without new decorative actions.
- `7777c7aa6dd4c327ec7ebe9aca308ef4f4b187b7` — added the focused screenshot-mapping/qualification note; that note is corrected by the later dedup lane to identify `RightPanel.SearchShortcuts.cs` as the keyboard owner.
- `3c3f4d5a101f5ef92955769e2020ee849f1fb3d0` — added `scripts/preflight-right-panel-compact-interactions.py`; the later dedup lane reconciles this gate with the older canonical layer-search preflight so it scans all `RightPanel*.cs` partials instead of assuming `RightPanel.Keyboard.cs` ownership.

## Integration history

- PR: `#463` — `fix(ui): restore and compact RightPanel interactions`.
- Integrated main commit: `19a40ff629122a0e2258c3a7a066a945e380a033`.
- The PR changed the keyboard partial, compact-shell partial, focused preflight and focused UI note.
- The keyboard partial from that merge is specifically superseded by the later handler-dedup repair; the compact-shell presentation remains in current ancestry.

## Canonical keyboard ownership

The authoritative callback is `RightPanel.SearchShortcuts.cs`, guarded by `scripts/preflight-right-panel-layer-search.py`. It owns the single XAML `PreviewKeyDown="OnRightPanelPreviewKeyDown"` route and preserves Ctrl+F, F5 and focused-search Escape behavior. No second partial should define that method.

## Coordination / exclusions honored

The compact presentation lane did not modify `RightPanel.xaml`, `RightPanel.xaml.cs`, `PaletteCoordinator.cs`, `QuantityInsightPanel*`, `WallQuantityWindow*`, `QuantitySummaryWindow*`, `WorkspacePanel*`, Ribbon, Start Center, Project Tools, Core reporting/persistence/semantic mutation, updater/release/signing or GitHub Actions.

## Validation / runtime boundary

The later repair lane provides the source-correctness reconciliation for the duplicate keyboard callback. Native BricsCAD V25/WPF/HiDPI dock/focus/render verification remains under the repository's existing local qualification boundary. No remote licensed runtime PASS is claimed.

## Final disposition

This claim remains `COMPLETED` for the compact RightPanel presentation work. Its keyboard-handler ownership record is corrected here: the added `RightPanel.Keyboard.cs` was erroneous and is superseded by the dedicated handler-dedup repair, while the compact visual/density improvements remain valid.
