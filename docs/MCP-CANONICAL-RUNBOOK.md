# QS3D MCP — CANONICAL START HERE

**Status:** CANONICAL MCP NAVIGATION / SOURCE-OF-TRUTH  
**Lane-Key:** `issue-4352`  
**Canonical Issue:** `#4352` — Production ChatGPT MCP onboarding + full CAD agent for QS3D  
**Canonical PR:** `#4425`  
**Canonical branch:** `agent/interactive-20260828-mcpui/issue-4352-gui-cloudflare-onboarding`  
**Runtime status:** source implementation substantially complete; licensed end-to-end runtime remains `PENDING_LOCAL` until exact-SHA proof  
**End-user model:** one QS3D install, click/browser-login setup, no PowerShell/CMD/Node/second MCP repository

> **MCP AGENTS MUST START HERE.** If a task mentions MCP, ChatGPT Web/custom MCP, Cloudflare Tunnel, AI drawing automation, `QS3DMCP*`, TOOL > MCP (AI), or BricsCAD mouse/keyboard automation, read this file immediately after the repository governance bootstrap. Do not reconstruct the architecture from old chats or historical MCP files.

## 1. The architecture in one line

```text
ChatGPT Web
  -> HTTPS public /mcp
  -> Cloudflare Named Tunnel
  -> http://127.0.0.1:8765/mcp
  -> McpEmbeddedServerV2 transport inside QS3D plugin
  -> McpCadAgentRuntime
  -> direct BricsCAD API / bounded native command / BricsCAD-only UI fallback
```

There is **one shipping repository and one end-user installation**: `trinhtanphat/QS3D-BricsCAD`.

The separate historical `QS3D-CAD-MCP` repository is reference/history only. Do **not** reintroduce it as a second clone, Node server, runtime dependency, or user setup requirement unless the owner explicitly changes the product architecture.

## 2. ACTIVE source vs LEGACY source — do not edit the wrong file

### ACTIVE runtime

These are the files future agents should inspect first:

1. `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs`
   - active loopback HTTP/MCP transport;
   - bearer authentication;
   - JSON/media-type/header admission;
   - Origin/DNS-rebinding defense;
   - MCP initialize/session/protocol lifecycle;
   - tool schemas/results;
   - delegates CAD/UI execution to `McpCadAgentRuntime`.

2. `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs`
   - active BricsCAD runtime;
   - direct transactional CAD tools;
   - `ExecuteInApplicationContext` dispatch;
   - bounded/allowlisted native command execution;
   - BricsCAD-process-only mouse/keyboard fallback;
   - timeout/late-callback semantics;
   - emergency stop/resume/cancel;
   - local mutation audit.

3. `src/QS3D.BricsCAD.V25/McpTopLevelJson.cs`
   - security-sensitive top-level JSON parsing;
   - duplicate/nested mutation-confirmation protection.

4. `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`
   - main click-first MCP user control center/self-test UI.

5. `src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs`
   - default provider-browser Cloudflare login;
   - Named Tunnel create/reuse/DNS/config/autostart.

6. `src/QS3D.BricsCAD.V25/McpCloudflaredBootstrapper.cs`
   - GUI install/update path for `cloudflared`;
   - keeps terminal installation out of the normal end-user flow.

7. `src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs`
   - advanced token mode;
   - Quick Tunnel fallback/testing;
   - not the preferred production path.

8. `src/QS3D.BricsCAD.V25/McpPublicEndpointResolver.cs`
   - single canonical public HTTPS `/mcp` resolver;
   - provider-managed state wins over fallbacks;
   - rejects loopback/private/invalid public candidates.

9. `src/QS3D.BricsCAD.V25/McpConnectorRibbonCommands.cs`
   - Ribbon/user commands;
   - local MCP protocol probe/dashboard/docs.

10. `src/QS3D.BricsCAD.V25/PluginEntry.cs`
    - starts embedded MCP fail-soft;
    - attempts saved Named Tunnel autostart;
    - resolves canonical public endpoint;
    - owns shutdown cleanup.

