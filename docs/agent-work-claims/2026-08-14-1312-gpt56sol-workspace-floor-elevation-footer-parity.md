# Work claim — Workspace floor elevation footer parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-footer`
- Registered: `2026-08-14T13:12:00+07:00`
- Baseline main SHA: `008b668766bc4ea27d7b072dacc6d418f3cb131b`
- Owner request: continue all and complete remaining screenshot/session gaps without speculative native-runtime changes.

## Concrete screenshot gap

The supplied BLT3D reference footer shows the active floor together with its elevation (`Tầng … • Cao độ 0.000 m`). QS3D already shows live Project / Zone / Floor in `WorkspacePanel.FooterContext.cs`, and the canonical floor domain stores `FloorDefinition.ElevationM` in meters, but the footer does not display that elevation.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FooterContext.cs`
- `scripts/preflight-workspace-footer-context.py`
- `docs/BLT-REFERENCE-UI-PARITY-PLAN-2026-08-14.md`
- this claim file

## Implementation boundary

- Presentation-only: read the already-active `FloorDefinition.ElevationM`; do not mutate ProjectState, active floor, CAD entities, document lifecycle or host state.
- Render elevation with invariant `0.000 m` precision so signed/decimal output is deterministic.
- Preserve existing Project / Zone / Floor context and current non-breaking exception boundary.
- Do not touch #1125 Level/Curtain frame-Z production logic, RightPanel active claim files, startup/Ribbon lifecycle, or LOCAL_ONLY runtime surfaces.

## Validation

- Extend the existing focused footer preflight to require `ElevationM` and formatted `CAO ĐỘ` output while retaining read-only forbidden-token checks.
- Read back the exact merged source/guard.
- BricsCAD V25 visual width, clipping, DPI and dark-theme acceptance remain local/native; no `LOCAL_PASS` claim.
