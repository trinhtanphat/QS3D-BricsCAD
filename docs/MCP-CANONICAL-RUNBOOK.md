# QS3D MCP — CANONICAL START HERE

**Status:** CANONICAL MCP SOURCE-OF-TRUTH  
**Lane-Key:** `issue-4352`  
**Canonical Issue:** `#4352` — Production ChatGPT MCP onboarding + full CAD agent for QS3D  
**Canonical branch:** `agent/interactive-20260828-mcpui/issue-4352-gui-cloudflare-onboarding`  
**Runtime product:** QS3D BricsCAD V25/V26 plugin  
**End-user model:** click/browser-login first; no PowerShell/CMD and no second MCP repository

> **Agents:** if the task mentions MCP, ChatGPT Web, Cloudflare Tunnel, AI drawing automation, mouse/keyboard automation, `QS3DMCP*`, or the TOOL > MCP (AI) Ribbon area, read this file before changing MCP source. This document is the navigation map. Current source is implementation truth; older MCP notes are supporting history only.

## 1. One-sentence architecture

`ChatGPT Web -> HTTPS public /mcp -> Cloudflare Named Tunnel -> 127.0.0.1:8765/mcp -> McpEmbeddedServer inside the QS3D plugin -> BricsCAD application context -> direct CAD API / bounded native command / BricsCAD-only UI fallback`.

There is **one shipping repository**: `trinhtanphat/QS3D-BricsCAD`.

The separate historical `QS3D-CAD-MCP` repository is **not** a runtime dependency, is **not** required on the user's machine, and must not be reintroduced as a second server/clone/Node deployment unless the owner explicitly changes this architecture.

## 2. What the owner asked for

The completed design target from the MCP implementation session is:

- ChatGPT Web can connect to QS3D through MCP.
- MCP runs inside the QS3D BricsCAD plugin; no second project installation.
- End users do not use PowerShell/CMD for normal setup.
- Cloudflare authentication happens in the provider browser. QS3D never asks for or stores the Cloudflare password.
- Normal production exposure uses a persistent Cloudflare Named Tunnel; Quick Tunnel is test/fallback only.
- QS3D should automatically reuse/start the saved Named Tunnel on later BricsCAD starts.
- ChatGPT can inspect a drawing, create/edit geometry, annotate/dimension/hatch, work with blocks/xrefs/layouts/viewports, save/plot/export through bounded native CAD workflows, and use mouse/keyboard only as a fallback.
- UI automation must stay inside a window owned by the BricsCAD process.
- Mutations are confirmation-gated, audited, cancellable, and emergency-stoppable.
- No remote PowerShell, `cmd.exe`, arbitrary shell, arbitrary process execution, or desktop-wide mouse/keyboard control is exposed by the MCP server.

## 3. Files to read — exact order for MCP work

After the repository governance bootstrap (`AGENTS.md`, runtime contract, main-write policy, product boundary, CI policy), read these MCP files in this order:

1. `docs/MCP-CANONICAL-RUNBOOK.md` — **this file; navigation/source-of-truth**.
2. `src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs` — protocol, auth, tool registry, CAD API, command bridge, UI automation, audit/safety.
3. `src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs` — default click-first browser-login Named Tunnel flow.
4. `src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs` — advanced token mode + Quick Tunnel fallback.
5. `src/QS3D.BricsCAD.V25/McpPublicEndpointResolver.cs` — canonical public MCP URL resolution/validation.
6. `src/QS3D.BricsCAD.V25/McpConnectorRibbonCommands.cs` — TOOL/Ribbon user commands, MCP protocol probe, dashboard/docs entry points.
7. `src/QS3D.BricsCAD.V25/PluginEntry.cs` — embedded server + saved tunnel startup/shutdown lifecycle.
8. `src/QS3D.BricsCAD.V25/Ribbon/McpRibbonCommandOverride.cs` and `RibbonInitializationCoordinator.cs` — MCP Ribbon routing/order.
9. `scripts/preflight-embedded-mcp.py`, `scripts/preflight-mcp-full-agent.py`, `scripts/preflight-blt3d-tool-ribbon.py` — source contracts.
10. `docs/MCP-FULL-CAD-AGENT.md` — detailed functional model + local end-to-end qualification matrix.
11. `docs/CHATGPT-MCP-INTEGRATION.md` — supporting integration background; do not treat older wording there as overriding this canonical runbook/current source.
12. `docs/LOCAL-AGENT-INBOX.md` — live LOCAL_ONLY status/evidence queue.

If an old handoff, chat summary, issue comment, or separate MCP-repo document disagrees with this runbook/current source, **current source wins** and this runbook should be updated on the same canonical lane.

## 4. Runtime topology

