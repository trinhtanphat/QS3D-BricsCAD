# Agent work claim — BLT3D Project Properties surface

- Owner/session: `chatgpt-gpt56sol`
- Date: 2026-08-16
- Baseline: `main@b5900e556ede5c931ea9d881a7d26f0a30cca768`
- Branch: `agent/chatgpt-gpt56sol/project-properties-blt3d-20260816`

## Scope

Fix the `THIẾT LẬP DỰ ÁN → Thuộc tính dự án` BLT3D parity defect where the ribbon entry currently routes to the broad `QS3DPROJECTTOOLS` dashboard instead of its own bounded surface.

Planned changes:

- route `QS3D_PROJECT_PROPERTIES` to a dedicated `QS3DPROJECTPROPERTIES` command;
- add a dedicated read-only Project Properties window matching the supplied BLT3D reference state `(Chưa xây dựng — Thuộc tính dự án)` rather than inventing unsupported project-property semantics;
- keep `Thông tin dự án` on `QS3DPROJECTTOOLS` and `Cài đặt tầng` on `QS3DLEVELS`;
- add an aggregate-discovered source preflight pinning the three-entry routing and dedicated placeholder surface;
- preserve V26 sharing through the existing V25 source wildcard.

## Intended files

- `src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs`
- `src/QS3D.BricsCAD.V25/ProjectPropertiesCommands.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/ProjectPropertiesWindow.cs` (new)
- `scripts/preflight-blt3d-project-properties.py` (new)
- this claim file

## Non-goals / safety boundary

- No ProjectState/QSDB/schema/semantic mutation changes.
- No fake renderer or replacement for the BricsCAD drawing viewport.
- No BLT3D proprietary code, binaries, CUI, icons, or assets.
- No claim of licensed interactive BricsCAD visual/runtime PASS from remote CI.
- No direct/force write to `main`; integration remains through PR and required CI.
