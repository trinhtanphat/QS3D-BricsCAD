# Agent work claim — Release #34 Model Health identity/baseline gates

- Status: `COMPLETED`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 13:35 Asia/Ho_Chi_Minh`
- Completed: `2026-08-12 13:39 Asia/Ho_Chi_Minh`

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

## Implementation

- claim: `3e0b840a60c0a95df336f1ed68f144f76106c566`
- delimiter-collision regression: `36693d8219e23857d2846253a370f78ead6f5873`
- baseline gate: `2d6847665f156fe7cc71b3d8acf0412a67940db9`
- identity-ambiguity gate: `c08f04828c06113c3a20e6b15813c6337c6a9b33`

## Evidence & limitations

Readback confirms the baseline gate now pins the existing length-prefixed `KeyPart` framing and the smoke contains an exact delimiter-collision counterexample. The identity gate now requires Room Finish null semantic entries to fail visibly so Comprehensive Health can surface `HEALTH_PROVIDER_FAILED`, while duplicate-id/Level/graph ambiguity assertions remain intact. Production health services were not changed. No GitHub Actions or licensed BricsCAD runtime was executed.
