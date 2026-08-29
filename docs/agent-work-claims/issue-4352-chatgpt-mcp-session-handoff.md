# #4352 — Canonical ChatGPT MCP / full-CAD-agent session handoff

**Status:** ACTIVE / SOURCE HARDENING / RUNTIME `PENDING_LOCAL`  
**Updated:** 2026-08-29 (UTC+7)  
**Lane-Key:** `issue-4352`  
**Canonical issue:** #4352 — Production ChatGPT MCP onboarding + full CAD agent for QS3D  
**Canonical branch:** `agent/interactive-20260828-mcpui/issue-4352-gui-cloudflare-onboarding`  
**Canonical PR:** #4425  
**Runtime qualification:** `PENDING_LOCAL` until an exact candidate SHA passes licensed Windows/BricsCAD/Cloudflare/ChatGPT end-to-end qualification.

> This file is the canonical handoff for the MCP work started in the 2026-08-28 ChatGPT session. Future agents must read this file first, then `docs/MCP-FULL-CAD-AGENT.md`, `docs/LOCAL-AGENT-INBOX.md`, `docs/AGENT-RUNTIME-CONTRACT.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, and `CI_POLICY.md` before changing this lane.

## 1. Product goal and non-negotiable UX

Ship a single QS3D-BricsCAD product installation that lets ChatGPT Web/custom MCP securely operate the loaded BricsCAD session and complete an end-to-end drawing workflow. The shipping/runtime dependency is **only `QS3D-BricsCAD`**; the separate `QS3D-CAD-MCP` repository is historical/reference/development material and must never become a second clone/install/runtime requirement for end users.

The product remains a BricsCAD-hosted plugin. The MCP server is embedded in the plugin assembly and binds to loopback only. The desired user experience is click-first: open `TOOL > MCP (AI) > Cài đặt MCP`, install/update `cloudflared` from the QS3D UI when needed, authenticate on Cloudflare's browser page, enter a public hostname, press connect, copy the generated MCP configuration, and open ChatGPT. End users must not need PowerShell, CMD, Git, Node.js, a second MCP repository, or hand-authored tunnel commands.

Cloudflare username/password are provider credentials. They are entered only on Cloudflare's browser page. QS3D must never ask for, inspect, log, or store the Cloudflare password.

## 2. Canonical architecture

```text
ChatGPT Web / custom MCP app
        |
        | HTTPS + Authorization: Bearer <QS3D token>
        v
Cloudflare Named Tunnel (production)
        |
        | local origin only
        v
127.0.0.1:8765/mcp
        |
        v
McpEmbeddedServerV2 transport inside QS3D-BricsCAD plugin
        |
        v
McpCadAgentRuntime
        |
        +--> direct BricsCAD/Teigha database API
        |      via ExecuteInApplicationContext + document lock + transaction
        |
        +--> bounded allowlisted BricsCAD command-line workflow
        |      via SendStringToExecute
        |
        +--> BricsCAD-process-only Win32 UI fallback
               mouse/keyboard only; never arbitrary desktop targeting
```

Health endpoint: `http://127.0.0.1:8765/healthz`.  
MCP endpoint: `http://127.0.0.1:8765/mcp`.  
Production public endpoint: `https://<configured-hostname>/mcp`.  
Quick Tunnel is a test fallback only.

## 3. Current MCP protocol/security contract

The active modular service implements Streamable-HTTP request/response behavior for MCP protocol `2025-06-18` with compatibility for `2025-03-26`. `McpEmbeddedServerV2` owns loopback HTTP, bearer authentication, exact `application/json` media-type admission, MCP protocol/session routing and tool schemas/results; `McpCadAgentRuntime` owns CAD/editor work. `initialize` creates a bounded session, returns `Mcp-Session-Id`, and post-initialize calls use that session. The server supports ping, initialized/cancelled notifications, `tools/list`, `tools/call`, and session DELETE. Sessions expire and are bounded; HTTP header/body sizes and concurrent clients are bounded.

`/mcp` requires bearer authentication. The bearer secret is either a sufficiently long `QS3D_MCP_BEARER_TOKEN` environment override or a cryptographically generated 32-byte token persisted in the QS3D application-data directory; it is never a Cloudflare account password. The listener binds only to `IPAddress.Loopback`.

Mutation confirmation is top-level only: nested `confirmMutation=true` never authorizes a mutation, and duplicate top-level `confirmMutation` is rejected fail-closed by the top-level JSON parser. HTTP media-type admission rejects lookalikes such as `application/jsonevil`; only `application/json` with optional parameters is admitted.

