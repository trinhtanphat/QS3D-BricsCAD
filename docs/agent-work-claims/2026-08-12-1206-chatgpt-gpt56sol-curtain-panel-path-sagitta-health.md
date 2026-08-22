# Work claim — Curtain Panel path sagitta health

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-curtain-panel-path-sagitta-health-20260812-1206`
- Registered: `2026-08-12T12:06:00+07:00`
- Completed: `2026-08-12T12:09:00+07:00`
- Claim commit: `dc7e2ec7fb4f40558b4ec05901d08649b489003b`
- Source fix commit: `aeba31f5a5831d25817e91e381e2fc7fe700928b`
- Focused smoke commit: `e3f5309e7d96e25ce38b3506382dc699cda68fea`
- Integration PR: `#862`
- Main integration SHA: `07dea99dc12e4da1c15fdd7e6fd9da1dcb860bd0`
- Priority: P1 generated-output health completeness

## Confirmed defect

`CurtainWallPathPanelSolidBuilder` accepts `WallArcSagittaM` only as a finite value at least `1e-6`, uses it to tessellate the selected open polyline, and persists `GeneratedCurtainPanelPathSagittaM` with exact invariant round-trip (`R`) formatting. `GeneratedCurtainPanelHealthService.Mode(...)` validated path segment/mapped counts and source kind but did not validate the persisted sagitta snapshot.

## Integrated contract

- Only path modes require `GeneratedCurtainPanelPathSagittaM`.
- Missing/malformed/non-finite/below-`1e-6` values emit Warning `CURTAIN_PANEL_PATH_SAGITTA_INVALID`.
- Valid values must use exact `value.ToString("R", CultureInfo.InvariantCulture)` spelling; aliases emit Error `CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL`.
- LINE mode remains unaffected and does not require sagitta metadata.
- Existing area/count/mode/source-kind/fingerprint/stale/native semantics are preserved.

## Regression evidence

`tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelPathSagittaHealthSmoke.cs` is auto-registered and covers canonical path sagitta, missing/below-min/non-finite values, explicit-plus/padded/trailing-zero aliases, and LINE mode without sagitta.

PR #862 was reviewed as exactly two changed files and squash-merged with expected head `3840574a114b2a0d3fa74617cf02733aec4076e8` as `07dea99dc12e4da1c15fdd7e6fd9da1dcb860bd0`.

## Validation boundary

Source and focused regression were integrated/read back through GitHub. No GitHub Actions/full local .NET build/executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed without execution.
