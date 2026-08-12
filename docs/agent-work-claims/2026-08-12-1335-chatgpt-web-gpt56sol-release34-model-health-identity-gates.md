# Agent work claim — Release #34 Model Health identity/baseline gates

- Status: `ACTIVE`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 13:35 Asia/Ho_Chi_Minh`

## Scope

Reconcile the Release #34 Model Health baseline and identity-ambiguity gates with current fail-closed production behavior. Lock the length-prefixed baseline identity key that prevents delimiter/newline collisions, add direct smoke coverage for the historical collision class, and require Room Finish diagnostics to fail visibly on null semantic entries rather than silently filtering them.

## Files

- `scripts/preflight-model-health-baseline.py`
- `tests/QS3D.Core.SmokeTests/ModelHealthBaselineSmoke.cs`
- `scripts/preflight-model-health-identity-ambiguity.py`
- this claim file

## Out of scope

- production `ModelHealthBaselineService` / `RoomFinishHealthService` behavior
- native V25/V26 diagnostics
- updater/signing/release behavior

## Acceptance checks

- baseline gate pins `KeyPart` length-prefix framing and no longer requires newline concatenation;
- smoke proves two diagnostics that collide under delimiter concatenation remain distinct;
- stale-code diagnostics remain message-insensitive while ordinary diagnostics remain message-sensitive;
- Room Finish identity gate requires explicit null-entry failure plus duplicate-id ambiguity handling;
- no diagnostic failure is silently suppressed.
