# Work claim — ĐỊNH LƯỢNG Ribbon reference parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-ribbon-reference-parity`
- Registered: `2026-08-11T21:26:00+07:00`
- Baseline main SHA: `95649da0c5d423105cf66eaa4ab3282f5e22e685`
- Priority: P1 screenshot/reference workflow parity

## Reserved scope

- `src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs` (new isolated augmenter)
- `src/QS3D.BricsCAD.V25/PluginEntry.cs` (initialization/reset hook only)
- `scripts/preflight-ribbon-quantity-reference-parity.py`
- this claim file
- `src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs` is now **audit-only / no edit planned** so concurrent bootstrap/reconciliation winners remain untouched.

## Goal

Bring the existing `ĐỊNH LƯỢNG` Ribbon closer to the supplied BLT3D reference without inventing decorative controls. Surface the already-implemented QS3D calculation settings, regeneration/takeoff, ED2 export, quantity review/explanation, wall takeoff, Excel reverse-locate and old/new revision comparison workflows using clear Vietnamese labels.

## Implementation shape

Use a dedicated reconciliation-safe augmenter, following the repo's existing `ProjectRibbonAugmenter`, `ReferenceWallRibbonAugmenter` and `QuickWorkflowRibbonAugmenter` pattern. It locates the already-created `QS3D_QTY` tab, finds-or-creates one uniquely identified reference panel, and finds-or-creates buttons by stable IDs while always reconciling current text/command/handler. `PluginEntry` only invokes `TryInitialize()` after `RibbonBootstrapper` and resets the augmenter during termination.

## Functional contract

- Keep every existing `QS3D_QTY` bootstrap panel/button and all previously registered command bindings; this lane is additive and does not rewrite/remove existing bootstrap panels.
- Add one reference-oriented panel whose buttons dispatch real existing commands only:
  - `Cài đặt tính toán` -> `QS3DQUANTITYSETTINGS`
  - `Tính khối lượng` -> `QS3DREGEN`
  - `Xuất ED2` -> `QS3DED2`
  - `Xem khối lượng` -> `QS3DBQ`
  - `Diễn giải` -> current full `QS3DBQ` workflow (which exposes summary/detail modes); a later dedicated detail command may replace the command parameter under the same stable button ID without changing this UI slot
  - `Khối lượng tường` -> `QS3DWALLQTY`
  - `Excel → CAD` -> `QS3DEXCELLOCATE`
  - `Đối chiếu Cũ/Mới` -> `QS3DREVDIFF`
- Preserve Ribbon reconciliation semantics: do not clear panels/items, do not remove bootstrap/augmenter/user controls, and keep click-time active-document command dispatch.
- If the base `QS3D_QTY` tab is not available yet, the augmenter fails closed/returns false rather than creating a competing tab.
- Do not modify Quantity Settings implementation/store, Wall Quantity implementation, Commands.cs, Core quantity arithmetic, Direct Draw, updater/release work or GitHub Actions.

## Validation plan

- Re-fetch latest `main`, `PluginEntry` and Ribbon sources immediately before the write; preserve concurrent winners.
- Add an auto-discovered focused static preflight requiring the new panel/title/stable button IDs/labels/commands exactly once, find-or-create reconciliation, plugin initialization/reset hooks, command registration evidence, and no collection clear/removal behavior.
- Continue to require the pre-existing `QS3D_QTY` bootstrap panels so the augmenter cannot silently replace the canonical Ribbon information architecture.
- Re-fetch final source and ancestry. Do not dispatch GitHub Actions.

## Completion condition

The screenshot-level quantity workflows are directly discoverable from the `ĐỊNH LƯỢNG` Ribbon using real existing commands, with additive reconciliation-safe source and regression coverage, and this claim is marked `COMPLETED` with exact SHAs.
