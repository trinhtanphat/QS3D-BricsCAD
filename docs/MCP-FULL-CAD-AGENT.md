# QS3D full CAD agent over MCP

Status: SOURCE HARDENING / PENDING_LOCAL  
Canonical issue: #4352  
Lane-Key: `issue-4352`  
Canonical PR: #4425  
Canonical session handoff: `docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md`

QS3D embeds the MCP endpoint directly in the BricsCAD plugin. End users do not need a second MCP repository, Node runtime, PowerShell setup, or command-shell workflow.

The active MCP implementation is modular: `McpEmbeddedServerV2.cs` owns loopback HTTP, bearer authentication, MCP protocol/session routing and tool schemas/results; `McpCadAgentRuntime.cs` owns BricsCAD database/editor operations, bounded command dispatch, BricsCAD-process-only UI input, emergency recovery and audit. The legacy monolithic `McpEmbeddedServer.cs` remains only as historical source and is explicitly excluded from V25 and V26 compilation.

## End-user path — no terminal

The default UI is `TOOL > MCP (AI) > Cài đặt MCP` or `AI Dashboard`. Both route to `QS3DMCPAGENTCENTER`, the single click-first Agent Center.

1. Open BricsCAD with QS3D loaded and open **MCP Agent Center**.
2. Click **Cài / cập nhật Cloudflare Tunnel tự động**. `McpCloudflaredBootstrapper` downloads the official Windows `cloudflared` executable into the current-user QS3D folder, applies conservative size bounds, verifies Windows Authenticode trust and a Cloudflare signer identity, then adopts the binary. Failed replacement attempts retain/restore the prior working binary when possible.
3. Click **Đăng nhập Cloudflare + tạo Named Tunnel**. `cloudflared tunnel login` opens Cloudflare's provider-owned browser page. Username/password are typed only there; QS3D never asks for or stores the Cloudflare password.
4. Enter a public hostname such as `qs3d.example.com`. QS3D creates or safely reuses the exact `qs3d-bricscad` tunnel, creates the DNS route, writes hostname-scoped ingress to `http://127.0.0.1:8765`, and starts the tunnel.
5. Back in Agent Center, click **Copy MCP URL**, **Copy Bearer Token**, or **Copy URL + Authorization**, then **Mở ChatGPT**.
6. Add the copied HTTPS `/mcp` endpoint to the ChatGPT MCP/app setup and scan tools.
7. Click **Tự kiểm tra Agent (read-only)** for the built-in local MCP lifecycle/read-only sweep. Engineering/local agents can additionally run `scripts/test-mcp-loopback-readonly.py`; this script is not an end-user setup requirement and never prints the bearer token or calls mutation tools.
8. Future BricsCAD starts reuse the Named Tunnel configuration and attempt automatic reconnect.

Quick Tunnel remains a one-click test fallback only. The advanced token flow remains available only for users who already manage Cloudflare tunnels from the dashboard. No PowerShell or CMD is part of normal setup.

Agent Center also exposes **EMERGENCY STOP AGENT**, **Hủy command BricsCAD hiện tại (ESC x2)**, **Resume Agent**, protocol check, tunnel status, and the MCP audit folder.

## Managed cloudflared bootstrap

`McpCloudflaredBootstrapper` installs the verified executable under `%LOCALAPPDATA%\QS3D\MCP\bin\cloudflared.exe`. The bootstrap belongs to local owner-facing UI code; the network MCP transport cannot invoke it.

Acceptance requires HTTPS download from the official Cloudflare Windows release asset, bounded download size, `WinVerifyTrust` verification, Cloudflare signer inspection, safe replacement/rollback, and local path persistence without putting tunnel credentials on a command line. QS3D deliberately lets Windows perform normal certificate-chain construction rather than forcing cache-only trust evaluation.

## Public endpoint contract

