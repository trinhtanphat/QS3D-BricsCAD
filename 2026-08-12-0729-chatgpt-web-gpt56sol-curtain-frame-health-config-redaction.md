# Work claim — curtain frame health config redaction

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-health-config-redaction`
- Registered: `2026-08-12T07:29:00+07:00`
- Completed: `2026-08-12T07:29:00+07:00`
- Baseline main SHA: `55692f337fc9278852880ca3ebd473643e9c8016`
- Priority: P1 — generated curtain-frame config diagnostics must not reflect raw validation exception detail or swallow unrelated failures.
- Task Key: `CORE-CURTAIN-FRAME-HEALTH-CONFIG-REDACTION`

## Confirmed defect

`GeneratedCurtainFrameHealthService.ValidateConfigFingerprint(...)` caught every `Exception` and appended `ex.Message` to `CURTAIN_FRAME_CONFIG_INVALID`. Config values originate from persisted Family/Instance properties, and local `Number(...)` validation names those persisted property keys in `InvalidOperationException`; `CurtainWallFrameFingerprint.Compute(...)` also performs argument validation. Because the provider handled the exception itself, aggregate health redaction could not sanitize the detail, and the broad catch hid unrelated exception classes.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- `scripts/preflight-curtain-frame-health-config-redaction.py`
- this claim file

`CurtainWallFrameFingerprint`, curtain builders/regeneration, ownership, runtime CAD health, and BricsCAD code were not modified.

## Completed implementation

- Claim registration: `c314dcf2cb91db82919afa278dd5f1005e9167ee`.
- Source fix: `7290ce07c560e78a654073102752a326bf19c8f3` (`fix(health): redact curtain frame config failures`).
- Focused regression gate: `6a32e6c8cc2e2a1287e5963f32f32ed1a32bd064` (`test(health): pin curtain frame config redaction`).
- `CURTAIN_FRAME_CONFIG_INVALID` remains `HealthSeverity.Warning` with stable actionable text and no raw exception detail.
- The catch now filters through `IsConfigDataFailure(...)`, limited to `InvalidOperationException` and `ArgumentException`; unrelated exception classes are no longer swallowed.
- Existing handle/count/grid/opening/ownership/stale/config-stale diagnostics and read-only inspection remain unchanged.

## Validation actually performed

- Re-fetched current `main` source after source/gate commits; the relevant source is blob `33cee0fb99773421b917b5da92e85d57e46a3508` with filtered catch and stable message.
- Re-fetched the focused gate from `main`; gate blob is `80fc0997080f654bac11da1741da8ae967f6e2c2` and pins Warning severity, bounded config exception family, absence of `ex.Message`, neighboring diagnostics, and read-only mutation exclusions.
- `CurtainWallFrameFingerprint.cs` was read only as evidence and was not edited.
- No GitHub Actions/build/release workflow was dispatched. No executable Core smoke, full solution build, or BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied on merged source contract: curtain-frame config validation remains fail-visible without raw exception detail, unrelated failures are not swallowed, focused regression coverage pins the contract, and this claim is closed `COMPLETED`.
