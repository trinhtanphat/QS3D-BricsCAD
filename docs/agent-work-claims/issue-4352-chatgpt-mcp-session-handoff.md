# #4352 — ChatGPT MCP / full-CAD-agent detailed session handoff

**Status:** ACTIVE / SOURCE HARDENING / RUNTIME `PENDING_LOCAL`  
**Updated:** 2026-08-29 (UTC+7)  
**Lane-Key:** `issue-4352`  
**Canonical issue:** #4352  
**Canonical PR:** #4425  
**Canonical branch:** `agent/interactive-20260828-mcpui/issue-4352-gui-cloudflare-onboarding`

> **READ `docs/MCP-CANONICAL-RUNBOOK.md` FIRST.** That file is the durable MCP navigation/source-of-truth. This handoff is only the detailed current-session state. Do not use this file to rediscover the architecture from scratch.

## Current state in one paragraph

The production MCP is now a **single-repository, embedded, modular QS3D plugin service**. The active transport is `McpEmbeddedServerV2.cs` and the active BricsCAD/CAD/UI runtime is `McpCadAgentRuntime.cs`. The historical monolithic `McpEmbeddedServer.cs` remains for reference but is excluded from active V25 compilation and must not be treated as runtime source. End-user onboarding is click-first: QS3D Agent Center/Cloudflare UI, provider-browser login, persistent Named Tunnel, no PowerShell/CMD/Node/second MCP clone. Source-side full-CAD agent capabilities and hardening are implemented; real licensed V25/V26 + Cloudflare + ChatGPT end-to-end qualification remains `LOCAL_ONLY / PENDING_LOCAL`.

## Active architecture

```text
ChatGPT Web/custom MCP
  -> HTTPS public /mcp + QS3D bearer auth
  -> Cloudflare Named Tunnel
  -> 127.0.0.1:8765/mcp
  -> McpEmbeddedServerV2.cs (compiled class McpEmbeddedServer)
  -> McpCadAgentRuntime.cs
       -> direct BricsCAD/Teigha DB API
       -> bounded allowlisted SendStringToExecute workflow
       -> BricsCAD-process-only Win32 mouse/keyboard fallback
```

Health endpoint: `http://127.0.0.1:8765/healthz`  
Local MCP endpoint: `http://127.0.0.1:8765/mcp`  
Production public endpoint: `https://<configured-hostname>/mcp`

## Do not edit the wrong MCP implementation

**ACTIVE:**

- `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` — active loopback HTTP transport; owns HTTP/MCP/auth/session/tool schemas/results.
- `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs` — CAD/editor/runtime/UI/recovery/audit.
- `src/QS3D.BricsCAD.V25/McpTopLevelJson.cs` — top-level security-sensitive JSON parsing.
- `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs` — main click-first user control center.
- `src/QS3D.BricsCAD.V25/McpCloudflaredBootstrapper.cs` — GUI cloudflared install/update.
- `src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs` — browser-login Named Tunnel.
- `src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs` — Quick/token fallback.
- `src/QS3D.BricsCAD.V25/McpPublicEndpointResolver.cs` — canonical public URL.
- `src/QS3D.BricsCAD.V25/McpConnectorRibbonCommands.cs` — probe/dashboard/Ribbon commands.
- `src/QS3D.BricsCAD.V25/PluginEntry.cs` — startup/autostart/shutdown ownership.
- Ribbon override/coordinator + V25/V26 csproj source composition.

**LEGACY:**

- `src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs` — legacy historical monolith only; explicitly removed from active V25 compile. Do not land normal MCP fixes there.

## Product decisions fixed by the owner/session

- One shipping repo/install: only `QS3D-BricsCAD`.
- `QS3D-CAD-MCP` is historical/reference only, never a second end-user runtime requirement.
- No PowerShell, CMD, Git, or Node in normal user setup.
- QS3D must never ask for, inspect, log, or store the Cloudflare password; Cloudflare username/password are entered only on Cloudflare's browser page.
- Persistent Named Tunnel is production path; Quick Tunnel is test/fallback only.
- ChatGPT drawing strategy is **direct CAD API tool first -> bounded native command second -> UI fallback last**.
- UI automation may target only an HWND owned by the current BricsCAD process; no desktop-wide remote control.
- No arbitrary shell/process execution through MCP.
- No production remote screenshot/desktop-capture tool in current scope; database/entity/view-state verification is canonical.

## Current tool groups