`McpPublicEndpointResolver` is the single source for setup UI, Agent Center, dashboard, generated guide, `connector_info`, and copy helpers. Resolution precedence is account-managed Named Tunnel, token/Quick Tunnel, then optional `QS3D_MCP_PUBLIC_URL` fallback.

A displayed/copyable endpoint must be an absolute non-loopback `https://` URL with no user-info, query, or fragment. `/` is canonicalized to `/mcp`; unrelated paths are rejected. Provider-resolved endpoints are synchronized into current process state so the GUI and remote tool describe the same endpoint.

Convenience commands remain available but are not required by normal onboarding: `QS3DMCPAGENTCENTER`, `QS3DMCPCOPYURL`, `QS3DMCPCOPYTOKEN`, `QS3DMCPCOPYCONFIG`, and `QS3DMCPCHECKHTTP`.

## MCP transport and lifecycle

The active `McpEmbeddedServerV2` service listens only on `127.0.0.1:8765` and exposes minimal `GET /healthz`, MCP JSON-RPC `POST /mcp`, authenticated session `DELETE /mcp`, and `OPTIONS /mcp`. `GET /mcp` returns `405` because the server does not emit server-initiated SSE notifications.

The service supports MCP protocol `2025-06-18` with compatibility for `2025-03-26`: `initialize` returns `Mcp-Session-Id`, then the client sends `notifications/initialized` and uses `ping`, `tools/list`, and `tools/call`. Live sessions, concurrent clients, HTTP headers and bodies are bounded. Duplicate security-sensitive headers and `Transfer-Encoding` are rejected; MCP POST requires `application/json` plus bearer authentication.

`Origin` is treated as a security-sensitive singleton and validated before every route to prevent loopback DNS-rebinding attacks. Requests that omit `Origin` remain valid for non-browser MCP/tunnel clients. When an `Origin` header is present, it must be a clean absolute HTTP/HTTPS loopback origin; malformed, opaque/`null`, user-info/query/fragment/path-bearing, or non-loopback origins are rejected with HTTP `403`.

For an initialized session, an explicitly supplied `MCP-Protocol-Version` must exactly match the negotiated version stored in that session. Empty/mismatched versions are HTTP `400`. DELETE performs the same version check atomically with session lookup/removal; rejecting a bad-version DELETE must leave the session alive, while an unknown/expired/already-terminated session is HTTP `404`.

Mutation authorization is top-level only: nested `confirmMutation=true` does not authorize a mutation, and duplicate top-level `confirmMutation` is rejected fail-closed by the top-level JSON parser. The exact media-type gate rejects lookalikes such as `application/jsonevil`; `application/json` with optional parameters remains accepted.

Generated bearer tokens contain 32 random bytes encoded as 64 hexadecimal characters. An explicit `QS3D_MCP_BEARER_TOKEN` override must be at least 32 characters. Tool successes expose both ordinary MCP text content and `structuredContent.data`, so ChatGPT can consume object/array results without reparsing human prose.

The local protocol probe exercises real lifecycle calls, not only HTTP reachability. `scripts/test-mcp-loopback-readonly.py` additionally verifies hostile/opaque Origin rejection with loopback-Origin acceptance, bearer rejection, negotiated protocol-version mismatch rejection without accidental session termination, session creation/deletion, terminated-session HTTP `404`, the required tool set, and bounded observation calls while keeping output sanitized.

## Agent tool model

The production decision order is **direct CAD API first → bounded allowlisted native command workflow second → BricsCAD-process-only mouse/keyboard fallback last**.

Read/observation tools include `connector_info`, `qs3d_status`, `cad_active_document`, `cad_selection`, `cad_database_snapshot`, `cad_entity_inspect`, `cad_view_state`, `cad_wait_idle`, `cad_sysvar`, `cad_command_catalog`, and `cad_audit_tail`.