```text
ChatGPT Web / custom MCP app
        |
        | HTTPS + bearer auth
        v
https://<public-host>/mcp
        |
        | Cloudflare Named Tunnel
        v
http://127.0.0.1:8765/mcp
        |
        v
McpEmbeddedServer (inside QS3D.BricsCAD.V25/V26 plugin)
        |
        +--> direct BricsCAD/Teigha database API tools
        |
        +--> bounded allowlisted native BricsCAD command sequencing
        |
        +--> BricsCAD-process-window-only mouse/keyboard fallback
        |
        v
BricsCAD active document / native viewport / DWG database
```

The listener must remain loopback-only. Cloudflare provides the public HTTPS boundary; the plugin must not bind the MCP server directly to a public network interface.

## 5. End-user setup — normal path

The intended user experience is deliberately simple:

1. Open BricsCAD with QS3D loaded.
2. Open `TOOL > MCP (AI) > Cài đặt MCP`.
3. If `cloudflared` is missing, click the install/download action and complete the normal Windows installer UI.
4. Click **Đăng nhập Cloudflare**.
5. Cloudflare opens in the browser; user types username/email/password **on Cloudflare's page**.
6. Back in QS3D, enter a public hostname such as `qs3d.example.com`.
7. Click **Tạo / reuse tunnel + kết nối**.
8. QS3D creates/reuses tunnel `qs3d-bricscad`, creates the DNS route, writes local tunnel config, starts it, and marks it for autostart.
9. Click **Copy MCP URL** and **Copy Bearer Token**.
10. Click **Mở ChatGPT**, create/scan the custom MCP app, and connect to the copied `https://<host>/mcp` endpoint.
11. On later BricsCAD starts, `PluginEntry` starts embedded MCP and attempts to restart the saved Named Tunnel automatically.

**Do not require PowerShell/CMD in the normal user path.** Terminal commands may exist only as developer diagnostics, not as the UX contract.

## 6. Cloudflare behavior

### Default production path

`McpCloudflareAccountTunnelManager` owns browser-auth/account mode:

- uses `cloudflared tunnel login` to open provider browser auth;
- expects provider-created local certificate state, not a QS3D password field;
- creates/reuses the stable tunnel name `qs3d-bricscad`;
- creates the hostname DNS route;
- stores local tunnel UUID/hostname/config/autostart state under the current user's QS3D application-data area;
- starts the saved named tunnel on later plugin initialization;
- never stores Cloudflare username/password.

### Advanced/fallback path

`McpCloudflareTunnelManager` owns:

- dashboard-issued tunnel-token mode;
- Windows DPAPI-protected token storage for that advanced mode;
- Quick Tunnel test mode;
- `cloudflared` discovery and provider/browser links.

Quick Tunnel is **not** the normal production configuration.

## 7. Public endpoint truth

`McpPublicEndpointResolver` is the single resolver for the public endpoint reported to the rest of the plugin.

Priority is provider-managed/saved state first, advanced/quick state second, optional configured environment fallback last.

Only a non-loopback HTTPS endpoint with canonical `/mcp` path may be published. Private/local/documentation-range literal addresses are rejected. Do not scatter independent public-URL resolution logic across new MCP files.

## 8. Embedded MCP protocol contract

`McpEmbeddedServer` owns the actual Streamable-HTTP-style MCP endpoint:

- local MCP: `http://127.0.0.1:8765/mcp`;
- health: `http://127.0.0.1:8765/healthz`;
- bearer authentication is mandatory on `/mcp`;
- bounded headers/body and bounded concurrent clients;
- JSON-RPC 2.0 validation;
- MCP initialize/session handling and session expiry;
- supported protocol versions are defined in source;
- `tools/list`, `tools/call`, `ping`, initialized/cancel notifications, session delete;
- no arbitrary OS shell/process execution surface.

Token precedence is implemented by source. Treat the token as a secret and never commit it or copy it into sanitized evidence.

## 9. MCP tool surface

The exact current tool registry lives in `McpEmbeddedServer.ToolsListResponse`; source is authoritative. Conceptually it is split as follows.

### Read/inspect

- `connector_info`
- `qs3d_status`
- `cad_active_document`
- `cad_selection`
- `cad_database_snapshot`
- `cad_view_state`
- `cad_wait_idle`
- `cad_command_catalog`
- `cad_audit_tail`

### Direct deterministic CAD mutations

- `cad_create_line`
- `cad_create_circle`
- `cad_create_polyline`
- `cad_create_text`
- `cad_entity_transform`
- `cad_entity_delete`
- `cad_layer`

These should be preferred over UI automation when they can perform the task deterministically through the native database API.

### Native command bridge

- `cad_command_sequence`
- `qs3d_run_command`
- `cad_cancel_command`

`cad_command_sequence` is allowlist-only and is intended for native CAD flows not yet represented by direct tools: hatch, dimensions, blocks/insert, xrefs, layout/viewport, plot, open/save/save-as, undo/redo, advanced geometry/edit/cleanup and similar explicitly allowed commands.

