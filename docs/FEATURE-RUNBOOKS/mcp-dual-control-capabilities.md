# MCP dual-control capabilities

Lane-Key: `issue-5073`

## Contract

QS3D exposes two simultaneous MCP control capabilities for BricsCAD:

- **Background Control** is available and preferred by default. It uses direct CAD/QS3D APIs, bounded command dispatch, and same-process BricsCAD UI messaging. It must not seize global mouse, keyboard, foreground focus, clipboard, or user-screen access and must never silently fall back to foreground desktop automation.
- **Foreground Control** is an explicit local-user permission for `desktop_*` interactions that can use the user's mouse, keyboard, focus, clipboard, or screen. Enabling it requires current local desktop consent; losing that consent must fail closed and leave Background Control available.

`bricscad_interaction_policy_get` reports both capabilities plus `defaultRoute=background`, `fallback=explicit_only`, and `implicitForegroundFallback=false`.

## Agent Center

Agent Center shows both capability states. The foreground toggle is local-only. Turning it off, Pause Desktop, Emergency Stop, or consent revocation disables foreground access without disabling Background Control.

Runtime API-key text must continue to reflect restart-safe Windows Credential Manager persistence from PR #5063.

## Regression requirements

`scripts/preflight-mcp-dual-control-capabilities-v2.py` verifies the capability JSON contract, local consent checks, fail-closed UI wiring, and that `desktop_screenshot` remains behind the foreground/global-interaction gate.

Hosted Shared CI must pass exact-head `preflight` and `core`, including locked BricsCAD V25 compile. Licensed interactive BricsCAD verification remains `LOCAL_ONLY` and is not implied by hosted CI.
