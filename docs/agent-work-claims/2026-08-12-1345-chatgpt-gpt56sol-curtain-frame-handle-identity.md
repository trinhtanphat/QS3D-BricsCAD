# Work claim — Curtain Frame numeric handle identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-curtain-frame-handle-identity-20260812-1345`
- Registered: `2026-08-12T13:45:00+07:00`
- Completed: `2026-08-12T14:28:00+07:00`
- Baseline main SHA: `56a482da853afe55000f2902e5792ba0d41340bd`
- Priority: P0 generated ownership/health identity parity
- Task Key: `CORE-CURTAIN-FRAME-HANDLE-NUMERIC-IDENTITY`

## Confirmed defect

`GeneratedCurtainFrameHealthService` validated generated frame handles as hexadecimal but then used trimmed textual spelling for local duplicate/count, provider-local ownership, `SourceHandles`, and live-CAD membership. The shared generated-handle contract canonicalizes positive hexadecimal CAD identities, so aliases such as `A`, `0A`, and `000A` could represent one CAD object while Curtain Frame health treated them as distinct. A persisted `GeneratedCurtainFrameHandles=000A` with live handle `A` could emit a false `CURTAIN_FRAME_GENERATED_SOLID_MISSING`, and `A;000A` could evade duplicate detection/count parity.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- `tests/QS3D.Core.SmokeTests/CurtainFrameHandleIdentitySmoke.cs`
- this claim file

## Completed contract

- preserved existing hexadecimal validity and whitespace diagnostics;
- normalized valid frame handles through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` before duplicate/count, ownership, source, and live checks;
- normalized the provider-local ownership index with the same identity contract;
- numeric aliases now represent one CAD object without changing persisted spelling or unrelated frame config/mode/geometry health behavior;
- inspection remains read-only.

## Evidence

- Claim registration: `d97b1cf12190683e734902be3f503b9c09e20410`
- Source commit: `4a6b1c2dc24a191ea055523d2681e9f671161741`
- Focused smoke commit: `baaa66f15a63959e7cc635e9d8351ce8abbf1c32`
- Pull request: `#935`
- Integration SHA: `610218b78f5d3af7ebb8ea850c7c2dc6f6fbb8ec`
- Main source blob after merge: `d7811be4d2e500a2914e0ec51bdf0aff55023d49`
- Main smoke blob after merge: `7a5b33cb41138e64f47220c1520e048e0a0a278d`
- Post-merge ancestry check: integration SHA is the exact merge base of `102cfbf2e0f6172efa71494227378a8762687789`; `behind_by=0`.

## Validation boundary

Focused auto-registered Core smoke source was added and read back on `main`. GitHub Actions, a full .NET build, the executable smoke process, and licensed BricsCAD V25/V26 runtime were not run and are not claimed as PASS.