Do not replace the allowlist with unrestricted command strings. Do not add shell escape routes.

### BricsCAD-only UI fallback

- `cad_ui_click`
- `cad_ui_type`
- `cad_ui_key`

UI automation is a fallback, not the primary CAD API. It must verify the target window belongs to the current BricsCAD process and stop if focus changes. Do not broaden it into desktop/browser-wide remote control.

### Safety/control

- `cad_agent_stop`
- `cad_agent_resume`

Emergency stop/cancel must remain available when normal mutation tools are blocked. Resume requires explicit confirmation.

## 10. Full drawing strategy for ChatGPT agents

For a request such as “create a complete drawing”, the intended loop is:

```text
inspect active document
-> inspect existing geometry/layers/view
-> decide deterministic drawing plan
-> create/set layers
-> create geometry with direct tools when possible
-> modify/transform/delete as needed
-> run bounded native command flows for hatch/dim/block/xref/layout/plot/etc.
-> use mouse/keyboard only for a genuinely UI-only step
-> wait for CAD idle between asynchronous native commands
-> re-inspect database/view state
-> correct mistakes with direct edits / bounded command / undo
-> save
-> layout/plot/export as requested
-> verify final drawing state
```

The autonomous goal is **not** “blindly click the UI”. It is API-first, command-second, UI-fallback-last.

## 11. Mutation and safety invariants

All MCP work must preserve these invariants:

- ordinary mutations require `confirmMutation=true`;
- UI input only targets windows owned by the current BricsCAD process;
- input stops if foreground ownership changes;
- dangerous close shortcut such as Alt+F4 remains blocked from MCP UI automation;
- CAD command execution is allowlisted and bounded;
- command input chaining/control-character injection is rejected;
- QS3D command dispatch remains `QS3D*`-name constrained;
- emergency stop disables autonomous mutation/UI paths and attempts to cancel the active command;
- mutations are locally audited without logging typed text contents or secrets;
- MCP does not expose PowerShell, `cmd.exe`, arbitrary shell, arbitrary executable launch, credential harvesting, or desktop-wide control;
- Cloudflare password is provider-owned and browser-entered only.

If a proposed “convenience” feature breaks one of these invariants, do not implement it without explicit owner architecture/security approval.

## 12. Plugin lifecycle

`PluginEntry.Initialize()` is expected to:

1. initialize normal QS3D host services/Ribbon;
2. start embedded MCP fail-soft;
3. attempt saved Named Tunnel autostart fail-soft;
4. resolve/publish the canonical public endpoint;
5. continue normal plugin startup even if optional MCP/tunnel startup fails.

`PluginEntry.Terminate()`/teardown must stop both tunnel modes and embedded MCP without allowing one cleanup failure to strand the other host services.

V26 links the V25 adapter source into the V26 project. MCP changes in shared V25 source therefore require V25/V26 compatibility; do not fork a second independent V26 MCP implementation unless the host API genuinely requires it.

## 13. Ribbon/commands users and agents should know

Important command entry points include:

- `QS3DMCPACCOUNTSETUP` — default click-first Cloudflare/browser setup.
- `QS3DMCPSETUP` — advanced/Quick Tunnel fallback setup.
- `QS3DMCPSETTINGSHTTP` — connector settings/status.
- `QS3DMCPDOCSHTTP` — local guide.
- `QS3DMCPCHECKHTTP` — protocol check.
- `QS3DAIDASHBOARDHTTP` — MCP/AI dashboard.
- `QS3DMCPSTART` / `QS3DMCPSTOP` — embedded MCP service control.

The TOOL > MCP (AI) Ribbon should route to the embedded/browser-first flow, not to the historical legacy sidecar/Node MCP path.

## 14. What is source-complete vs what still requires real runtime proof

### Source-side implementation target

The repository now contains the intended embedded MCP architecture, click-first Cloudflare onboarding, public endpoint resolver, direct CAD tools, bounded native-command surface, BricsCAD-process-only UI fallback, confirmation/audit/emergency controls, Ribbon/lifecycle integration, preflight contracts and runbooks.

That means an agent should **not restart the design from scratch** or create another MCP repository/server when continuing this lane. Fix the current implementation in place.

### LOCAL_ONLY acceptance

Source review/CI cannot prove the full real-world chain:

`ChatGPT Web -> public Cloudflare hostname -> tunnel -> live embedded MCP -> licensed BricsCAD V25/V26 -> native DWG operations -> mouse/keyboard UI fallback -> save/reopen/plot`.

Until the exact candidate SHA passes that live matrix, status is `PENDING_LOCAL`, not `LOCAL_PASS` and not commercially “100% runtime-qualified”.

Use `docs/MCP-FULL-CAD-AGENT.md` for the detailed local matrix and register/update the matching live item in `docs/LOCAL-AGENT-INBOX.md`.

