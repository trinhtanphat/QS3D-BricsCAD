# Work claim — Quantity Insight palette layout persistence

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-palette-layout-persistence`
- Registered: `2026-08-11T21:11:00+07:00`
- Completed: `2026-08-11T21:16:00+07:00`
- Baseline main SHA: `3abc5637ad65b81ee489b1ae8cf3c0198a95dd5c`
- Priority: P1

## Implemented

- `944f869611c424ce5a55877a70dfb15fc9871ef5` — extended `UserUiLayout` with independent `QuantityPaletteWidth=330` / `QuantityPaletteHeight=720`, dedicated 280x360 minimum policy, optional backward-compatible loading, serialization, normalization, equivalence and clone coverage.
- Existing `ui-layout-v1.txt` files remain compatible: missing Quantity keys fall back to the new defaults through the same optional `Int(...)` reader rather than invalidating the file or touching QSDB state.
- `aaac2e706d2e20d09ba6217eed62cba133dba27b` — `PaletteCoordinator` now creates the Quantity Insight palette from its own persisted dimensions/minimums and persists its actual `DeviceIndependentSize` independently during hide/dispose/recreate.
- The old coupling to `Math.Max(310, layout.RightPaletteWidth)` / `layout.RightPaletteHeight` and hard-coded `new DrawingSize(280, 360)` is removed.
- Workspace/Right dimensions, splitter preferences, visibility preservation and best-effort teardown persistence remain unchanged.
- `91d4611ec97af50929d12b08bed285ea90f9bff9` — added `scripts/preflight-quantity-palette-layout.py`, guarding all Quantity store read/write/normalize/clone/equality paths, coordinator restore/persist wiring, backward-compatible optional keys, no direct QSDB/project storage, and removal of the old borrowed/hard-coded layout path.

## Source validation

- Re-fetched `UserUiLayoutStore.cs` and `PaletteCoordinator.cs` from current `main` after concurrent commits; the independent Quantity dimensions/minimums and three-palette persistence wiring remain intact.
- The focused preflight is auto-discoverable under `scripts/preflight-*.py` and contains no runtime or private-DWG assumptions.
- Existing Workspace and Right palette keys/minimum policy remain present, so older per-user layout state and the pre-existing layout preflight contract are preserved.
- Implementation/test commits are ancestors of current `main`; concurrent reporting/ownership work was preserved and no force push was used.
- No GitHub Actions were dispatched in this lane.

## LOCAL_ONLY disposition

- Physical BricsCAD V25 dock/resize/restart click-through remains part of the existing local WPF/palette qualification boundary. This source change does not create a distinct new private-DWG scenario, so no duplicate local inbox item was added.
- No remote native runtime PASS is claimed.

## Completion evidence

- Source-contract behavior now allows the far-right Quantity Insight palette to keep its own user-selected size across hide/dispose/recreate instead of resetting from the drawing/layer palette dimensions.
- Implementation: `944f869611c424ce5a55877a70dfb15fc9871ef5`, `aaac2e706d2e20d09ba6217eed62cba133dba27b`; regression guard: `91d4611ec97af50929d12b08bed285ea90f9bff9`.