11. `src/QS3D.BricsCAD.V25/Ribbon/McpRibbonCommandOverride.cs`
    and `RibbonInitializationCoordinator.cs`
    - TOOL > MCP (AI) routing and initialization order.

12. `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`
    and `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj`
    - active source composition for V25/V26.

### LEGACY / historical source

`src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs` is the old monolithic implementation. It remains in repository history for review/reference but is **explicitly excluded from the active V25 compile**. Do not “fix MCP” by editing this legacy monolith unless the task is explicitly about deleting/migrating historical code.

The active class name is still `McpEmbeddedServer`, but its compiled implementation comes from `McpEmbeddedServerV2.cs`.

## 3. Mandatory reading order for any MCP agent

After `AGENTS.md`, runtime contract, main-write policy, product boundary and CI policy:

1. **this file** — `docs/MCP-CANONICAL-RUNBOOK.md`;
2. Issue `#4352` and PR `#4425` for current carrier/head/state;
3. `docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md` for detailed session decisions/current hardening notes;
4. active source files in section 2 above;
5. `docs/MCP-FULL-CAD-AGENT.md` for end-user/full-drawing/runtime qualification details;
6. `docs/LOCAL-AGENT-INBOX.md` for the live `LOCAL_ONLY` queue/evidence state;
7. MCP preflights/tests listed in section 11.

**Current source wins over stale handoffs.** This file tells you where to look; the active source defines exact implementation behavior.

## 4. What the owner actually wants

The consolidated product target from this session is:

- ChatGPT Web/custom MCP connects to the currently loaded QS3D/BricsCAD session.
- MCP is embedded in the QS3D plugin.
- User installs QS3D once; no second MCP repo/runtime.
- Normal setup is click-first; no PowerShell/CMD/Git/Node required for the end user.
- Cloudflare username/password are entered only on Cloudflare's provider browser page; QS3D never asks for or stores the password.
- Production uses persistent Cloudflare Named Tunnel; Quick Tunnel is only a testing/fallback path.
- QS3D can install/update/detect `cloudflared` through GUI onboarding and automatically reuse the saved tunnel on later BricsCAD starts.
- ChatGPT can inspect a drawing and complete a full CAD workflow using **API first, native command second, mouse/keyboard fallback last**.
- UI input must remain confined to a window owned by the BricsCAD process.
- Mutations are explicitly confirmation-gated, audited, cancellable and emergency-stoppable.
- MCP never exposes arbitrary PowerShell, `cmd.exe`, shell/process launch, arbitrary desktop control or unrestricted CAD command execution.

## 5. End-user flow — no terminal required

The intended production user path is:

```text
Open BricsCAD + QS3D
-> TOOL > MCP (AI) > Cài đặt MCP / Agent Center
-> install/update Cloudflare Tunnel from QS3D UI if needed
-> click Đăng nhập Cloudflare
-> authenticate on Cloudflare browser page
-> enter public hostname (for example qs3d.example.com)
-> click create/reuse/connect Named Tunnel
-> copy MCP URL + bearer configuration
-> click open ChatGPT
-> add/scan QS3D MCP app
-> use ChatGPT against the live BricsCAD session
```

Future BricsCAD starts should reuse the saved Named Tunnel configuration and attempt automatic reconnect.

Do not write normal-user documentation that sends the user to PowerShell/CMD when the QS3D UI owns that operation.

## 6. Cloudflare contract

### Production/default

`McpCloudflareAccountOnboarding` + bootstrapper own the normal flow:

- discover/install/update `cloudflared` through GUI;
- `cloudflared tunnel login` opens provider browser auth;
- Cloudflare credentials remain with Cloudflare;
- create or safely reuse tunnel `qs3d-bricscad`;
- create DNS route;
- write local tunnel config pointing to loopback MCP origin;
- start tunnel and persist autostart state;
- refuse ambiguous/stale tunnel ownership instead of guessing.

