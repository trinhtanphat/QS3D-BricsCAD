# MCP background BricsCAD host control

Issue: #4765  
Canonical carrier: `agent/chatgpt-gpt56sol-20260830-direct-tools/issue-4765-direct-diagnostics-theme-tools`

## Why this exists

The historical desktop MCP tools are intentionally explicit and guarded, but mouse/keyboard mutation uses Windows foreground focus, cursor movement and `SendInput`. That is appropriate only as a fallback because it competes with the person using the same Windows session. Window screenshots also historically sampled the virtual-desktop pixels inside a window rectangle, so an occluding window could corrupt the returned target image.

The background-host layer changes the default decision model for BricsCAD-hosted work:

1. direct CAD/QS3D API;
2. bounded BricsCAD command dispatch through the CAD application context;
3. same-process background host controls described here;
4. explicit foreground desktop fallback only after the user locally enables desktop control and MCP sets `foreground_fallback`.

The default policy after each plugin/process start is `background_only`.

## Direct tools

- `bricscad_interaction_policy_get` — read `background_only` vs `foreground_fallback`.
- `bricscad_interaction_policy_set` — set the policy; all modes require `confirmMutation=true`. Enabling `foreground_fallback` additionally requires current local desktop consent. A remote caller cannot silently turn on global mouse/keyboard takeover.
- `bricscad_ui_text_snapshot` — sensitive bounded read of visible text-bearing windows/controls owned by the current BricsCAD process. `scope=commandline` filters to Edit/RichEdit/command/prompt-like controls; `scope=popup` walks visible non-main top-level BricsCAD windows and their children; `scope=all` returns the bounded union. Requires `confirmSensitiveRead=true`.
- `bricscad_ui_invoke` — sends bounded `BM_CLICK` only to an exact visible standard `Button` HWND owned by the current BricsCAD process. Requires `confirmMutation=true`.
- `bricscad_ui_set_text` — sends bounded `WM_SETTEXT` only to an exact visible standard `Edit` or `RichEdit*` HWND owned by the current BricsCAD process. Requires `confirmMutation=true`.

All mutation calls continue through `McpCadAgentRuntime.Mutation`, so emergency-stop epoch validation and normal mutation confirmation remain authoritative.

## No-contention boundary

While the policy is `background_only`, these global desktop mutation tools fail before injection:

- `desktop_window_focus`
- `desktop_mouse_move`
- `desktop_mouse_click`
- `desktop_mouse_scroll`
- `desktop_mouse_drag`
- `desktop_type`
- `desktop_key`
- `desktop_clipboard_write`
- `desktop_sequence`

Read-only desktop observation remains available under its existing confirmation/consent rules. The existing foreground tools are not deleted because some third-party or custom-rendered workflows cannot be driven by APIs/window messages; they become an explicit opt-in fallback instead of the normal path.

Foreground fallback still requires the existing non-persistent QS3D desktop-consent session on every guarded desktop mutation. The interaction-policy flag is process-memory-only and resets to `background_only` on restart.

## Screenshot behavior

`desktop_screenshot scope=window` captures the validated target window into an off-screen compatible bitmap through Windows `PrintWindow(PW_RENDERFULLCONTENT)` before applying the bounded crop/scale/PNG rules. It does not focus the window and does not sample whatever other application currently occupies the target screen rectangle.

`scope=screen` intentionally remains the existing bounded virtual-desktop `BitBlt` capture.

`PrintWindow` is a best-effort Windows rendering API. A minimized, GPU-composed or custom-rendered surface may return incomplete/blank pixels; the MCP must report failure rather than silently stealing focus or substituting unrelated desktop pixels. Issue #4689 / PR #4767 separately owns converting the screenshot payload into native MCP `image/png` content without Base64 duplication.

## BricsCAD command line, popups and logs

For commands, prefer direct CAD/QS3D tools (`cad_*`, `cad_command_sequence`, `qs3d_run_command`) rather than typing into the command line. `bricscad_ui_text_snapshot` exists to inspect command-line/status/popup text when Windows exposes it as ordinary HWND text. It does not use OCR and does not persist captured text into the MCP audit stream; audit records contain only scope/count metadata.

Issue #4689 / PR #4767 owns the passive same-process popup observer so modal warning/error text can enter the bounded diagnostics stream even while normal CAD command execution is blocked. Issue #4765 complements it with on-demand UI text inspection and direct diagnostics tools.

`McpDiagnosticHub`/the direct diagnostics tools remain the canonical log path for MCP transport/OAuth/QS3D/BricsCAD lifecycle errors. There is no arbitrary filesystem reader.

## Security invariants

- No arbitrary shell, PowerShell, cmd, process launch, script/eval or executable path.
- No arbitrary cross-process HWND control.
- Every background HWND is revalidated against `Process.GetCurrentProcess().Id`.
- UI text reads are explicit sensitive reads and bounded by item/per-control/total character ceilings.
- Captured UI text is returned to ChatGPT but not written into audit logs.
- Background Button/Edit operations use `SendMessageTimeout` with a short bound so a hung UI control cannot stall the MCP worker indefinitely.
- Button invoke accepts only standard `Button`; text set accepts only standard `Edit`/`RichEdit*` controls.
- Global mouse/keyboard fallback remains under local desktop consent, blue-overlay/emergency-stop behavior and mutation confirmation.

## Verification

Run:

```text
python scripts/preflight-mcp-background-host-control.py
python scripts/preflight-mcp-direct-diagnostics-theme.py
```

Then require fresh shared preflight/core/V25 plugin CI on the exact current candidate. Real Windows/BricsCAD behavior for PrintWindow, standard/control-class discovery and background messages remains `LOCAL_ONLY / PENDING_LOCAL` until exercised on the licensed host.