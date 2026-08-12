# Work claim — Curtain Panel build-state canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-panel-build-state-canonicality-20260812-1131`
- Registered: `2026-08-12T11:31:00+07:00`
- Priority: P1 generated-output health parity

## Confirmed defect

Both native Curtain Panel writers persist `GeneratedCurtainPanelBuildState` as exact `"Complete"`. `GeneratedCurtainPanelHealthService.Inspect(...)` currently trims and compares case-insensitively, so persisted aliases such as `" complete "` or `"COMPLETE"` are silently accepted even though no production writer emits them.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

Preserve the existing missing/unsupported build-state warning. When a stored token normalizes case-insensitively to `Complete` but is not exact ordinal `Complete`, emit `CURTAIN_PANEL_BUILD_STATE_NON_CANONICAL` as `HealthSeverity.Error`. Keep the normalized semantic state valid for downstream panel diagnostics; do not change handles, integer/fingerprint/mode/floating metadata, stale logic, writer/native code, or runtime behavior.

## Validation boundary

Add focused regression coverage for padded and case-varied aliases, exact canonical `Complete`, and invalid/missing precedence. Source-safe readback only; no GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS claimed without execution.
