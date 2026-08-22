# Work claim — Quantity Insight single-click 3D reveal

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-single-click-reveal`
- Registered: `2026-08-11T21:18:00+07:00`
- Completed: `2026-08-11T21:22:00+07:00`
- Baseline main SHA: `8bfb74a049083105d58d65ecdd9ef74739050fc4`
- Priority: P1

## Implemented

- `22a14287143edabc584bb3fc23f2b6a9ad80899d` — Quantity Insight toolbar now exposes `Click = 3D`, enabled by default, and the quantity tree wires `SelectedItemChanged` in addition to the existing manual `Định vị`/double-click affordances.
- The panel help text now states the actual default behavior: clicking a quantity leaf reveals and zooms the corresponding objects in the real BricsCAD View 3D.
- `1b013d19e157ffa924d0f0d5eedd8250a3dd208f` — `OnQuantityTreeSelectedItemChanged` runs only when auto-reveal is enabled and only for `QuantityInsightItemViewModel` leaves. Floor/group selection remains passive.
- Auto-reveal reuses `LocateSelected()` unchanged at the safety boundary: bound active DWG -> ProjectId/fingerprint -> detached-preview current-row revalidation -> current semantic Handles -> native CAD selection -> `QS3DZOOMSELECTED`.
- Double-click now returns immediately while auto-reveal is enabled, preventing the second click of a double-click gesture from dispatching a duplicate locate. When auto-reveal is disabled, double-click remains the manual leaf-only fallback.
- `607bb1be20768cc3b7b3074342bebc1050112e43` — added `scripts/preflight-quantity-insight-single-click-reveal.py` covering XAML wiring, default toggle state, leaf-only gating, non-duplicating double-click fallback and the existing fail-closed native locate ordering.

## Source validation

- Re-fetched current `main` after concurrent commits. XAML still contains `AutoRevealCheck`, `IsChecked="True"` and `SelectedItemChanged="OnQuantityTreeSelectedItemChanged"`; code-behind still gates on the toggle and leaf type before calling `LocateSelected()`.
- Re-fetched the focused preflight from current `main`; it continues to forbid creating/mutating project access and stale direct `item.ElementIds` resolution.
- `607bb1be20768cc3b7b3074342bebc1050112e43` is an ancestor of current `main`; subsequent concurrent formula/update/direct-draw work does not touch this lane.
- No GitHub Actions were dispatched.

## LOCAL_ONLY disposition

- Actual mouse/keyboard selection, PICKFIRST visual feedback and camera zoom in licensed BricsCAD V25 remain part of the existing local palette/selection runtime qualification boundary. No duplicate local inbox item was created.
- No remote native runtime PASS is claimed.

## Completion evidence

- The supplied BLT-style interaction is now represented directly: by default, one normal click on a quantity leaf row reveals it in the real 3D CAD viewport, while users can turn that behavior off and retain explicit/manual locate controls.
- Implementation: `22a14287143edabc584bb3fc23f2b6a9ad80899d`, `1b013d19e157ffa924d0f0d5eedd8250a3dd208f`; regression guard: `607bb1be20768cc3b7b3074342bebc1050112e43`.