Remote MCP must never expose PowerShell, `cmd.exe`, arbitrary shell execution, arbitrary program launch, arbitrary desktop input, or unbounded CAD script execution. Keep request parsing fail-closed, reject malformed/duplicate security-sensitive headers, reject unsupported transfer encoding and JSON media-type lookalikes, and preserve no-store/nosniff response headers.

## 4. Tool surface and agent decision order

Agents must follow this priority: **direct CAD API tool first → bounded allowlisted command workflow second → mouse/keyboard UI fallback last**. Do not replace a deterministic CAD API operation with blind UI clicking.

Current source exposes/targets these tool groups:

- **Connector/status/read:** `connector_info`, `qs3d_status`, `cad_active_document`, `cad_selection`, `cad_database_snapshot`, `cad_entity_inspect`, `cad_view_state`, `cad_wait_idle`, `cad_sysvar`, `cad_audit_tail`.
- **Direct transactional CAD mutation:** `cad_create_line`, `cad_create_circle`, `cad_create_arc`, `cad_create_polyline`, `cad_create_text`, `cad_create_mtext`, `cad_entity_transform` (move/rotate/scale), `cad_entity_delete`, `cad_entity_set_layer`, `cad_layer`.
- **Native/full-workflow bridge:** `cad_command_catalog`, `cad_command_sequence`, `qs3d_run_command`.
- **UI fallback:** `cad_ui_click`, `cad_ui_type`, `cad_ui_key`.
- **Safety/recovery:** `cad_agent_stop`, `cad_agent_resume`, `cad_cancel_command`.

The bounded CAD-command catalog covers drawing/editing plus hatch, dimensions, blocks/inserts, xrefs, layouts/viewports, plot, open/save/save-as, undo/redo, cleanup and selected 3D/native workflows. Inputs are bounded by total size, line count and per-line size; known command chaining/control characters are rejected. `qs3d_run_command` accepts only one `QS3D*` command name matching the source allowlist pattern.

All ordinary mutation tools require a top-level `confirmMutation=true`. Emergency stop and cancel remain deliberately available without mutation confirmation. Emergency stop disables subsequent autonomous mutation/UI calls until explicit resume.

## 5. Full-drawing acceptance target

The final autonomous loop is:

```text
inspect document/model/view
-> plan layers/styles/geometry
-> create/modify native geometry
-> hatch/annotate/dimension
-> blocks/xrefs as needed
-> layout + viewport + page setup
-> inspect/verify/correct
-> save/reopen
-> plot/export
```

A feature is not complete merely because a command name appears in `tools/list`. Local qualification must prove ChatGPT can discover the tool, supply bounded arguments, observe the resulting drawing state, recover from a bad step using cancel/undo/emergency stop, and persist the disposable drawing through save/reopen.

### Visual/UI observation policy

Database/entity/view-state inspection is the canonical production verification path for CAD geometry. The current production MCP scope deliberately does **not** add a remote screenshot/desktop-capture tool: UI fallback stays process-confined and does not create a new image-exfiltration surface. If a later separately reviewed scope adds visual snapshot support, it must capture only a window owned by the current BricsCAD process, return a bounded image payload, avoid private/customer drawings in committed evidence, and add an explicit source guard plus local runtime cell. Desktop-wide screenshots are forbidden.

## 6. UI automation security invariants

Mouse and keyboard injection must target only an HWND owned by the current BricsCAD process. Before every injection, validate the process owner; mouse coordinates must be client-relative and inside the target window. If foreground ownership changes during a multi-input sequence, abort immediately. `Alt+F4` remains blocked. Typed content is bounded and control characters are rejected.

`cad_agent_stop` sets the stop flag immediately and sends ESC twice. `cad_cancel_command` also sends ESC twice. Source includes a foreground-BricsCAD fallback for the case where CAD application-context dispatch cannot be serviced. Never weaken this into global ESC/keyboard injection.

## 7. Cloudflare onboarding contract

### Default end-user path

1. User opens `TOOL > MCP (AI) > Cài đặt MCP` / Agent Center.
2. QS3D detects `cloudflared`; if absent/outdated, the GUI bootstrapper downloads the official Windows binary and verifies the expected publisher/signature policy before making it available to QS3D.
3. User clicks **Đăng nhập Cloudflare**; `cloudflared tunnel login` opens Cloudflare's provider browser page. Credentials stay with Cloudflare.
4. User enters a DNS hostname already under the Cloudflare account/zone.
5. QS3D creates or safely reuses the canonical named tunnel `qs3d-bricscad`, refuses ambiguous duplicate/stale ownership, creates the DNS route, writes the canonical ingress config to the loopback MCP origin, and starts the tunnel.
6. QS3D persists only the minimum provider-issued local certificate/tunnel credentials/config needed for reconnect and enables autostart.
7. UI exposes copy actions for the public MCP URL and bearer authorization plus a local MCP probe.