**Read/inspect:** `connector_info`, `qs3d_status`, `cad_active_document`, `cad_selection`, `cad_database_snapshot`, `cad_entity_inspect`, `cad_view_state`, `cad_wait_idle`, `cad_sysvar`, `cad_command_catalog`, `cad_audit_tail`.

**Direct mutation:** `cad_create_line`, `cad_create_circle`, `cad_create_arc`, `cad_create_polyline`, `cad_create_text`, `cad_create_mtext`, `cad_entity_transform`, `cad_entity_delete`, `cad_entity_set_layer`, `cad_layer`.

**Native workflow:** `cad_command_sequence`, `qs3d_run_command`, `cad_cancel_command`.

**UI fallback:** `cad_ui_click`, `cad_ui_type`, `cad_ui_key`.

**Safety:** `cad_agent_stop`, `cad_agent_resume`, `cad_cancel_command`.

The bounded command catalog covers representative drawing/editing, hatch, dimensions, blocks/inserts, xrefs, layouts/viewports, plot, open/save/save-as, undo/redo, cleanup and selected advanced/3D native workflows. The active source is authoritative for the exact allowlist.

## Security/hardening truth already implemented in source

- loopback-only listener;
- bearer authentication;
- bounded header/body/session/concurrency state;
- exact `application/json` media-type admission with explicit rejection of JSON media-type lookalikes;
- fail-closed duplicate security-sensitive headers;
- unsupported transfer encoding rejection;
- Origin/DNS-rebinding checks;
- MCP initialize/session/protocol-version handling and session DELETE truth;
- top-level-only mutation confirmation, including duplicate/nested fail-closed behavior;
- ordinary mutation requires top-level `confirmMutation=true`;
- each confirmed mutation is bound to an automation epoch; stop/restart/resume invalidates older work and queued CAD mutation re-checks its captured epoch before application-context execution;
- long UI typing/click/key paths re-check the current mutation epoch close to injection so Emergency Stop prevents further stale input;
- bounded command inputs and known chaining/control-character rejection;
- repeated leading AutoCAD global/English prefixes (`.` and `_`) are canonicalized before command checks, closing mixed-prefix injection such as `._LINE` / `_._LINE`;
- `QS3D*` command-name boundary;
- BricsCAD-process-only mouse/keyboard with foreground revalidation;
- Alt+F4 blocked;
- emergency stop/resume/cancel with BricsCAD-only ESC recovery;
- application-context timeout handling uses atomic `Queued → Running` / `Queued → CancelledBeforeStart` transitions and distinguishes cancelled-before-start from already-running/completion-uncertain work so agents must not blindly retry;
- rotating/sanitized local mutation audit;
- no remote PowerShell/cmd/arbitrary shell/process launch.

## Cloudflare/end-user state

Normal user path is owned by the QS3D UI:

```text
TOOL > MCP (AI) / Agent Center
-> install/update cloudflared via GUI if needed
-> click Cloudflare login
-> authenticate in provider browser
-> enter hostname
-> create/reuse tunnel qs3d-bricscad + DNS route
-> start tunnel + persist autostart
-> copy MCP URL/bearer config
-> open ChatGPT and scan/connect tools
```

Advanced token mode uses DPAPI protection. Quick Tunnel remains test-only. Saved Named Tunnel startup is attempted by plugin lifecycle on later BricsCAD starts.

## Source status checklist

- [x] One-repo modular embedded MCP; no Node/second runtime.
- [x] Hardened loopback/auth/session/protocol transport.
- [x] Active runtime split from transport.
- [x] Direct read/inspection surface.
- [x] Direct line/circle/arc/polyline/DBText/MText + transform/delete/re-layer/layer mutation surface.
- [x] Bounded native command bridge covering full-drawing workflow classes.
- [x] Mixed-prefix command canonicalization prevents alternate-spelling known-command injection.
- [x] BricsCAD-process-only mouse/keyboard fallback.
- [x] Epoch-invalidated Emergency Stop/restart/resume prevents stale queued mutation and stale long UI input from continuing after Stop.
- [x] Emergency stop/resume/cancel.
- [x] Timeout/late-CAD-callback no-blind-retry semantics.
- [x] Audit hardening.
- [x] Click-first Agent Center and Cloudflare browser-login Named Tunnel.
- [x] GUI cloudflared bootstrap.
- [x] Quick/token fallback.
- [x] Canonical public endpoint resolver.
- [x] Sanitized read-only loopback protocol probe.
- [x] MCP source guards/runbooks, including production hardening guards for stop epochs and command canonicalization.
- [x] `docs/MCP-CANONICAL-RUNBOOK.md` now gives every future agent one start point and explicitly distinguishes ACTIVE vs LEGACY source.
- [ ] Exact final runtime candidate still needs licensed local end-to-end proof before `LOCAL_PASS`.

