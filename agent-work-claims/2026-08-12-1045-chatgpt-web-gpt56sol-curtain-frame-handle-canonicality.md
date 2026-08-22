# Work claim — Curtain Frame generated handle canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-handle-canonicality`
- Registered: `2026-08-12T10:45:00+07:00`
- Baseline main SHA: `84df2060da5d1eb4b5cd7e4c180146cd3937cc8b`
- Priority: P1 — generated Curtain Frame handle metadata must preserve the writer-owned delimiter/spacing contract.
- Task Key: `CORE-CURTAIN-FRAME-HANDLE-CANONICALITY`

## Confirmed defect

`CurtainWallFrameSolidBuilder` records each generated solid with `solid.Handle.ToString()` and persists `GeneratedCurtainFrameHandles` as `string.Join(";", update.Handles)`. `GeneratedCurtainFrameHealthService` previously trimmed every token before validating it, so persisted aliases such as `"A; B"` could pass handle validity without health evidence even though the writer never emits surrounding whitespace.

## Completed implementation

- Claim commit: `1fc1a279f71c7a31e514f97ae75c11116d7f4ac7`.
- Branch source commit: `483f328a8679e09e3e9ab0648d89e5700145e483`.
- Branch smoke commit: `2149191b29a6ce4cf31fb83962fc6e380d13cd6e`.
- PR: `#772` (`chatgpt-curtain-frame-canon-20260812`).
- Squash merge on `main`: `9a61a08606f4dcb92ab677d4903da06d72c4860c`.
- Merged source and `GeneratedCurtainFrameHandleCanonicalitySmoke.cs` were read back from `main`.
- Ancestry was verified from squash merge `9a61a08606f4dcb92ab677d4903da06d72c4860c` to `main` snapshot `9bb555764a1d8096350c61da9bd69746c218ec3c`; intervening commits did not touch this lane.

## Resulting contract

- Non-empty generated Curtain Frame handle tokens with leading/trailing whitespace emit `CURTAIN_FRAME_GENERATED_HANDLE_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing invalid/duplicate/count/source-overlap/live-solid/ownership checks continue to use the trimmed token.
- Empty tokens retain `INVALID_CURTAIN_FRAME_GENERATED_HANDLE` precedence without canonicality noise.
- Lower/upper hex spelling remains accepted; this lane only owns writer-proven whitespace/delimiter canonicality.
- Fingerprint/geometry/mode/stale health behavior remains unchanged.

## Verification boundary

No GitHub Actions were dispatched. No full local .NET build PASS and no BricsCAD V25/V26 runtime PASS are claimed by this lane.