Direct transactional mutation tools include `cad_create_line`, `cad_create_circle`, `cad_create_arc`, `cad_create_polyline`, `cad_create_text`, `cad_create_mtext`, `cad_entity_transform`, `cad_entity_delete`, `cad_entity_set_layer`, `cad_layer`, and `qs3d_run_command`. Stable advanced/native workflows use `cad_command_sequence`. UI fallback uses `cad_ui_click`, `cad_ui_type`, and `cad_ui_key`. Recovery/control uses `cad_agent_stop`, `cad_agent_resume`, and `cad_cancel_command`.

Every ordinary mutation requires top-level `confirmMutation=true`. `cad_agent_stop` and `cad_cancel_command` intentionally remain confirmation-free so an operator can always stop an active action.

## Direct CAD database behavior

Database mutation is marshalled through `Application.DocumentManager.ExecuteInApplicationContext`. Direct entity changes use BricsCAD/Teigha transactions and document locks. Geometry values must be finite; handles, layer/text sizes, polyline vertices, database snapshot sizes and other caller-controlled values are bounded.

`cad_entity_inspect` reads a known handle and returns type/layer/extents plus useful geometry/text details for supported entity types. `cad_database_snapshot` returns a bounded ModelSpace view and reports whether more entities remain. `cad_sysvar` is read-only and only exposes a fixed privacy-reviewed allowlist.

Remote document identity is intentionally privacy-safe: active-document/status tools return a basename and booleans rather than the full local filesystem path or database handseed. `DWGNAME` is reduced to its filename before leaving the runtime.

### CAD dispatch timeout truth

`McpCadAgentRuntime` uses an atomic three-state dispatch transition: `Queued → Running` or `Queued → CancelledBeforeStart`. The application-context callback must atomically claim `Running` before executing work; a timeout can cancel only work that has not started. If the timeout observes that work already entered `Running`, the caller receives an explicit **completion is uncertain / do not retry automatically** error. This closes the previous check-then-act ambiguity without pretending a running BricsCAD mutation can be rolled back by a network timeout.

A client receiving the uncertain-completion result must inspect entity/database/audit state before deciding the next action. This is intentional exactly-once safety behavior, not a transport retry signal.

## Bounded BricsCAD command workflow

`cad_command_sequence` accepts one command from an explicit BricsCAD allowlist plus bounded newline-delimited prompt input. Coverage includes drawing/editing, hatch, dimensions, blocks/inserts, xrefs, layout/viewports, plot, open/save/save-as, undo/redo, cleanup, and selected 3D/native workflows.

It rejects forbidden control characters, excessive total/line size, continued input after a blank command terminator, and known CAD/QS3D command names injected as later prompt lines. It is not a shell, PowerShell surface, command prompt, or arbitrary process launcher. Direct API tools remain preferred when they can express the requested operation deterministically.

## BricsCAD-only UI fallback

UI automation uses Windows `SendInput`. Before click/type/key injection, QS3D verifies that the target HWND belongs to the current BricsCAD process. Click coordinates are client-relative and checked against the verified target window before `ClientToScreen`; input aborts if foreground ownership changes during the sequence. Printable Unicode typing is bounded and `Alt+F4` is blocked.

These tools are for BricsCAD dialogs, palettes, and ribbon actions when no stable API/command route exists. They are not desktop/browser-wide remote-control tools. The production MCP deliberately does not expose remote desktop capture; geometry/UI outcomes should be verified through database/entity/view state. Any future image tool requires a separately reviewed BricsCAD-window-only privacy boundary.

## Emergency stop and audit

`cad_agent_stop` immediately latches autonomous mutation/UI tools off and attempts ESC twice through CAD application context. If that path is unavailable or times out, the fallback sends ESC only after foreground/process ownership is verified as current BricsCAD. `cad_cancel_command` uses the same two-path cancellation model without changing the persistent stopped flag. `cad_agent_resume` requires `confirmMutation=true`.

Mutations are recorded to `%APPDATA%\QS3D\mcp-agent-audit.jsonl`. Audit storage is bounded/rotated and details are sanitized; typed text itself is not persisted in audit detail.

## Cloudflare onboarding behavior