### Advanced/fallback

`McpCloudflareOnboarding` owns:

- dashboard-issued token mode with Windows DPAPI protection;
- Quick Tunnel one-click testing.

Named Tunnel and Quick/token process ownership must not fight each other. Background/stale callbacks must not overwrite current tunnel state.

## 7. MCP transport/security contract

The active modular transport currently implements these core invariants:

- listener is loopback-only at `127.0.0.1:8765`;
- `/mcp` requires bearer authentication;
- `/healthz` is local health/status;
- supported MCP protocol includes `2025-06-18` with compatibility path for `2025-03-26` as defined in source;
- bounded headers, body, sessions and concurrent clients;
- exact JSON media-type admission; lookalikes are rejected;
- security-sensitive duplicate headers fail closed;
- unsupported transfer encoding fails closed;
- Streamable-HTTP Origin/DNS-rebinding defense is fail-closed;
- initialize creates bounded session state and returns `Mcp-Session-Id`;
- post-initialize protocol version must match negotiated session state;
- stale/expired/terminated session behavior is explicit;
- `tools/list`, `tools/call`, ping, notifications and session DELETE are implemented;
- top-level mutation confirmation is parsed fail-closed;
- no arbitrary OS execution surface exists in the transport.

Bearer token and Cloudflare account credentials are separate things. Never commit/log/paste the bearer secret into sanitized evidence.

## 8. Current full-CAD tool model

The exact tool list is defined by the active `McpEmbeddedServerV2` schema + `McpCadAgentRuntime.Call`; source is authoritative.

### Read/inspect

- `connector_info`
- `qs3d_status`
- `cad_active_document`
- `cad_selection`
- `cad_database_snapshot`
- `cad_entity_inspect`
- `cad_view_state`
- `cad_wait_idle`
- `cad_sysvar`
- `cad_command_catalog`
- `cad_audit_tail`

### Direct transactional CAD mutation

- `cad_create_line`
- `cad_create_circle`
- `cad_create_arc`
- `cad_create_polyline`
- `cad_create_text`
- `cad_create_mtext`
- `cad_entity_transform`
- `cad_entity_delete`
- `cad_entity_set_layer`
- `cad_layer`

Direct tools are preferred for deterministic geometry/database changes.

### Native/full-workflow bridge

- `cad_command_sequence`
- `qs3d_run_command`
- `cad_cancel_command`

The bounded native command catalog covers classes including hatch, dimensions, blocks/inserts, xrefs, layouts/viewports, plot, open/save/save-as, undo/redo, cleanup and selected advanced/3D workflows. Command inputs remain bounded and command chaining/control-character injection is rejected.

### UI fallback

- `cad_ui_click`
- `cad_ui_type`
- `cad_ui_key`

These target only a window owned by the current BricsCAD process. They are not a general remote-desktop API.

### Safety/recovery

- `cad_agent_stop`
- `cad_agent_resume`
- `cad_cancel_command`

Emergency stop/cancel remain available to recover an active CAD operation; ordinary mutations require top-level `confirmMutation=true`.

## 9. How ChatGPT should complete a full drawing

Use this order:

```text
inspect document/model/view
-> plan geometry/layers/styles
-> use direct API tools for deterministic geometry
-> use bounded native commands for hatch/dimension/block/xref/layout/viewport/plot/save/etc.
-> use mouse/keyboard only for a genuinely UI-only step
-> wait for CAD idle where required
-> re-inspect database/entity/view state
-> correct with direct edits / bounded command / undo
-> save
-> plot/export
-> reopen/verify when requested
```

The goal is **not blind clicking**. It is API-first autonomous CAD with UI fallback only where needed.

The current production scope deliberately does **not** expose a remote desktop/screenshot capture tool. Database/entity/view-state inspection is the canonical verification path. Any future image-capture feature must be a separately reviewed BricsCAD-window-only scope.

