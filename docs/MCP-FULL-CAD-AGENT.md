# QS3D full CAD agent over MCP

Status: SOURCE_READY / PENDING_LOCAL
Canonical issue: #4352
Lane-Key: `issue-4352`

The QS3D BricsCAD plugin embeds the MCP endpoint directly. A second MCP repository, Node runtime, PowerShell setup, or command-shell workflow is not required for end users.

## End-user path — no terminal

The default UI is `TOOL > MCP (AI) > Cài đặt MCP` or `AI Dashboard`. Both route to `QS3DMCPAGENTCENTER`, a single click-first Agent Center.

1. Open BricsCAD with QS3D loaded and open **MCP Agent Center**.
2. Click **Cài / cập nhật Cloudflare Tunnel tự động**. QS3D downloads the official Windows `cloudflared` executable into the current user's local QS3D folder, checks its size, verifies Windows Authenticode trust, verifies the signer contains Cloudflare, and only then publishes the managed executable path. A failed replacement restores the previous working binary when possible.
3. Click **Đăng nhập Cloudflare + tạo Named Tunnel**. `cloudflared tunnel login` opens Cloudflare's provider-owned browser page. Username/password are typed only there; QS3D never asks for or stores the Cloudflare password.
4. Enter a public hostname such as `qs3d.example.com`, then let QS3D create/reuse the exact `qs3d-bricscad` tunnel, create the DNS route, write hostname-scoped ingress to `http://127.0.0.1:8765`, and start the tunnel.
5. Back in Agent Center, click **Copy MCP URL**, **Copy Bearer Token**, or **Copy URL + Authorization**, then click **Mở ChatGPT**.
6. Add the copied HTTPS `/mcp` endpoint to the ChatGPT MCP/app setup and scan tools.
7. Click **Tự kiểm tra Agent (read-only)** in QS3D to run a real local MCP lifecycle and read-only tool sweep without mutating the drawing.
8. Future BricsCAD starts reuse the named-tunnel configuration and attempt to reconnect automatically.

No PowerShell or CMD is part of the normal setup. Quick Tunnel remains a one-click test fallback only. The advanced token flow remains available only for users who already manage Cloudflare tunnels from the dashboard.

Agent Center also exposes always-available operator controls: **EMERGENCY STOP AGENT**, **Hủy command BricsCAD hiện tại (ESC x2)**, **Resume Agent**, protocol check, tunnel status, and the MCP audit folder.

## Managed cloudflared bootstrap

`McpCloudflaredBootstrapper` installs the official Cloudflare Windows amd64 executable to:

`%LOCALAPPDATA%\QS3D\MCP\bin\cloudflared.exe`

The bootstrap is owner-facing local UI code; the remote MCP server cannot invoke it. Acceptance requires all of the following before the executable path is adopted:

- HTTPS download from the official Cloudflare GitHub release asset.
- conservative downloaded-file size bounds;
- Windows `WinVerifyTrust` Authenticode verification;
- signer certificate inspection with Cloudflare identity;
- replacement rollback if swapping in the new verified binary fails;
- process/user `QS3D_CLOUDFLARED_PATH` persistence without putting tunnel credentials on a command line.

QS3D intentionally lets Windows perform normal certificate-chain construction. It does not use cache-only trust evaluation, because that can falsely reject a newly downloaded valid executable when an intermediate signer certificate is not already cached.

## Public endpoint contract

`McpPublicEndpointResolver` is the single source used by setup UI, Agent Center, dashboard, generated guide, and copy helpers. Resolution precedence is:

1. account-managed Named Tunnel;
2. token/Quick Tunnel;
3. optional `QS3D_MCP_PUBLIC_URL` fallback.

A displayed/copyable endpoint must be an absolute non-loopback `https://` URL with no user-info, query, or fragment. `/` is canonicalized to `/mcp`; any unrelated path is rejected. Provider-resolved endpoints are synchronized into the current process environment so `connector_info` and local status report the same endpoint after onboarding.