## 15. Agent continuation rules — do not get lost

When another agent receives “continue MCP”, “fix MCP”, “finish ChatGPT integration”, or similar:

1. Do repository governance bootstrap first.
2. Open Issue `#4352` and reuse its canonical lane unless it has been explicitly superseded/merged/closed.
3. Read this file.
4. Resolve the current canonical branch head; do not rely on a SHA copied from an old chat.
5. Read current MCP source files listed in section 3.
6. Search current diff/main before changing code; another agent may have landed a fix after an old handoff.
7. Fix current implementation in place. Do **not** open a parallel MCP carrier merely because CI is red or because the branch is behind.
8. If changing tool behavior, update the source guard/runbook in the same lane.
9. If changing a real-runtime scenario, update `docs/LOCAL-AGENT-INBOX.md` in the same lane.
10. Preserve the one-repo/no-terminal-end-user architecture unless the owner explicitly changes it.

## 16. Things agents must NOT do

Do not:

- require the user to clone/install `QS3D-CAD-MCP`;
- introduce Node as an end-user MCP runtime dependency;
- tell normal users to run PowerShell/CMD setup steps when the QS3D wizard can own the flow;
- ask QS3D users for their Cloudflare password inside QS3D;
- store Cloudflare username/password;
- expose the loopback listener directly on `0.0.0.0` or a public NIC;
- remove bearer auth just to simplify ChatGPT setup;
- replace the CAD command allowlist with arbitrary command execution;
- expose arbitrary process/shell execution to MCP;
- make mouse/keyboard desktop-wide;
- claim `LOCAL_PASS` from static source/CI evidence;
- create a duplicate MCP branch/issue/PR while `issue-4352` remains the canonical active carrier.

## 17. Debugging decision tree

If MCP is broken, follow this order instead of randomly searching the repository:

1. **Plugin not loaded?** Check `PluginEntry` and normal QS3D load diagnostics.
2. **Local MCP not running?** Check `QS3DMCPSTART`, `/healthz`, listener/port errors, then `QS3DMCPCHECKHTTP`.
3. **Protocol/auth failure?** Check bearer token, initialize/session headers, `tools/list`, current `McpEmbeddedServer` parsing/response logic.
4. **No public URL?** Check `McpPublicEndpointResolver`, saved Named Tunnel state, then advanced/Quick fallback.
5. **Cloudflare not connected?** Check browser authentication/certificate, saved tunnel UUID/credentials/config/hostname/DNS route and `cloudflared` process state.
6. **ChatGPT cannot discover tools?** Verify public `/mcp` reaches the local endpoint with auth and protocol probe before changing CAD tools.
7. **CAD read tool fails?** Check `ExecuteInApplicationContext`, active document and transaction/read boundaries.
8. **CAD mutation fails?** Check `confirmMutation`, emergency-stop state, document lock/transaction and command allowlist.
9. **Mouse/keyboard fails?** Check BricsCAD foreground/process-window ownership; do not weaken the confinement check to make it pass.
10. **V25 works/V26 fails?** Remember V26 links V25 source; fix shared-source compatibility deliberately.

## 18. Handoff template for future agents

A good MCP handoff is short and exact:

```text
Lane-Key: issue-4352
Canonical branch: <resolve current branch>
Current head: <resolve, never guess>
Canonical doc: docs/MCP-CANONICAL-RUNBOOK.md
Source status: <what is implemented / exact remaining source defect>
Runtime status: PENDING_LOCAL or LOCAL_PASS with exact evidence SHA
Files changed: <paths>
Do not redo: one-repo embedded MCP; browser-first Named Tunnel; API-first full CAD agent; BricsCAD-only UI fallback
Next action: <one concrete source/local/PR action>
```

Do not paste an entire chat transcript as the primary handoff. Capture durable decisions here and keep exact transient CI/SHA state in the canonical Issue/PR.

## 19. Definition of done for this MCP lane

The MCP lane is fully complete only when all applicable items are true:

- source implementation matches this architecture;
- source guards/build/CI are green for the final candidate;
- canonical branch is reconciled with current main as required;
- one canonical PR is protected-green and merged to `main`;
- exact resulting `main` contains the MCP implementation;
- live V25/V26 local MCP protocol + CAD tool matrix passes on an exact source SHA;
- browser Cloudflare login + Named Tunnel autostart passes;
- ChatGPT Web discovers tools through the public endpoint;
- disposable full-drawing workflow passes through create/edit/annotate/layout/save/plot/reopen;
- BricsCAD-window-only mouse/keyboard and emergency stop are proven;
- no secret/private/customer evidence is committed;
- `docs/LOCAL-AGENT-INBOX.md` records the exact local result.

Until then, report the exact incomplete layer rather than saying “100%” generically.
