# MCP local-control permission UI design

Lane-Key: `issue-5054`

## Goal

Make Agent Center clearly show the two real MCP local-control authority layers already implemented by QS3D: background BricsCAD/API control and explicit foreground desktop fallback, while preserving restart-safe Runtime API-key persistence.

## Existing truth

- `background_only` is the process-start default.
- Direct CAD/QS3D API, bounded command dispatch and same-process BricsCAD background-host controls do not require global mouse/keyboard injection.
- Foreground `desktop_*` mutation/sensitive-read work remains behind local `McpDesktopControlSession`, confirmation gates and `foreground_fallback` where applicable.
- Runtime API key persistence is user-scoped through Windows Credential Manager with exact read-back verification before publication into `CONTROL_PLANE_API_KEY`.
- The embedded MCP bearer token is durably verified before publication and has no ephemeral-process-token fallback.

## UI model

Render one compact permission panel beside the existing Resume desktop control:

1. checked read-only checkbox: `MCP chạy nền BricsCAD/API (không chiếm chuột/phím): BẬT`;
2. local interactive checkbox: `Cho phép chuột / bàn phím / màn hình user`.

The first checkbox explains the always-available/default API/background path rather than creating a second remote permission grant. The second checkbox is the real local foreground consent switch.

A compact status line explains that background is the normal path and foreground is only a fallback when desktop interaction is genuinely required.

## Foreground behavior

Turning the foreground checkbox ON locally must:

1. call `McpDesktopControlSession.ResumeFromLocalUser()`;
2. set `bricscad_interaction_policy_set` to `foreground_fallback` with mutation confirmation;
3. show the authoritative state on the next UI refresh.

Turning it OFF must:

1. set policy back to `background_only`;
2. call `DisableForegroundAccessFromLocalUser` so global desktop reads/input stop;
3. leave background CAD/API automation alive.

Any failure is fail-closed: restore `background_only` where possible, revoke foreground desktop consent and report a bounded local error without throwing through the WPF dispatcher.

Remote MCP still cannot silently enable local desktop consent. Existing `confirmMutation`, `confirmSensitiveRead`, target validation, bounded payloads, mutation epoch and physical Esc×2 Emergency Stop remain independent safety gates.

## Credential persistence

Do not weaken the merged credential contract. A Runtime API key typed in Agent Center is written to Windows Credential Manager, read back and compared exactly, then published into the current process. V25/V26 startup restore it before transport auto-start. It is not written plaintext into QS3D config/log files.

Preview updater scope remains outside MCP credential surfaces.

## Validation

Add a deterministic feature preflight proving the checkbox UI, two-mode policy/consent path, fail-closed behavior, Runtime API-key verification and updater credential isolation. Re-run the existing MCP background-host, credential-persistence and Agent Center UIUX guards. Protected PR `preflight` and `core` on the exact candidate remain authoritative for merge; hosted/static evidence is not licensed BricsCAD `LOCAL_PASS`.