Convenience commands remain available for command-line-oriented CAD users, but are not required by normal onboarding:

- `QS3DMCPAGENTCENTER` — unified GUI.
- `QS3DMCPCOPYURL` — copy validated public `/mcp` URL.
- `QS3DMCPCOPYTOKEN` — copy embedded MCP bearer token.
- `QS3DMCPCOPYCONFIG` — copy URL plus `Authorization: Bearer ...`.
- `QS3DMCPCHECKHTTP` — local protocol/tool-call check.

## MCP transport and lifecycle

The embedded service listens only on `127.0.0.1:8765` and exposes:

- `GET /healthz` for minimal local health;
- `POST /mcp` for MCP Streamable HTTP JSON-RPC;
- `DELETE /mcp` with `Mcp-Session-Id` to close a session;
- `OPTIONS /mcp` for method discovery.

`GET /mcp` intentionally returns `405`; this server does not require an SSE stream because it does not emit server-initiated notifications.

The server supports MCP protocol `2025-06-18` and compatibility with `2025-03-26`. A client performs `initialize`, receives `Mcp-Session-Id`, sends `notifications/initialized`, then uses `ping`, `tools/list`, and `tools/call`. Sessions expire after four hours and both concurrent clients and total live sessions are bounded.

The local protocol probe exercises `initialize -> initialized -> tools/list -> tools/call connector_info -> ping -> DELETE`, not only HTTP reachability. Agent Center's read-only self-test additionally discovers the complete expected tool surface and calls multiple observation tools without drawing mutation.

HTTP framing is deliberately narrow: request/header/body sizes are bounded, duplicate security-sensitive headers are rejected, `Transfer-Encoding` is rejected, and MCP POST requires `Content-Type: application/json` plus bearer authentication.

## Agent tool model

The MCP surface follows an API-first rule. ChatGPT should use direct CAD database operations when a direct tool exists, the bounded BricsCAD command-line tool for stable native workflows, and mouse/keyboard only when an operation genuinely requires UI interaction.

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

Advanced/native workflow:

- `cad_command_sequence`

BricsCAD-window UI fallback:

- `cad_ui_click`
- `cad_ui_type`
- `cad_ui_key`

Recovery/control:

- `cad_agent_stop`
- `cad_agent_resume`
- `cad_cancel_command`

Every ordinary mutation requires `confirmMutation=true`. `cad_agent_stop` and `cad_cancel_command` intentionally remain confirmation-free so an operator can always stop an active action.

## Direct CAD database behavior

Database mutation is marshalled through `Application.DocumentManager.ExecuteInApplicationContext`. Direct entity creation/editing uses BricsCAD/Teigha transactions and document locks. Numeric geometry inputs must be finite. Handles are validated as hexadecimal entity handles. Polyline vertex count, text size, layer names, snapshot size, and other externally supplied values are bounded before use.

`cad_entity_inspect` reads one known handle without requiring a whole-database snapshot. `cad_database_snapshot` returns an explicitly bounded ModelSpace view and reports whether more entities remain beyond the requested limit.

Timed-out CAD-context work is marked cancelled before the caller returns. Synchronization lifetime is retained for a delayed BricsCAD callback and abandoned work owns final cleanup, avoiding timeout/use-after-dispose races.

## Bounded BricsCAD command workflow

`cad_command_sequence` is restricted to an explicit BricsCAD command allowlist. It covers drawing/editing plus hatch, dimensions, blocks, xrefs, layouts/viewports, plot, open/save, undo/redo, and cleanup workflows.

The tool accepts only one initial allowlisted command plus bounded newline-delimited prompt input. It rejects forbidden control characters, excessive prompt lines/line length, continued non-empty input after a blank command terminator, and known CAD/QS3D command names injected as later prompt lines. It is not a general shell, command prompt, PowerShell surface, or arbitrary process launcher.

