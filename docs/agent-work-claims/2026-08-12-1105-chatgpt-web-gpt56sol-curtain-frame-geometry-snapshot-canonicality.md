# Work claim — Curtain Frame geometry snapshot canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-geometry-snapshot-canonicality`
- Registered: `2026-08-12T11:05:00+07:00`
- Baseline main SHA: `8ced6f932a3e5a7e3618116587e0363e72ea136b`
- Priority: P1 — generated Curtain Frame geometry snapshots must preserve exact writer-owned round-trip numeric spelling.
- Task Key: `CORE-CURTAIN-FRAME-GEOMETRY-SNAPSHOT-CANONICALITY`

## Confirmed defect

Both `CurtainWallFrameSolidBuilder` and `CurtainWallPathFrameSolidBuilder` persist `GeneratedCurtainFrameDepthM`, `GeneratedCurtainFrameSourceLengthM`, and `GeneratedCurtainFrameHeightM` with `double.ToString("R", CultureInfo.InvariantCulture)`. `GeneratedCurtainFrameHealthService` previously validated these snapshots through numeric parsing only, allowing alternate raw spellings for the same positive value to pass health even though the writers never emit those spellings.

## Completed implementation

- Claim commit: `3272a35050014642064aa1109e7a202379dfeb08`.
- Branch source commit: `d805ea785fbd1b7bf777d406fd8ac02f7cd8c194`.
- Branch smoke commit: `e1a8b4a9e363ad0a7883a5522dd2a3fcb9012185`.
- PR: `#796` (`chatgpt-curtain-frame-geometry-canon-20260812`).
- Squash merge on `main`: `e60af2087b02fc1db623835d912e07253c375f57`.
- Merged source and `GeneratedCurtainFrameGeometrySnapshotCanonicalitySmoke.cs` were read back from `main`.
- Ancestry was verified from squash merge `e60af2087b02fc1db623835d912e07253c375f57` to `main` snapshot `0eb91eb254094bc9515197dad39a7aa832a65d68`; the intervening commit did not touch this lane.

## Resulting contract

- After depth/source-length/height snapshots parse as finite and positive, their raw text must match `value.ToString("R", CultureInfo.InvariantCulture)` or emit dedicated `HealthSeverity.Error` canonicality diagnostics.
- Existing invalid/nonfinite/nonpositive warnings retain precedence and invalid values do not receive canonicality noise.
- Existing geometry stale comparisons continue to use parsed numeric values.
- Exact writer-owned round-trip strings preserve existing behavior.
- Inspection remains read-only and deterministic.

## Verification boundary

No GitHub Actions were dispatched. No full local .NET build PASS and no BricsCAD V25/V26 runtime PASS are claimed by this lane.
