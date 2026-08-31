# MCP restart persistence and screen-permission audit

Lane-Key: `issue-5062`

This audit closes two post-integration gaps in the ChatGPT MCP background-control flow: the foreground permission toggle must also own user-screen screenshot access, and restart-critical OpenAI Secure Tunnel state must be persisted with explicit verification instead of relying on UI focus events or silent text writes.

## Required behavior

- `background_only` denies global mouse/keyboard input and `desktop_screenshot`.
- The local foreground toggle is the explicit gate for user mouse, keyboard, and screen access.
- Same-process BricsCAD background controls and API-first CAD/QS3D tools remain available while the foreground toggle is OFF.
- An explicitly supplied Runtime API key is saved and read-back verified through Windows Credential Manager as part of tunnel Start.
- Runtime key resolution can fall back directly to the saved Windows credential.
- Tunnel ID, tunnel-client path, and autostart state use verified persistence and fail visibly if restart state cannot be saved.
- User-facing copy accurately describes Credential Manager persistence; no secret is written plaintext into QS3D config/timeline files.

## Validation

Run `scripts/preflight-mcp-background-host-control.py`, Shared CI preflight/core, deterministic smoke tests, and locked-reference BricsCAD V25 compile. Licensed interactive BricsCAD validation remains LOCAL_ONLY when a local QS3D connector is available.