## 10. Safety invariants future agents must preserve

Do not weaken these to make a test easier:

- ordinary mutation requires top-level `confirmMutation=true`;
- nested/duplicate confirmation must not authorize mutation;
- UI input targets only HWNDs owned by the current BricsCAD process;
- UI sequences abort if foreground ownership changes;
- outside-window mouse coordinates are refused;
- dangerous close behavior such as Alt+F4 remains blocked;
- CAD native commands are allowlisted and bounded;
- no unrestricted command chaining;
- `qs3d_run_command` remains constrained to valid `QS3D*` command names;
- emergency stop changes automation state immediately and has bounded BricsCAD-only ESC recovery;
- CAD application-context timeout semantics distinguish cancelled-before-start from already-running/completion-uncertain work so an agent does not blindly retry a mutation;
- mutations are locally audited without storing typed text content/secrets;
- no PowerShell, `cmd.exe`, arbitrary shell/process launch, password harvesting, desktop-wide input or arbitrary screenshot exfiltration is added to MCP.

## 11. Source guards/tests for MCP changes

When source changes, inspect/update the relevant contract in the same canonical lane:

- `scripts/preflight-embedded-mcp.py`
- `scripts/preflight-mcp-full-agent.py`
- `scripts/preflight-mcp-production-hardening.py`
- `scripts/preflight-mcp-session-handoff.py`
- `scripts/preflight-mcp-loopback-readonly.py`
- `scripts/test-mcp-loopback-readonly.py`
- related Ribbon preflight(s) when TOOL > MCP routing changes

The read-only loopback probe is engineering/local-agent evidence support, not an end-user setup step.

## 12. Code status — what is done and what is not

### Source-side implemented

The current lane contains the intended production architecture and source surface:

- one-repo embedded modular MCP;
- hardened loopback/auth/session transport;
- direct CAD inspection and transactional mutation tools;
- bounded native full-workflow command bridge;
- BricsCAD-process-only UI fallback;
- emergency stop/resume/cancel;
- timeout/late-callback safeguards;
- local audit;
- click-first Agent Center/onboarding;
- GUI `cloudflared` bootstrap;
- provider-browser Cloudflare login;
- Named Tunnel create/reuse/DNS/autostart;
- Quick/token fallback;
- canonical public endpoint resolver;
- Ribbon + plugin lifecycle ownership;
- V25/V26 shared-source composition;
- source guards and a sanitized read-only protocol probe.

Therefore **future agents should fix/harden this implementation in place**, not redesign it from zero or create another MCP runtime carrier.

### Still not equivalent to “100% runtime done”

Real end-to-end proof remains `LOCAL_ONLY / PENDING_LOCAL` until one exact candidate SHA proves:

```text
licensed BricsCAD V25/V26
+ plugin load
+ local MCP protocol/auth/session
+ GUI cloudflared install/login
+ real Named Tunnel/public hostname/autostart
+ ChatGPT Web tool discovery
+ direct CAD read/write tools
+ representative hatch/dimension/block/xref/layout/save/plot flows
+ BricsCAD-only mouse/keyboard fallback
+ timeout/cancel/emergency recovery
+ save/reopen final disposable drawing
```

Source review or GitHub CI must never be relabeled `LOCAL_PASS`.

## 13. Canonical continuation procedure for another agent

When an agent receives “continue MCP”, “fix MCP”, “finish ChatGPT integration”, etc.:

1. do repository governance bootstrap;
2. read this file;
3. open Issue `#4352` and PR `#4425`;
4. resolve the current canonical branch head — never reuse an old SHA from chat;
5. read the detailed session handoff;
6. inspect **ACTIVE V2** source, not legacy `McpEmbeddedServer.cs`;
7. refresh before every write because multiple agents may touch the carrier;
8. fix current source in place; do not create a duplicate MCP branch/issue/PR merely because CI is red or main moved;
9. update relevant preflight/docs if behavior changes;
10. if local runtime acceptance changes, update the single MCP item in `docs/LOCAL-AGENT-INBOX.md`;
11. preserve one-repo + click-first + API-first + BricsCAD-only UI boundaries;
12. reconcile/merge only under normal repository PR/main policy.

