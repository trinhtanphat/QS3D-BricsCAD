# Work claim — curtain frame health config redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-health-config-redaction`
- Registered: `2026-08-12T07:29:00+07:00`
- Baseline main SHA: `55692f337fc9278852880ca3ebd473643e9c8016`
- Priority: P1 — generated curtain-frame config diagnostics must not reflect raw validation exception detail or swallow unrelated failures.
- Task Key: `CORE-CURTAIN-FRAME-HEALTH-CONFIG-REDACTION`

## Confirmed defect

`GeneratedCurtainFrameHealthService.ValidateConfigFingerprint(...)` catches every `Exception` and appends `ex.Message` to `CURTAIN_FRAME_CONFIG_INVALID`. Config values originate from persisted Family/Instance properties, and local `Number(...)` validation names those persisted property keys in `InvalidOperationException`; `CurtainWallFrameFingerprint.Compute(...)` also performs argument validation. Because the provider catches the exception itself, aggregate health redaction cannot sanitize the detail, and the broad catch hides unrelated exception classes.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

`CurtainWallFrameFingerprint`, curtain builders/regeneration, ownership, runtime CAD health, and BricsCAD code are excluded.

## Intended contract

- Preserve `CURTAIN_FRAME_CONFIG_INVALID` as a Warning for invalid current semantic/family config.
- Replace raw `Exception.Message` reflection with stable text.
- Catch only config validation failures (`InvalidOperationException` and `ArgumentException`); unexpected exception classes must propagate.
- Preserve all handle/count/stale/ownership/grid/opening diagnostics and read-only inspection.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Curtain-frame config validation remains fail-visible without raw exception detail, unrelated failures are not swallowed, focused regression coverage pins the source contract, and this claim is closed after merged-main readback.
