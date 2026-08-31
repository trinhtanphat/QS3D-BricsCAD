# MCP granular local permissions design

Lane-Key: `issue-5054`

## Goal

Make Agent Center show and enforce exactly which local machine-control capabilities ChatGPT MCP may use, while preserving API/background-first automation and the already-merged restart-safe credential persistence contract.

## Existing truth

- Runtime API key persistence is user-scoped through Windows Credential Manager and is verified by immediate read-back before publication into `CONTROL_PLANE_API_KEY`.
- The embedded MCP bearer token is persisted and verified before server publication; there is no ephemeral-process-token fallback.
- `background_only` is the process-start interaction policy.
- Same-process BricsCAD background host control already avoids global cursor/keyboard injection.
- Current Agent Center augmentation exposes only one combined foreground desktop toggle, so users cannot see or independently revoke screen, mouse, keyboard, or clipboard authority.

## Permission model

Add one process-memory-only permission state owned by local plugin/UI code. MCP calls may read permission status but may never grant a permission.

Permissions:

1. `BackgroundHostControl` — permits same-process BricsCAD UI text reads and bounded Button/Edit/RichEdit background operations. Default `true` for the BricsCAD process.
2. `ScreenRead` — permits `desktop_screenshot`. Default `false`.
3. `MouseInput` — permits `desktop_window_focus`, `desktop_mouse_move`, `desktop_mouse_click`, `desktop_mouse_scroll`, and `desktop_mouse_drag`. Default `false`.
4. `KeyboardInput` — permits `desktop_type` and `desktop_key`. Default `false`.
5. `ClipboardAccess` — permits `desktop_clipboard_read` and `desktop_clipboard_write`. Default `false`.

Foreground permissions remain subordinate to the existing local desktop-consent boundary and `foreground_fallback` policy. Enabling a foreground permission locally may resume local desktop consent and set `foreground_fallback`; disabling the last foreground permission returns interaction policy to `background_only` and revokes foreground desktop consent without stopping API/background automation.

`desktop_sequence` is evaluated step-by-step against the permissions required by its contained tools; the sequence itself must not become a bypass around granular checks.

Read-only desktop metadata (`desktop_cursor_position`, `desktop_window_list`, `desktop_foreground_window`, `desktop_wait_for_window`) remains available because it neither captures screen pixels nor injects global input. Existing mutation/sensitive-read confirmations remain unchanged.

## Agent Center UX

Replace the single coarse foreground-toggle action with a local permission group rendered next to the existing Resume desktop area:

- checkbox: `Chạy nền trong BricsCAD (không chiếm chuột/phím)`
- checkbox: `Cho phép đọc/chụp màn hình`
- checkbox: `Cho phép điều khiển chuột`
- checkbox: `Cho phép nhập bàn phím`
- checkbox: `Cho phép đọc/ghi clipboard`

The background checkbox is checked by default. Foreground checkboxes are unchecked by default and are process-memory-only so a BricsCAD restart fails closed again. The UI periodically refreshes from authoritative runtime state and must not pretend a failed permission change succeeded.

A compact status line explains that background control uses direct CAD/QS3D APIs and same-process BricsCAD controls without moving the user's cursor; foreground rights are explicit fallbacks.

## Enforcement

Introduce a focused `McpLocalControlPermissions` runtime responsible only for permission state and local grant/revoke methods. `McpDesktopAutomationRuntime` and `McpBackgroundHostRuntime` consult it before executing applicable tools.

Tool denial is fail-closed and names the missing local permission. Existing `confirmMutation`, `confirmSensitiveRead`, local desktop consent, emergency stop, target validation, bounded payloads, and interaction-policy checks remain additional independent gates.

`bricscad_interaction_policy_set` remains unable to create local permission authority. Setting `foreground_fallback` remotely still requires local desktop consent, and actual desktop tool calls additionally require the matching granular permission.

## Credential persistence

Do not weaken credential handling. The canonical runbook must be updated to remove stale statements saying the OpenAI Runtime API key is RAM-only/not persisted. It must describe Windows Credential Manager persistence with exact read-back verification and clarify that the key is not stored in plaintext QS3D config/logs.

Preview updater scope remains limited to the existing plugin/Core payload and must not delete or overwrite MCP bearer files or Windows Credential Manager state.

## Tests and validation

Add a deterministic source preflight that proves:

- permission defaults and tool mappings;
- foreground grants are local-only and process-memory-only;
- background host tools require `BackgroundHostControl`;
- screen/mouse/keyboard/clipboard tools require their exact permission;
- `desktop_sequence` performs per-step permission checks;
- Agent Center uses WPF `CheckBox` controls with stable tags/labels;
- the coarse legacy foreground toggle is removed;
- credential persistence and updater credential-protection guards remain present;
- canonical MCP docs describe the current persistent credential contract.

Run the new focused preflight plus existing MCP background-control and credential-persistence preflights. Protected PR `preflight` and `core` remain authoritative for merge. Licensed BricsCAD UI qualification may remain LOCAL_ONLY and is not inferred from hosted/static checks.
