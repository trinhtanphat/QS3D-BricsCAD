# Work claim — Curtain Panel integer snapshot canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-panel-integer-snapshot-canonicality-20260812-1116`
- Registered: `2026-08-12T11:16:00+07:00`
- Priority: P1 generated-output health parity

## Confirmed defect

`GeneratedCurtainPanelHealthService.Integer(...)` parses writer-owned integer metadata with `NumberStyles.Integer` but does not verify the exact invariant spelling. As a result, persisted values such as `"01"`, `"+1"` or `" 1 "` can pass the integer validity path without health evidence. Both native Curtain Panel writers emit these fields with `ToString(CultureInfo.InvariantCulture)`, and the sibling Curtain Frame health provider already fails visible on non-canonical integer snapshots.

Affected shared helper keys include `GeneratedCurtainPanelCount`, `GeneratedCurtainPanelBaseCount`, `GeneratedCurtainPanelColumns`, `GeneratedCurtainPanelRows`, `GeneratedCurtainPanelOpeningCount`, and path-panel `GeneratedCurtainPanelPathSegmentCount` / `GeneratedCurtainPanelMappedCount`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

After an integer parses and passes its existing zero/positive bound, require `raw == value.ToString(CultureInfo.InvariantCulture)` ordinally. Emit a dedicated `HealthSeverity.Error` canonicality issue on aliases while preserving the parsed value for all existing count/grid/path consistency checks. Preserve all current missing/invalid warnings and do not alter handle, BuildState, mode/source-kind, fingerprint, floating-point metadata, stale, ownership, or native runtime behavior.

## Validation boundary

Add focused regression coverage for leading-zero, explicit-plus and surrounding-whitespace aliases plus canonical controls. Source-safe readback only; no GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS claimed without execution.
