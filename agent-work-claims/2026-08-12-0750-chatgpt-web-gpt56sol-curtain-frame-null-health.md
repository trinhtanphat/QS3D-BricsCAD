# Work claim — Generated Curtain Frame health null-element fail-visible

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-null-health`
- Registered: `2026-08-12`
- Baseline main SHA: `3a766aeb9192ae12d42fc4f9bd2d27b05baaae37`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-CURTAIN-FRAME-NULL-HEALTH`

## Confirmed defect

`GeneratedCurtainFrameHealthService.Inspect(ProjectState)` and its ownership-index traversal executed `if (element == null) continue;`. A malformed project containing a null semantic element could therefore be silently normalized by this standalone provider instead of failing visibly.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- `scripts/preflight-curtain-frame-null-health.py`
- this claim file

## Completed contract

- Direct Curtain Frame health inspection now throws `InvalidOperationException` on a null project element instead of silently skipping it.
- Ownership-index traversal now follows the same fail-visible contract.
- Existing valid-project handle/count/config/mode/staleness diagnostics remain in place.
- Composite health retains its existing `AddSafely` fail-visible boundary and `HEALTH_PROVIDER_FAILED` contract.
- Inspection remains read-only.

## Commits

- Claim: `a6782d5321bf8a431099aaafeeb1a9f362984d1c`
- Source fix: `eb3cce3bc3bcdd1344375120100fa1fdcc039b27`
- Regression gate: `21b199383876f900e94567c47a5faa5c89a9724e`

## Verification

Readback on `main` after the regression commit confirmed both null-entry traversals throw and the focused preflight is present. The preflight was not executed in this remote connector session, so no executable test/build PASS is claimed. No GitHub Actions/build/release was dispatched and no BricsCAD runtime PASS is claimed.