The default path runs `cloudflared tunnel login`, so provider credentials stay in the Cloudflare browser. Before reusing local tunnel state, QS3D queries live provider tunnel state and accepts only the exact `qs3d-bricscad` name with matching local credentials. Stale saved IDs are not trusted solely because an old JSON file exists.

DNS-route errors are fail-closed; a generic `already exists` response is not silently accepted because the hostname could belong to another tunnel. Generated config uses hostname-scoped ingress to `http://127.0.0.1:8765` plus final `http_status:404` fallback. CLI output capture is bounded and asynchronously drained. Named, token, and Quick modes hand ownership to one another so QS3D does not intentionally run competing forwarders for the same endpoint.

The advanced token fallback protects a dashboard-issued token with Windows DPAPI `CurrentUser` and passes it through the child-process environment rather than the command line. Neither onboarding path exposes Cloudflare credentials through remote MCP.

## Full-drawing workflow

A capable ChatGPT client should run:

`inspect drawing -> establish layers/styles -> create/modify geometry -> hatch/annotate/dimension -> blocks/xrefs -> layout/viewports -> verify database/view state -> correct with move/trim/undo -> save -> plot/export`

Direct Arc and MText creation plus entity-layer reassignment reduce dependence on interactive commands for common drawing/annotation work. `cad_command_sequence` supplies the remaining complex native workflows, while mouse/keyboard is a last-resort UI bridge.

A successful tool response is not proof the drawing is correct. Re-read entity/database/view state, correct/undo when needed, and save/plot only after verification.

## Security boundary

The listener binds to loopback only and public access comes through the configured tunnel. `/mcp` requires constant-time bearer comparison. Present `Origin` headers are restricted to clean loopback HTTP/HTTPS origins before routing. The service bounds HTTP input, concurrent clients, sessions, snapshots, command prompt input, and audit storage; it has mutation confirmation, process-confined UI input, a CAD command allowlist, emergency stop, and local audit evidence.

Remote MCP does not expose PowerShell, `cmd.exe`, arbitrary shell execution, arbitrary process launch, tunnel setup, cloudflared installation, or desktop-wide input/capture. Cloudflare username/password are entered only in Cloudflare's browser flow. QS3D stores provider-issued tunnel material, its MCP bearer token, and non-secret local configuration needed for reconnect.

## LOCAL_ONLY qualification

Hosted source/CI can prove guards/builds but cannot prove licensed BricsCAD desktop behavior or a real ChatGPT-to-Cloudflare-to-BricsCAD session. Final runtime status remains `PENDING_LOCAL` until one exact candidate SHA passes the full matrix.

The local agent must use a clean exact SHA and disposable drawings, then prove V25 and V26 plugin load; active modular V2 server composition; Agent Center routing; verified GUI cloudflared install/update; `scripts/test-mcp-loopback-readonly.py` including Origin 403/loopback admission, negotiated protocol-version mismatch 400, rejected-DELETE session preservation and stale-session 404; browser login/Named Tunnel/DNS/autoreconnect; stale-tunnel and DNS-conflict fail-closed behavior; Quick Tunnel discovery; public endpoint/token copy consistency; ChatGPT Web tool discovery and structured results; read/entity/view/sysvar inspection; direct line/circle/arc/polyline/DBText/MText create/edit/delete/layer operations; bounded hatch/dimension/block/xref/layout/viewport/save/plot workflows; BricsCAD-process-only mouse/typing/keys and foreground-loss rejection; atomic timeout/no-auto-retry behavior; emergency stop/cancel/resume; tunnel-mode mutual exclusion; save/reopen round trip; and zero task-owned process residue.

Committed evidence must never contain bearer tokens, Cloudflare credentials, private paths, customer/private DWGs, proprietary BricsCAD binaries, or unsanitized screenshots. Record exact tested SHA and sanitized outcomes in the single matching item in `docs/LOCAL-AGENT-INBOX.md`. Until that matrix passes, report `PENDING_LOCAL`, never `LOCAL_PASS`.