Direct API tools should be preferred whenever they can express the requested geometry/edit deterministically.

## BricsCAD-only UI fallback

UI automation uses Windows `SendInput`, not legacy global mouse APIs. Before click/type/key injection it verifies that the foreground target belongs to the current BricsCAD process. Click coordinates are client-relative, checked against that verified target window, then converted with `ClientToScreen`. If the foreground window changes mid-operation, input stops rather than continuing into another application.

`cad_ui_type` accepts printable Unicode text and optional Enter. `cad_ui_key` accepts only a bounded named-key surface with explicit modifiers; `Alt+F4` is blocked.

These tools are fallback for BricsCAD dialogs, palettes, and ribbon actions; they are not browser/desktop-wide remote-control tools. ChatGPT should verify CAD state through database/entity/view tools after UI fallback operations rather than assuming a click succeeded semantically.

## Emergency stop and audit

`cad_agent_stop` immediately disables ordinary autonomous mutation/UI tools and attempts two ESC characters through the BricsCAD application context. If that dispatch is unavailable or times out because a CAD command owns the application context, it attempts a second path using foreground-process-verified `SendInput` ESC twice. `cad_cancel_command` uses the same two-path cancellation model without changing the persistent stopped flag.

`cad_agent_resume` requires `confirmMutation=true`.

Mutations are recorded to `%APPDATA%\QS3D\mcp-agent-audit.jsonl`. The audit file is size-bounded and rotates to `.1`; detail fields are length/control-character sanitized. Typed text content itself is not persisted in audit detail.

## Cloudflare onboarding behavior

The default path runs `cloudflared tunnel login`, which opens the provider browser. Before reusing local tunnel state, QS3D asks `cloudflared tunnel list` for live provider state and accepts only the exact `qs3d-bricscad` tunnel name plus a matching local credential file. A stale local tunnel ID is not trusted merely because an old JSON file still exists.

If the named tunnel does not exist, QS3D creates it and verifies its UUID/credential material. DNS-route errors are fail-closed: a generic `already exists` response is not silently accepted because that hostname could belong to another tunnel. The generated configuration uses hostname-scoped `ingress` to `http://127.0.0.1:8765` plus final `http_status:404` fallback.

Cloudflared CLI stdout/stderr is consumed asynchronously with bounded captured output and is drained before disposal. Long-lived process ownership is assigned before exit events are enabled to avoid an immediate-exit race leaving a dead process stored as active.

Quick Tunnel URL discovery is asynchronous, so the setup window polls at 500 ms for a bounded ten-second window and refreshes the public endpoint when the `trycloudflare.com` URL appears.

Named-browser, token, and Quick modes hand ownership to one another so QS3D does not intentionally run multiple `cloudflared` forwarders for the same embedded MCP endpoint at once.

The advanced token fallback protects a dashboard-issued token with Windows DPAPI `CurrentUser` and passes it to `cloudflared` through the process environment rather than the command line. Neither onboarding path exposes Cloudflare credentials through the network MCP server.

## Full-drawing workflow

A capable ChatGPT client can run a loop such as:

`inspect drawing -> establish layers/styles -> create/modify geometry -> hatch/annotate/dimension -> blocks/xrefs -> layout/viewports -> verify database/view state -> correct with move/trim/undo -> save -> plot/export`

Direct tools should be preferred for deterministic geometry and handle-based edits. `cad_command_sequence` supplies remaining stable native command-line workflows. Mouse/keyboard fallback is for UI-only dialogs or ribbon/palette actions with no stable API/command-line route.

A successful remote tool response is not by itself proof that the drawing is correct. The agent should re-read entity/database/view state, use undo/correction when needed, and only save/plot after verification.

## Security boundary

