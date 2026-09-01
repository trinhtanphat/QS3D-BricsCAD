# MCP desktop function-calling design

Issue: #4629

## Goal

Extend the existing embedded QS3D MCP server rather than creating a second transport. ChatGPT must keep the current OAuth/session-hardened `tools/list` + `tools/call` path and gain a separate, explicit `desktop_*` tool surface for bounded Windows desktop interaction.

## Architecture

- `McpEmbeddedServerV2.cs` remains the only MCP network transport and continues to own auth, sessions, JSON-RPC and result envelopes.
- `McpCadAgentRuntime.cs` remains the central automation stop/confirmation/audit authority. Its dispatcher recognizes desktop tools and routes desktop mutations through the existing `Mutation(...)` epoch gate.
- New `McpDesktopAutomationRuntime.cs` owns Win32/WPF desktop operations only. It must not launch processes, shells or arbitrary commands.
- Existing `cad_ui_*` behavior remains BricsCAD-process confined. Desktop automation uses distinct names so the safety boundary is visible to the model and user.

## Tool surface

Read-only:
- `desktop_cursor_position`
- `desktop_window_list`
- `desktop_foreground_window`

Mutating/input:
- `desktop_window_focus`
- `desktop_mouse_move`
- `desktop_mouse_click`
- `desktop_mouse_scroll`
- `desktop_type`
- `desktop_key`
- `desktop_clipboard_write`

Sensitive reads:
- `desktop_clipboard_read`
- `desktop_screenshot`

Every mutating/input call requires `confirmMutation=true` through the existing CAD runtime gate. Clipboard reads and screenshots require `confirmSensitiveRead=true`. Input targeting is limited to the current interactive Windows session; window handles are revalidated before focus/type/key operations. Text, list counts, click counts, wheel deltas, screenshot dimensions and screenshot output are bounded.

## Screenshot contract

`desktop_screenshot` captures either the virtual desktop or one validated visible window, scales down to a bounded maximum size, encodes PNG through WPF/GDI interop, and returns JSON containing MIME type, dimensions and base64. It does not write screenshots to disk.

## Safety and privacy

- No `Process.Start`, `cmd.exe`, `powershell.exe`, `CreateProcess` or arbitrary executable path surface.
- No process path enumeration. Window listing returns bounded handle/title/bounds/foreground metadata only.
- Emergency stop invalidates in-flight desktop mutation epochs through `McpCadAgentRuntime.Mutation` and per-input callback checks.
- Desktop reads/mutations use the existing local audit callback where appropriate without logging clipboard text, typed text or screenshot pixels.
- Real desktop behavior is LOCAL_ONLY until exercised on Windows with licensed BricsCAD and the real ChatGPT connector.
