# MCP dual-control capabilities

Status: canonical feature runbook for Issue #5051 / PR #5053.

## Intent

QS3D exposes two BricsCAD control capabilities at the same time. They are not mutually exclusive modes of the whole MCP server.

### Background Control — preferred/default

Background Control is the default route for work that can be completed without taking over the user's desktop. It includes direct `cad_*` tools, `qs3d_*` domain tools, bounded BricsCAD command dispatch, and the same-process `bricscad_ui_*` helpers.

Background Control must not move the global mouse cursor, inject global keyboard input, or steal foreground focus. The `bricscad_ui_*` helpers are deliberately bounded to HWNDs owned by the current BricsCAD process and use bounded window messages. Unsupported controls fail; they do **not** silently fall back to desktop automation.

### Foreground Control — explicit/local-consent gated

Foreground Control covers `desktop_*` operations that can use the user's mouse, keyboard, clipboard, focus, or visible desktop. It is available only when both conditions are true:

1. the interaction policy is `foreground_fallback`; and
2. the local desktop-consent session is enabled.

Remote MCP can never create local consent. The local user must explicitly enable it from Agent Center. Pause, Emergency Stop, Esc×2/local revocation, or a foreground synchronization failure must fail closed.

Enabling Foreground Control does not disable Background Control. Background remains available and preferred.

## Routing contract

- `defaultRoute = background`
- `fallback = explicit_only`
- `implicitForegroundFallback = false`
- Background request unsupported by the background surface => return an error; do not call `desktop_*` automatically.
- A caller that intentionally needs desktop interaction must explicitly call the foreground/desktop tool after local consent exists.

## Status contract

`bricscad_interaction_policy_get` reports both capabilities while preserving the legacy interaction-policy fields. The response includes:

```json
{
  "mode": "background_only|foreground_fallback",
  "globalInputAllowed": false,
  "defaultMode": "background_only",
  "processScoped": true,
  "backgroundControl": {
    "available": true,
    "preferred": true,
    "usesGlobalInput": false
  },
  "foregroundControl": {
    "available": false,
    "localConsent": false,
    "policyEnabled": false,
    "usesGlobalInput": true
  },
  "defaultRoute": "background",
  "fallback": "explicit_only",
  "implicitForegroundFallback": false
}
```

`globalInputAllowed` and `foregroundControl.available` are strict combined state: both policy and live local consent must be enabled.

## Agent Center behavior

The local Agent Center shows separate **Thao tác nền · Background Control** and **Thao tác trực tiếp · Foreground Control** status text. The existing Resume/Pause/Emergency controls are synchronized by `McpPersistentAgentCenterAugmenter` so local Resume enables the foreground policy only after consent is granted, while Pause/Emergency disarm the foreground policy. The dedicated foreground toggle performs the same synchronization directly and fails closed on error.

If consent disappears independently (for example Esc×2), the augmenter detects the stale foreground policy and disarms it back to background-only.

## Security boundaries

Background-host code must not add generic shell/process execution or global input APIs such as `SendInput`, `SetCursorPos`, or `SetForegroundWindow`. Same-process HWND ownership must be verified immediately before bounded window messages.

Foreground desktop mutations remain subject to the existing local-consent and mutation-confirmation layers. This feature does not weaken those gates.

## Qualification

Source qualification is protected by `scripts/preflight-mcp-dual-control-capabilities-v2.py` plus the repository's normal protected PR CI. Licensed BricsCAD behavioral verification remains `LOCAL_ONLY`; CI success alone must not be reported as a local BricsCAD runtime PASS.