The MCP listener binds to loopback only. Public access is through the configured tunnel. `/mcp` requires a bearer token compared in constant time. The server bounds HTTP input, concurrent clients, live sessions, database snapshots, CAD prompt input, and audit storage. It has explicit mutation confirmation, BricsCAD-process-only UI targeting, a CAD command allowlist, emergency stop, and local audit evidence.

The network MCP server does not expose PowerShell, `cmd.exe`, arbitrary shell execution, arbitrary process launch, tunnel setup, or cloudflared download/install. Process launch exists only in local owner-facing onboarding/document-opening code, never as an MCP tool.

Cloudflare username/password are provider-owned credentials entered only in the Cloudflare browser-login flow. QS3D stores only provider-generated local certificate/tunnel material needed by `cloudflared`, its own MCP bearer token, and non-secret local configuration such as hostname/autostart state.

## LOCAL_ONLY qualification

Hosted source/CI can prove source guards, Core tests, and managed-reference plugin compilation, but it cannot prove licensed BricsCAD desktop input or a real ChatGPT-to-Cloudflare-to-BricsCAD session. Final runtime qualification therefore remains `PENDING_LOCAL` until a local agent tests the exact source SHA.

Required local matrix:

- Load the exact V25 plugin in licensed BricsCAD V25 and exact V26 plugin in licensed BricsCAD V26.
- Open Agent Center from both Ribbon MCP Settings and AI Dashboard buttons.
- On a machine without QS3D-managed cloudflared, click auto-install and verify the downloaded binary passes Authenticode/Cloudflare signer checks, appears under `%LOCALAPPDATA%\QS3D\MCP\bin`, and can be adopted without PowerShell/CMD/admin setup. Exercise a simulated failed replacement or locked destination and verify the previous binary is not lost.
- Verify embedded `/healthz`, bearer rejection/acceptance, `initialize`, initialized notification, `tools/list`, `tools/call connector_info`, `ping`, protocol/session binding, and session `DELETE`.
- Run Agent Center's read-only self-test and prove it performs no drawing mutation.
- Complete browser Cloudflare login from QS3D without terminal use; create/reuse the named tunnel and public hostname; restart BricsCAD and verify automatic reconnect.
- Verify stale saved tunnel IDs are not trusted, exact tunnel-name matching is used, missing local credentials fail closed, and a conflicting DNS route is not silently accepted.
- Start Quick Tunnel and verify the public URL appears in the setup UI within the bounded poll window without reopening the dialog.
- Verify Agent Center and `QS3DMCPCOPYURL` / `QS3DMCPCOPYTOKEN` / `QS3DMCPCOPYCONFIG` return the same validated endpoint/authentication material as dashboard and `connector_info`.
- Connect ChatGPT Web/custom MCP to public `/mcp` and verify discovery of the complete tool set above.
- Read active document, selection, database snapshot, one entity by handle, and view state.
- Create line/circle/polyline/text and verify native entities/layers/handles in the real DWG.
- Transform/delete entities and prove transaction consistency plus audit entries.
- Exercise command-sequence coverage for hatch, dimension, block/insert, xref, layout/viewport, save, and plot on a disposable drawing.
- Exercise BricsCAD-process-only mouse click, Unicode typing, and named-key input; verify out-of-window coordinates are rejected and foreground switching prevents injection.
- Trigger an active CAD command, click Agent Center emergency stop, verify either CAD-context ESC or foreground ESC fallback cancels it, prove subsequent mutation/UI requests are refused, then Resume explicitly.
- Exercise token/Quick fallback once and prove starting a fallback stops the browser-login named process rather than running two forwarding modes concurrently.
- Save/reopen the disposable DWG and verify the resulting drawing survives the full round trip.
- Never use customer/private DWGs or expose bearer tokens, Cloudflare credentials, private paths, or unsanitized screenshots in committed evidence.

Until this matrix passes on an exact source SHA, report it as `PENDING_LOCAL`, never `LOCAL_PASS`.