### Advanced/test path

Token-mode onboarding remains an advanced fallback with DPAPI protection. Quick Tunnel remains one-click testing only and must not be presented as the durable production configuration.

Named-tunnel and Quick-tunnel process ownership must remain mutually exclusive. Background output readers must be bounded and stale callbacks from an old process must not overwrite current tunnel status.

## 8. Main source/files to inspect before changing this lane

- `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` — active loopback HTTP transport, bearer auth, MCP protocol/session routing, tool schemas/results and exact JSON media-type admission.
- `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs` — active CAD/editor runtime: transactional API tools, atomic application-context dispatch, bounded native commands, BricsCAD-only UI input, recovery and audit.
- `src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs` — legacy historical monolith only; it is explicitly excluded from V25/V26 compilation and must not be treated as the active transport/runtime.
- `src/QS3D.BricsCAD.V25/McpTopLevelJson.cs` — security-sensitive top-level JSON/member parsing.
- `src/QS3D.BricsCAD.V25/McpConnectorRibbonCommands.cs` — local protocol probe/dashboard/user-facing connector commands.
- `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs` — unified click-first GUI and local read-only/self-test controls.
- `src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs` — default browser-login named-tunnel wizard.
- `src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs` — advanced token/Quick Tunnel fallback.
- `src/QS3D.BricsCAD.V25/McpCloudflaredBootstrapper.cs` — verified GUI cloudflared installation/update.
- `src/QS3D.BricsCAD.V25/McpPublicEndpointResolver.cs` — canonical public HTTPS `/mcp` resolution.
- `src/QS3D.BricsCAD.V25/Ribbon/McpRibbonCommandOverride.cs` and `RibbonInitializationCoordinator.cs` — TOOL/MCP routing/order.
- `src/QS3D.BricsCAD.V25/PluginEntry.cs` — embedded MCP/tunnel lifecycle ownership.
- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` and `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` — V25/V26 source composition, including explicit legacy-monolith exclusion.
- `scripts/preflight-embedded-mcp.py`, `scripts/preflight-mcp-full-agent.py`, `scripts/preflight-mcp-production-hardening.py`, `scripts/preflight-mcp-session-handoff.py`, `scripts/preflight-mcp-loopback-readonly.py` — source contracts.
- `scripts/test-mcp-loopback-readonly.py` — sanitized engineering/local-agent protocol + read-only tool probe; never an end-user setup requirement.
- `docs/MCP-FULL-CAD-AGENT.md` — end-user/runbook + local qualification matrix.
- `docs/LOCAL-AGENT-INBOX.md` — single live LOCAL_ONLY queue.
- this file — canonical cross-agent state/decision record.

## 9. Source-hardening checklist

A checked source item is **not** runtime evidence.

- [x] Embedded single-repository modular MCP listener/runtime; no Node/second MCP runtime.
- [x] Loopback binding + bearer auth + exact JSON media-type admission + bounded HTTP/session/concurrency behavior.
- [x] MCP initialize/ping/tools/list/tools/call/session lifecycle.
- [x] Direct read/inspection surface for document/selection/database/entity/view/idle/sysvar state.
- [x] Direct transactional line/circle/arc/polyline/DBText/MText create plus transform/delete/entity-layer/layer management.
- [x] Bounded allowlisted CAD command workflow covering advanced drawing/annotation/layout/save/plot classes.
- [x] `QS3D*` command dispatch boundary.
- [x] Mutation confirmation gate + audit log + rotation/sanitization.
- [x] BricsCAD-process-only mouse/keyboard fallback and foreground revalidation.
- [x] Emergency stop/resume/cancel paths with BricsCAD-only ESC fallback.
- [x] Click-first Cloudflare browser login + named tunnel + DNS route + canonical ingress + autostart.
- [x] GUI cloudflared bootstrap path; no terminal requirement for end user.
- [x] Quick Tunnel test fallback and advanced DPAPI token fallback.
- [x] Sanitized read-only loopback qualification probe with no mutation/shell/token output.
- [x] Production decision: no remote screenshot tool in the current MCP surface; use database/entity/view-state verification and keep any future BricsCAD-only image capture in a separate reviewed scope.
- [x] Final source audit for timeout/late-CAD-callback semantics: atomic `Queued → Running` / `Queued → CancelledBeforeStart` transitions prevent pre-start late mutation, while an already-running timeout explicitly reports completion uncertainty and forbids automatic retry.
- [ ] Keep this handoff, the runbook and the single matching item in `docs/LOCAL-AGENT-INBOX.md` synchronized whenever source materially changes the local scenario.

## 10. LOCAL_ONLY end-to-end matrix

No remote/static agent may convert these cells to `LOCAL_PASS`. Pin the **exact candidate SHA** before running. Use disposable drawings only.

Required runtime proof includes:

1. Load exact V25 plugin in licensed BricsCAD V25 and exact V26 plugin in licensed BricsCAD V26; record sanitized host/plugin identity.
2. Run `scripts/test-mcp-loopback-readonly.py` against the loaded exact candidate and prove `/healthz`, bearer rejection, initialize/session/version handling, initialized notification, ping, required `tools/list`, bounded read-only tool calls and session DELETE without printing the token or mutating the drawing.
3. Complete GUI cloudflared installation/update if required, Cloudflare browser login, named-tunnel create/reuse, DNS route, public `/mcp`, restart BricsCAD and automatic reconnect — without terminal use in the end-user flow.
4. Connect ChatGPT Web custom MCP to the public endpoint and prove tool discovery.
5. Read active document, selection, database/entity snapshot, view state and privacy-safe allowlisted sysvars.
6. Create direct line/circle/arc/polyline/DBText/MText entities, layer them, transform/delete/re-layer them, and verify native DB state/handles and audit entries.
7. Exercise representative bounded command workflows: hatch, dimension, block/insert, xref where disposable resources are available, layout/viewport, save/save-as, plot/export and undo/redo.
8. Exercise BricsCAD-process-only mouse click, printable Unicode typing and named keys; prove outside-window/non-BricsCAD targeting is rejected.
9. Exercise the application-context timeout boundary and prove queued work can be cancelled before start while already-running work returns explicit completion-uncertain/no-auto-retry truth; inspect CAD/database/audit state before any follow-up action.
10. Trigger emergency stop while autonomous work is active; prove later mutations/UI calls refuse until explicit resume; prove cancel/ESC recovery.
11. Save, close/reopen the disposable DWG and verify the intended final drawing survives; verify tunnel/plugin shutdown leaves no task-owned process residue.

Evidence must never contain bearer tokens, Cloudflare credentials, raw private paths, customer/private DWGs, proprietary BricsCAD binaries or unsanitized screenshots.

## 11. Future-agent operating procedure

1. Read #4352 and this handoff first. Confirm `Lane-Key: issue-4352`, PR #4425 and the canonical branch before touching source.
2. Refresh the canonical branch head immediately before **every write**. Multiple agents may be working concurrently; never overwrite a newer blob/SHA and never reset another agent's work.
3. Read the runtime contract, main-write authorization and CI policy. Do not manufacture `LOCAL_PASS` from source review or hosted CI.
4. Preserve the one-repository runtime rule and click-first UX. Do not introduce Node, a second clone, PowerShell/CMD setup instructions, or password storage into the user path.
5. Prefer API-first CAD implementation. Keep command sequencing allowlisted/bounded; keep UI automation BricsCAD-process-only.
6. For the current owner assignment, the owner is handling CI checks; source-focused agents should not spend the lane repeatedly polling Actions unless explicitly asked. Keep source contracts/preflights correct as code changes.
7. When source changes alter runtime acceptance, update this handoff and the single matching item in `docs/LOCAL-AGENT-INBOX.md` in the same batch.
8. Before PR/merge, reconcile the newest `main`, respect protected-main authorization, and require policy-compliant fresh checks. Do not force a stale/failed candidate into `main`.

## 12. Definition of done

**SOURCE_READY** means the one-repo embedded MCP, click-first production onboarding, direct/bounded/UI full-CAD tool surface, safety controls, lifecycle, source guards and documentation are coherent on one exact candidate. It does **not** mean real ChatGPT/Cloudflare/BricsCAD runtime passed.

**LOCAL_PASS** for #4352 requires the exact candidate SHA to pass the complete licensed V25/V26 + Windows UI + Cloudflare Named Tunnel + ChatGPT Web custom-MCP matrix above with sanitized evidence registered in `docs/LOCAL-AGENT-INBOX.md`.

**MERGED_MAIN** additionally requires repository merge authorization/policy and fresh protected checks on the intended PR/head. Release/publication remains a separate owner decision.

## 13. Session history / consolidation

The work began under #4314 as a ChatGPT↔BricsCAD MCP bridge. During the same 2026-08-28 session the owner clarified that users must install only QS3D-BricsCAD, that MCP belongs inside the plugin, that configuration must be simple/click-first, and that the final agent must be able to drive a full drawing including a bounded mouse/keyboard fallback. #4352 is therefore the canonical production carrier for the consolidated scope. Do not reopen a second runtime carrier for #4314 or `QS3D-CAD-MCP`; retain those only as historical/reference context.
