# Work claim — Quantity Insight palette layout persistence

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-palette-layout-persistence`
- Registered: `2026-08-11T21:11:00+07:00`
- Baseline main SHA: `3abc5637ad65b81ee489b1ae8cf3c0198a95dd5c`
- Priority: P1

## Reserved scope

- Give the third docked Quantity Insight palette its own persisted per-user width/height instead of borrowing the drawing/layer palette dimensions on every recreation.
- Centralize Quantity Insight minimum dimensions in `UserUiLayoutStore` alongside the existing Workspace/Right palette policy, and make palette teardown/recreate preserve the independently resized quantity surface.
- Preserve all existing visibility behavior, Workspace/Right persistence keys, model/family splitter persistence, and the BLT-style three-palette workspace.

## Expected files

- `src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs`
- `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`
- `scripts/preflight-quantity-palette-layout.py`
- this claim file for close-out

## Excluded scope

- `QuantityInsightPanel*` quantity calculations/interactions, Ribbon/Start Center/Workspace compact presentation, quantity settings/rules, Wall Takeoff, updater/release, Core persistence/domain.
- No native BricsCAD V25 resize/docking PASS claim from the remote connector environment.

## Functional contract

- Add `QuantityPaletteWidth`/`QuantityPaletteHeight` defaults, load/serialize/clone/equivalence/normalization support, with compatibility for existing `ui-layout-v1.txt` files that do not yet contain the keys.
- Add dedicated `QuantityPaletteMinWidth`/`QuantityPaletteMinHeight` constants and use them in `PaletteCoordinator` rather than hard-coded dimensions.
- `EnsureCreated()` must initialize the Quantity Insight `PaletteSet` from its own persisted dimensions.
- `PersistPaletteLayout()` must persist Workspace, drawing/layer, and Quantity Insight dimensions independently and remain best-effort/non-blocking.
- Reset/recreate visibility semantics must remain unchanged.

## Validation plan

- Re-fetch current `main` immediately before writes and preserve concurrent winners.
- Add an auto-discovered static preflight covering all store read/write/normalize/clone/equivalence paths plus coordinator create/persist wiring, while guarding that Workspace/Right keys remain present.
- Re-fetch final source and ancestry; do not dispatch GitHub Actions.

## Completion condition

- A user resize of the far-right Quantity Insight palette survives hide/dispose/recreate independently from the drawing/layer palette at source-contract level, with deterministic regression coverage and this claim marked `COMPLETED`.
