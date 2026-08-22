# Agent work claim — BLT3D BIM pixel parity + iconography

- Owner/session: `chatgpt-gpt56sol`
- Date: 2026-08-16
- Baseline: `main@6297bce4c03558205c769aa1c9b8f1de85aa3ce3`
- Branch: `agent/chatgpt-gpt56sol/blt3d-bim-pixel-parity-icons-20260816`

## Scope

Bring the `MÔ HÌNH BIM` docked workspace visibly closer to the owner-provided BLT3D reference while preserving the native BricsCAD viewport and existing QS3D mutation handlers.

Planned changes:

- remove QS3D-only workspace chrome that is not present in the reference screenshot;
- tighten the left model/family/properties layout and footer toward the supplied BLT3D proportions;
- tighten the drawing/layer manager on the right to the supplied BLT3D controls and ordering;
- add original, host-safe vector iconography to the visible workspace/right-panel buttons (no BLT3D proprietary assets copied);
- add original category glyphs to the model tree where safe without changing semantic tags/headers;
- preserve existing handlers and BricsCAD-native viewport behavior;
- add a deterministic source preflight for the parity/icon contract.

## Intended files

- `src/QS3D.BricsCAD.V25/UI/Blt3dVectorIcon.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dPixelParity.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/RightPanel.Blt3dPixelParity.cs` (new)
- `scripts/preflight-blt3d-bim-pixel-parity.py` (new)
- this claim file

## Non-goals / safety boundary

- No copy of BLT3D proprietary binaries, CUI, raster icons, or other protected assets.
- No replacement/fake renderer for the BricsCAD drawing viewport.
- No ProjectState/QSDB/schema semantic changes.
- No claim of interactive BricsCAD runtime screenshot PASS from remote CI; the final pixel check still requires a licensed local BricsCAD host.
- No direct/force write to `main`; integration remains through PR and required CI.
