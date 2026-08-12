# Work claim — Curtain Panel mode/source-kind canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-panel-token-canonicality-20260812-1135`
- Registered: `2026-08-12T11:35:00+07:00`
- Priority: P1 generated-output health parity

## Confirmed defect

Native Curtain Panel writers persist exact writer-owned enum-like tokens: `LinePanelSolids`, `LinePanelSolids.OpeningAware`, `PathPanelSolids`, `PathPanelSolids.OpeningAware`, and path `GeneratedCurtainPanelSourceKind=OpenPolyline`. `GeneratedCurtainPanelHealthService.Mode(...)` currently trims and compares these tokens case-insensitively, so padded/case-varied aliases remain health-clean.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

Preserve existing invalid-mode/source-kind and opening-aware mismatch semantics. Once a mode/source-kind resolves to one of the supported writer tokens, require its stored spelling to match that canonical token ordinally. Emit `CURTAIN_PANEL_MODE_NON_CANONICAL` or `CURTAIN_PANEL_PATH_SOURCE_KIND_NON_CANONICAL` as `HealthSeverity.Error` for aliases while continuing downstream checks with the normalized semantic value. Do not alter build-state, handles, integers, fingerprint, floating metadata, stale logic, writers/native runtime, or persistence format.

## Validation boundary

Add focused regression coverage for padded/case-varied line and path modes, path source-kind aliases, exact canonical controls, and invalid-token precedence. Source-safe readback only; no GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS claimed without execution.
