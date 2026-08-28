# QS3D full CAD agent over MCP

Status: SOURCE_READY / PENDING_LOCAL
Canonical issue: #4352
Lane-Key: `issue-4352`

The QS3D BricsCAD plugin embeds the MCP endpoint directly. A second MCP repository, Node runtime, PowerShell setup, or command-shell workflow is not required for end users.

## End-user path

1. Open BricsCAD with the QS3D plugin loaded.
2. Open `TOOL > MCP (AI) > Cài đặt MCP`.
3. If Cloudflare Tunnel is not installed, use the installer button and complete the normal Windows installer UI.
4. Click `Đăng nhập Cloudflare`; credentials are entered only in Cloudflare's provider-owned browser page. QS3D never asks for or stores the Cloudflare password.
5. Enter the public hostname to use for QS3D, then click the automatic create/reuse/connect action.
6. Copy the displayed public MCP URL and bearer token, open ChatGPT custom MCP configuration, add the public `/mcp` endpoint with `Authorization: Bearer <token>`, then scan tools.
7. Future BricsCAD starts reuse the named-tunnel configuration and attempt to reconnect automatically.

Quick Tunnel remains a one-click test fallback only. The advanced token flow is retained for users who already manage Cloudflare tunnels from the dashboard. Named-browser, token and Quick modes explicitly hand tunnel ownership to one another so multiple `cloudflared` processes do not intentionally forward the same embedded MCP endpoint at the same time.

## MCP transport and lifecycle

The embedded service listens only on `127.0.0.1:8765` and exposes:

- `GET /healthz` for minimal local health.
- `POST /mcp` for MCP Streamable HTTP JSON-RPC.
- `DELETE /mcp` with `Mcp-Session-Id` to close a session.
- `OPTIONS /mcp` for method discovery.

`GET /mcp` intentionally returns `405`; this server does not need an SSE stream because it does not emit server-initiated notifications.

The server supports MCP protocol `2025-06-18` and compatibility with `2025-03-26`. A client performs `initialize`, receives `Mcp-Session-Id`, sends `notifications/initialized`, then uses `ping`, `tools/list`, and `tools/call`. Sessions expire after four hours and the server bounds both concurrent clients and total live sessions. The local Ribbon probe exercises `initialize -> initialized -> tools/list -> tools/call connector_info -> ping -> DELETE` rather than checking only HTTP reachability.

HTTP framing is deliberately narrow: request/header/body sizes are bounded, duplicate security-sensitive headers are rejected, `Transfer-Encoding` is rejected, and MCP POST requires `Content-Type: application/json` plus bearer authentication.

## Agent tool model

The MCP surface follows an API-first rule. ChatGPT should use direct CAD database operations when a direct tool exists, use the bounded BricsCAD command-line tool for advanced/native workflows, and use mouse/keyboard only when the operation genuinely requires UI interaction.

Read/observation tools:

- `connector_info`
- `qs3d_status`
- `cad_active_document`
- `cad_selection`
- `cad_database_snapshot`
- `cad_entity_inspect`
- `cad_view_state`
- `cad_wait_idle`
- `cad_command_catalog`
- `cad_audit_tail`

Direct native mutation tools:

- `cad_create_line`
- `cad_create_circle`
- `cad_create_polyline`
- `cad_create_text`
- `cad_entity_transform` (`move`, `rotate`, `scale`)
- `cad_entity_delete`
- `cad_layer` (`create`, `set_current`)
- `qs3d_run_command`

Advanced/native command workflow:

- `cad_command_sequence`

BricsCAD-window UI fallback:

- `cad_ui_click`
- `cad_ui_type`
- `cad_ui_key`

Recovery/control:

- `cad_agent_stop`
- `cad_agent_resume`
- `cad_cancel_command`

Every ordinary mutation requires `confirmMutation=true`. `cad_agent_stop` and `cad_cancel_command` intentionally remain confirmation-free so a remote operator can always stop an active action.

## Direct CAD database behavior

Database mutation is marshalled through `Application.DocumentManager.ExecuteInApplicationContext`. Direct entity creation/editing uses BricsCAD/Teigha transactions and document locks. Numeric geometry inputs must be finite. Handles are validated as hexadecimal entity handles. Polyline vertex count, text size, layer names, snapshot size and other externally supplied values are bounded before use.

`cad_entity_inspect` reads one known handle without requiring a whole-database snapshot. `cad_database_snapshot` returns an explicitly bounded ModelSpace view and reports whether more entities remain beyond the requested limit.

Timed-out CAD-context work is marked cancelled before the caller returns. Its synchronization object is not disposed while a delayed BricsCAD callback could still signal it, preventing timeout/use-after-dispose races on the CAD thread.

## Bounded BricsCAD command workflow

`cad_command_sequence` is restricted to an explicit BricsCAD command allowlist. It covers drawing/editing plus hatch, dimensions, blocks, xrefs, layouts/viewports, plot, open/save, undo/redo and cleanup workflows.

The tool accepts only one initial allowlisted command plus bounded newline-delimited prompt input. It rejects forbidden control characters, excessive prompt lines/line length, continued non-empty input after a blank command terminator, and known CAD/QS3D command names injected as later prompt lines. It is not a general shell, command prompt, PowerShell surface or arbitrary process launcher.

Direct API tools should be preferred whenever they can express the requested geometry/edit deterministically.

## BricsCAD-only UI fallback

