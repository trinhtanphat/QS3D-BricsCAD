# Work claim — ĐỊNH LƯỢNG Ribbon reference parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-ribbon-reference-parity`
- Registered: `2026-08-11T21:26:00+07:00`
- Baseline main SHA: `95649da0c5d423105cf66eaa4ab3282f5e22e685`
- Priority: P1 screenshot/reference workflow parity

## Reserved scope

- `src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs`
- `scripts/preflight-ribbon-quantity-reference-parity.py`
- this claim file

## Goal

Bring the existing `ĐỊNH LƯỢNG` Ribbon closer to the supplied BLT3D reference without inventing decorative controls. Surface the already-implemented QS3D calculation settings, regeneration/takeoff, ED2 export, quantity review/explanation, wall takeoff, Excel reverse-locate and old/new revision comparison workflows using clear Vietnamese labels.

## Functional contract

- Keep every existing `QS3D_QTY` panel/button and all previously registered command bindings; this lane is additive so already-loaded Ribbon reconciliation does not strand legacy button IDs.
- Add one reference-oriented panel whose buttons dispatch real existing commands only:
  - `Cài đặt tính toán` -> `QS3DQUANTITYSETTINGS`
  - `Tính khối lượng` -> `QS3DREGEN`
  - `Xuất ED2` -> `QS3DED2`
  - `Xem khối lượng` -> `QS3DBQ`
  - `Diễn giải` -> current full `QS3DBQ` workflow (which exposes summary/detail modes); a later dedicated detail command may replace the command parameter under the same button ID without changing this UI slot
  - `Khối lượng tường` -> `QS3DWALLQTY`
  - `Excel → CAD` -> `QS3DEXCELLOCATE`
  - `Đối chiếu Cũ/Mới` -> `QS3DREVDIFF`
- Preserve Ribbon reconciliation semantics: do not clear panels/items, do not remove augmenter/user controls, and keep native click-time command dispatch through `RibbonCommandHandler`.
- Do not modify Quantity Settings implementation/store, Wall Quantity implementation, Commands.cs, Core quantity arithmetic, Direct Draw, updater/release work or GitHub Actions.

## Validation plan

- Re-fetch latest `main` and Ribbon source immediately before the write; preserve concurrent winners.
- Add an auto-discovered focused static preflight requiring the new panel/title/labels/commands exactly once while guarding all pre-existing quantity panels and the no-clear/reconciliation contract.
- Re-fetch final source and ancestry. Do not dispatch GitHub Actions.

## Completion condition

The screenshot-level quantity workflows are directly discoverable from the `ĐỊNH LƯỢNG` Ribbon using real existing commands, with additive reconciliation-safe source and regression coverage, and this claim is marked `COMPLETED` with exact SHAs.