## 14. What NOT to do

Do not:

- edit the legacy monolithic `McpEmbeddedServer.cs` thinking it is the active server;
- require `QS3D-CAD-MCP` on the user's PC;
- add Node as an end-user MCP runtime;
- tell normal users to run PowerShell/CMD for setup that QS3D can own;
- add a Cloudflare password field to QS3D;
- store/log Cloudflare username/password;
- bind the MCP server publicly instead of loopback;
- remove bearer auth for convenience;
- replace command allowlists with arbitrary command execution;
- expose arbitrary shell/process execution;
- make mouse/keyboard desktop-wide;
- add desktop-wide screenshots;
- claim `LOCAL_PASS` from source/CI;
- open another equivalent MCP carrier while #4352/#4425 remain canonical.

## 15. Debugging path — stop random repository searching

Use this exact order:

1. **Plugin/lifecycle:** `PluginEntry.cs`.
2. **Local MCP listener/protocol/auth/session:** `McpEmbeddedServerV2.cs` + `McpTopLevelJson.cs`.
3. **CAD tool/runtime/timeout/UI:** `McpCadAgentRuntime.cs`.
4. **User controls/probe:** `McpAgentControlCenter.cs` + `McpConnectorRibbonCommands.cs`.
5. **cloudflared install/update:** `McpCloudflaredBootstrapper.cs`.
6. **browser login/Named Tunnel/DNS/autostart:** `McpCloudflareAccountOnboarding.cs`.
7. **Quick/token fallback:** `McpCloudflareOnboarding.cs`.
8. **public URL mismatch:** `McpPublicEndpointResolver.cs`.
9. **TOOL button wrong command:** Ribbon override/coordinator.
10. **V25/V26 mismatch:** both csproj files and shared-source composition.
11. **runtime proof missing:** `docs/MCP-FULL-CAD-AGENT.md` + `docs/LOCAL-AGENT-INBOX.md`.

## 16. Durable handoff format

Do not paste the whole chat as the only handoff. Future MCP handoff should be:

```text
Lane-Key: issue-4352
Canonical Issue: #4352
Canonical PR: #4425
Canonical branch: agent/interactive-20260828-mcpui/issue-4352-gui-cloudflare-onboarding
Current head: <resolve fresh>
Start here: docs/MCP-CANONICAL-RUNBOOK.md
Detailed state: docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md
Active transport: McpEmbeddedServerV2.cs
Active CAD runtime: McpCadAgentRuntime.cs
Legacy/do-not-edit-as-runtime: McpEmbeddedServer.cs
Source status: <exact remaining defect or SOURCE_READY>
Runtime status: PENDING_LOCAL or exact LOCAL_PASS evidence SHA
Next action: <one concrete action>
```

## 17. Definition of done

The lane is fully complete only when:

- final source remains coherent with this architecture;
- source guards/build/required protected checks are green on the intended candidate;
- PR #4425 (or an explicitly superseding canonical PR) is merged through protected `main`;
- exact resulting `main` contains the implementation;
- exact-SHA licensed V25/V26 local runtime matrix passes;
- browser Cloudflare login + persistent Named Tunnel/autostart passes;
- ChatGPT Web discovers and calls the tools through public `/mcp`;
- a disposable full drawing survives create/edit/annotate/layout/save/plot/reopen;
- BricsCAD-only mouse/keyboard + timeout/cancel/emergency recovery are proven;
- secrets/private/customer data are absent from committed evidence;
- `docs/LOCAL-AGENT-INBOX.md` records the exact runtime result.

Until those are all true, report the exact incomplete layer instead of saying generic “100% done”.