UI automation uses Windows `SendInput`, not legacy global mouse APIs. Before any click/type/key injection it verifies that the foreground target belongs to the current BricsCAD process. Click coordinates are client-relative, checked against that verified target window, then converted with `ClientToScreen`. If the foreground window changes mid-operation, input stops rather than continuing into another application.

`cad_ui_type` accepts printable Unicode text and optional Enter. `cad_ui_key` accepts only a bounded named-key surface with explicit modifiers; `Alt+F4` is blocked.

These tools are UI fallback for BricsCAD dialogs/palettes/ribbon actions, not browser/desktop-wide remote-control tools.

## Emergency stop and audit

`cad_agent_stop` immediately disables ordinary autonomous mutation/UI tools and attempts two ESC characters through the BricsCAD application context. If that dispatch is unavailable or times out because a CAD command owns the application context, it attempts a second path using foreground-process-verified `SendInput` ESC twice. `cad_cancel_command` uses the same two-path cancellation model without changing the persistent stopped flag.

`cad_agent_resume` requires `confirmMutation=true`.

Mutations are recorded to `%APPDATA%\QS3D\mcp-agent-audit.jsonl`. The audit file is size-bounded and rotates to `.1`; detail fields are length/control-character sanitized. Typed text content itself is not persisted in audit detail.

## Cloudflare onboarding behavior

The default path runs `cloudflared tunnel login`, which opens the provider browser. The plugin then creates or reuses the locally-managed `qs3d-bricscad` tunnel, creates the DNS route and writes the local tunnel configuration pointing to `http://127.0.0.1:8765`.

Cloudflared CLI stdout/stderr is consumed asynchronously with a hard capture bound, and the process is allowed to drain async output callbacks before its `Process` object is disposed. This avoids the pipe deadlock and callback/disposal race patterns that can occur with redirected CLI output.

The advanced token fallback protects the dashboard-issued token with Windows DPAPI `CurrentUser` and passes it to `cloudflared` through the process environment rather than the command line. Neither onboarding path exposes Cloudflare credentials through the network MCP server.

## Full-drawing workflow

A capable ChatGPT client can run a drawing loop such as:

`inspect drawing -> establish layers/styles -> create/modify geometry -> hatch/annotate/dimension -> blocks/xrefs -> layout/viewports -> verify database/view state -> correct with move/trim/undo -> save -> plot/export`.

Direct tools should be preferred for deterministic geometry and handle-based edits. `cad_command_sequence` supplies remaining stable native command-line workflows. Mouse/keyboard fallback is intended only for UI-only dialogs or ribbon/palette actions that have no stable API/command-line path.

## Security boundary

The MCP listener binds to loopback only. Public access is through the configured tunnel. `/mcp` requires a bearer token compared in constant time. The server bounds HTTP input, concurrent clients, live sessions, database snapshots, CAD prompt input and audit storage. It has explicit mutation confirmation, BricsCAD-process-only UI targeting, a CAD command allowlist, emergency stop and local audit evidence.

The network MCP server does not expose PowerShell, `cmd.exe`, arbitrary shell execution or arbitrary process launch. `Process.Start` exists only in local owner-facing onboarding/document-opening code, never as a remote MCP tool.

Cloudflare username/password are provider-owned credentials entered only in the Cloudflare browser-login flow. QS3D stores only the provider-generated local certificate/tunnel material needed by `cloudflared`, plus its own MCP bearer token.

## LOCAL_ONLY qualification

Hosted source/CI can prove source guards, Core tests and managed-reference plugin compilation, but it cannot prove licensed BricsCAD desktop input or a real ChatGPT-to-Cloudflare-to-BricsCAD session. Final runtime qualification therefore remains `PENDING_LOCAL` until a local agent tests the exact source SHA.

Required local matrix:

- Load the exact V25 plugin in licensed BricsCAD V25 and the exact V26 plugin in licensed BricsCAD V26.
- Verify embedded `/healthz`, bearer rejection/acceptance, `initialize`, initialized notification, `tools/list`, `tools/call connector_info`, `ping`, protocol/session binding and session `DELETE`.
- Complete browser Cloudflare login from the QS3D wizard without terminal use; create/reuse the named tunnel and public hostname; restart BricsCAD and verify automatic reconnect.
- Connect ChatGPT Web/custom MCP to the public `/mcp` endpoint and verify discovery of the complete tool set above.
- Read active document, selection, database snapshot, one entity by handle and view state.
- Create line/circle/polyline/text and verify native entities/layers/handles in the real DWG.
- Transform/delete entities and prove transaction consistency plus audit entries.
- Exercise command-sequence coverage for hatch, dimension, block/insert, xref, layout/viewport, save and plot on a disposable drawing.
- Exercise BricsCAD-process-only mouse click, Unicode typing and named-key input; verify out-of-window coordinates are rejected and foreground switching prevents injection.
- Trigger an active CAD command, call `cad_agent_stop`, verify either CAD-context ESC or foreground ESC fallback cancels it, prove subsequent mutation/UI requests are refused, then resume explicitly.
- Exercise token/Quick fallback once and prove starting a fallback stops the browser-login named process rather than running two forwarding modes concurrently.
- Save/reopen the disposable DWG and verify the resulting drawing survives the full round trip.
- Never use customer/private DWGs or expose bearer tokens, Cloudflare credentials, private paths or unsanitized screenshots in committed evidence.

Until this matrix passes on an exact source SHA, report it as `PENDING_LOCAL`, never `LOCAL_PASS`.
