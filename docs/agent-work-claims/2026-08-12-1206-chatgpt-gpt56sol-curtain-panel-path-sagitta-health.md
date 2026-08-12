# Work claim — Curtain Panel path sagitta health

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-panel-path-sagitta-health-20260812-1206`
- Registered: `2026-08-12T12:06:00+07:00`
- Priority: P1 generated-output health completeness

## Confirmed defect

`CurtainWallPathPanelSolidBuilder` accepts `WallArcSagittaM` only as a finite value at least `1e-6`, uses it to tessellate the selected open polyline, and persists `GeneratedCurtainPanelPathSagittaM` with exact invariant round-trip (`R`) formatting. `GeneratedCurtainPanelHealthService.Mode(...)` validates path segment/mapped counts and source kind but does not validate the persisted sagitta snapshot.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`, path sagitta validation only
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Only path modes require `GeneratedCurtainPanelPathSagittaM`.
- Require invariant parse, finite value and `>= 1e-6`; otherwise Warning `CURTAIN_PANEL_PATH_SAGITTA_INVALID`.
- Valid values must use exact `value.ToString("R", CultureInfo.InvariantCulture)` spelling; aliases emit Error `CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL`.
- LINE mode remains unaffected and does not require sagitta metadata.
- Preserve all existing area/count/mode/source-kind/fingerprint/stale/native semantics.

## Validation boundary

Focused source/readback + Core smoke source only unless an executable build is actually run. No GitHub Actions or licensed BricsCAD runtime PASS is claimed.