## LOCAL_ONLY matrix

The single live local item is `LOCAL-024 — #4352 ChatGPT MCP full-agent qualification` in `docs/LOCAL-AGENT-INBOX.md`.

Use `docs/MCP-FULL-CAD-AGENT.md` for the detailed matrix. At minimum prove on one exact intended SHA:

1. V25 and V26 plugin load/identity in licensed BricsCAD.
2. `scripts/test-mcp-loopback-readonly.py` protocol/auth/session/read-only negatives and positives without token output or drawing mutation.
3. GUI cloudflared install/update, browser login, Named Tunnel/DNS/public `/mcp`, restart/autostart.
4. ChatGPT Web discovers tools through the public endpoint.
5. Direct read/inspect tools.
6. Direct line/circle/arc/polyline/DBText/MText + layer/transform/delete/re-layer tools.
7. Representative hatch/dimension/block/insert/xref/layout/viewport/save/save-as/plot/undo-redo native command workflows.
8. Mixed-prefix injection negative: later prompt lines such as `._LINE` / `_._LINE` must be rejected before native command dispatch.
9. BricsCAD-process-only click/type/key rejection and success boundaries.
10. Stop-epoch regression: queue a confirmed mutation while CAD application context is intentionally busy, issue Emergency Stop before it starts, then Resume; the stale queued mutation must never execute after resume.
11. During a long disposable UI typing/click sequence, issue Stop and prove further input injection ceases while recovery controls remain actionable.
12. Timeout uncertainty/no-auto-retry behavior.
13. Emergency stop/resume/cancel for newly submitted work.
14. Save/reopen final disposable drawing and clean shutdown/no task-owned residue.

Never commit bearer tokens, Cloudflare credentials, private paths, customer/private DWGs, proprietary binaries or unsanitized screenshots.

## Future-agent operating procedure

1. Do repository governance bootstrap.
2. Read `docs/MCP-CANONICAL-RUNBOOK.md` first.
3. Open Issue #4352 and PR #4425; resolve current canonical branch head freshly.
4. Read this handoff only for detailed current-session context.
5. Inspect ACTIVE V2 source, not `McpEmbeddedServer.cs`.
6. Refresh before every write because agents may share the carrier.
7. Fix/harden current implementation in place; do not create a duplicate MCP lane merely because CI is red or main moved.
8. Preserve one-repo/click-first/API-first/BricsCAD-only boundaries.
9. Update relevant MCP preflight/docs when behavior changes.
10. Update `LOCAL-024` when the real-runtime scenario/status changes.
11. Source-side CI may prove compilation/contracts only; it never replaces the exact-SHA licensed realtime matrix.
12. Merge/release follows normal protected-main policy; no direct-main shortcut.

## Durable handoff for the next session

```text
Lane-Key: issue-4352
Canonical Issue: #4352
Canonical PR: #4425
Canonical branch: agent/interactive-20260828-mcpui/issue-4352-gui-cloudflare-onboarding
Current head: resolve fresh
START HERE: docs/MCP-CANONICAL-RUNBOOK.md
Detailed handoff: docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md
Active transport: src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs
Active runtime: src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs
Legacy/non-runtime: src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs
Source status: source implementation present; fix current defects in place
Runtime status: LOCAL_ONLY / PENDING_LOCAL until exact-SHA proof
```

## Definition of done

`SOURCE_READY` means source architecture/tool/onboarding/security/docs are coherent on an exact candidate. It is not runtime proof.

`LOCAL_PASS` requires the exact candidate to pass the complete licensed V25/V26 + Windows UI + real Cloudflare Named Tunnel + ChatGPT Web matrix with sanitized evidence in `LOCAL-024`.

`MERGED_MAIN` additionally requires protected PR/main policy. Release/publication remains a separate owner decision.

## Session consolidation history

The work began under #4314 as the first embedded ChatGPT↔BricsCAD MCP bridge. During the same owner session, scope was expanded to one QS3D install, click-first Cloudflare onboarding, full CAD creation/edit/annotation/layout/save/plot capability, and bounded BricsCAD-only mouse/keyboard fallback. #4314 is therefore historical/superseded; #4352 + PR #4425 are the single canonical production carrier. Do not recreate a second runtime carrier from #4314 or `QS3D-CAD-MCP`.
