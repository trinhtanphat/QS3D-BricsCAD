# Work claim — Curtain Frame numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-frame-handle-identity-20260812-1345`
- Registered: `2026-08-12T13:45:00+07:00`
- Baseline main SHA: `56a482da853afe55000f2902e5792ba0d41340bd`
- Priority: P0 generated ownership/health identity parity
- Task Key: `CORE-CURTAIN-FRAME-HANDLE-NUMERIC-IDENTITY`

## Confirmed defect

`GeneratedCurtainFrameHealthService` validates generated frame handles as hexadecimal but then uses trimmed textual spelling for local duplicate/count, provider-local ownership, `SourceHandles`, and live-CAD membership. The shared generated-handle contract canonicalizes positive hexadecimal CAD identities, so aliases such as `A`, `0A`, and `000A` can represent one CAD object while Curtain Frame health treats them as distinct. A persisted `GeneratedCurtainFrameHandles=000A` with live handle `A` can emit a false `CURTAIN_FRAME_GENERATED_SOLID_MISSING`, and `A;000A` can evade duplicate detection/count parity.

Current recent-commit/open-PR checks found no Curtain Frame standalone numeric-handle identity lane. The active Wall Mesh claim owns only `GeneratedWallMeshHealthService` and its focused smoke.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- `tests/QS3D.Core.SmokeTests/CurtainFrameHandleIdentitySmoke.cs`
- this claim file

## Intended contract

- preserve existing hexadecimal validity and whitespace diagnostics;
- normalize valid frame handles through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` before duplicate/count, ownership, source, and live checks;
- normalize the provider-local ownership index with the same identity contract;
- treat numeric aliases as one CAD object without changing persisted spelling or unrelated frame config/mode/geometry health behavior;
- keep inspection read-only.

## Validation boundary

Focused auto-registered Core smoke + source/readback only. No GitHub Actions, full executable smoke, or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
