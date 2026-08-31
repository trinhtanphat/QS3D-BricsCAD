# MCP local-control permission UI

Lane-Key: `issue-5054`

## User-visible authority model

Agent Center exposes the two real MCP authority layers as checkboxes next to the existing local desktop controls:

- `MCP chạy nền BricsCAD/API (không chiếm chuột/phím): BẬT` — checked/read-only indicator for the default API/background path. This includes direct CAD/QS3D APIs, bounded command dispatch and same-process BricsCAD background-host controls that do not inject global mouse/keyboard input.
- `Cho phép chuột / bàn phím / màn hình user` — local interactive checkbox for the explicit foreground desktop fallback.

Background is the normal/default path. Foreground is only a fallback for workflows that genuinely require `desktop_*` interaction with the Windows session.

## Foreground checkbox contract

Turning the foreground checkbox ON is a local-user action. It resumes `McpDesktopControlSession`, then sets `bricscad_interaction_policy_set` to `foreground_fallback`. Remote MCP cannot silently create that local consent.

Turning the checkbox OFF sets the policy back to `background_only`, then revokes foreground desktop consent with `DisableForegroundAccessFromLocalUser`. That OFF path deliberately does not stop API/background CAD automation.

Any UI/policy failure is fail-closed: QS3D attempts to restore `background_only`, revokes foreground consent and reports a bounded local error instead of rethrowing into the WPF dispatcher.

Existing independent safety gates remain authoritative: `confirmMutation`, `confirmSensitiveRead`, target/window validation, bounded payloads, mutation epoch, physical Esc×2 Emergency Stop and BricsCAD/QS3D shutdown.

## Runtime API-key persistence

The OpenAI Secure MCP Runtime API key is restart-safe for the current Windows user:

1. local Agent Center capture calls `McpPersistentUserSettings.SaveOpenAiRuntimeApiKey`;
2. the key is written as a Windows Generic Credential under `QS3D.BricsCAD.MCP.OpenAI.RuntimeApiKey`;
3. QS3D performs an exact read-back verification;
4. only after verification is the key published into process `CONTROL_PLANE_API_KEY`;
5. V25 and V26 startup call `ApplyStartupSecretsToProcessEnvironment` before transport auto-start, so the saved key is restored after BricsCAD restarts.

The Runtime API key is không ghi plaintext into QS3D config/log files. The embedded MCP bearer token likewise uses verified persistent storage and has no ephemeral-process-token fallback.

Preview updater payloads remain restricted to the plugin/Core update surface and must not delete or overwrite Windows Credential Manager state, the MCP bearer-token file or process credential state.

## Deterministic validation

Run:

```text
python scripts/preflight-mcp-granular-local-permissions.py
python scripts/preflight-mcp-background-host-control.py
python scripts/preflight-mcp-credential-persistence.py
python scripts/preflight-mcp-agent-center-uiux.py
```

Protected PR `preflight` and `core` SUCCESS on the exact candidate remain authoritative for merge. Hosted/static validation is not licensed BricsCAD `LOCAL_PASS`.
