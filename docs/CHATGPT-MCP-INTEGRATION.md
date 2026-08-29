# ChatGPT Web ↔ QS3D embedded MCP

Status: SOURCE_READY / PENDING_LOCAL
Canonical issue: #4352
Lane-Key: `issue-4352`

`QS3D-BricsCAD` is the only shipping repository/package required for this integration. The MCP server, Cloudflare onboarding, Agent Center, CAD tools, safety controls, and local audit surface are embedded in the BricsCAD plugin. A second MCP repository, Node runtime, PowerShell, CMD, or manual terminal setup is not part of the normal end-user path.

The detailed full-CAD contract and runtime qualification matrix are maintained in `docs/MCP-FULL-CAD-AGENT.md`.

## Runtime architecture

```text
ChatGPT / custom MCP client
        |
        | HTTPS + Bearer
        v
Cloudflare Named Tunnel
        |
        | hostname-scoped ingress
        v
http://127.0.0.1:8765/mcp
        |
        v
QS3D.BricsCAD.V25.dll / QS3D.BricsCAD.V26.dll
  embedded MCP server
        |
        | ExecuteInApplicationContext
        v
BricsCAD document/editor/database + bounded UI fallback
```

The embedded service never binds a public/LAN socket. It listens only on `127.0.0.1:8765`; public access is delegated to the configured HTTPS tunnel.

## Default click-first setup

Open **TOOL > MCP (AI) > Cài đặt MCP** or **AI Dashboard**. Both Ribbon actions route to `QS3DMCPAGENTCENTER`.

From Agent Center the operator can:

1. install or update the official Windows `cloudflared` binary automatically;
2. complete Cloudflare login in the provider-owned browser page;
3. create/reuse the `qs3d-bricscad` Named Tunnel and DNS route;
4. start a Quick Tunnel only as a temporary test fallback;
5. copy the validated MCP URL, Bearer token, or ready URL + Authorization block;
6. open ChatGPT;
7. run the MCP protocol check;
8. run the read-only Agent self-test;
9. emergency-stop the Agent, send ESC twice to BricsCAD, or resume explicitly;
10. open the local MCP audit folder.

Cloudflare username/password are entered only on Cloudflare's browser page. QS3D does not request or persist those credentials.

## Managed cloudflared bootstrap

The click-first installer downloads the official Cloudflare Windows amd64 executable to the current user's QS3D local-data folder. Before adopting the binary, QS3D applies conservative file-size bounds, Windows `WinVerifyTrust` Authenticode validation, and signer inspection requiring Cloudflare identity.

A failed replacement does not intentionally destroy the previous working managed binary. Windows is allowed to build the signer certificate chain normally; cache-only certificate validation is not used because it can false-negative on a clean machine missing an intermediate certificate cache entry.

The network MCP server cannot invoke the installer or launch arbitrary processes.

## Public endpoint contract

`McpPublicEndpointResolver` is the single source of truth for copy/display/status paths. Resolution precedence is:

1. account-managed Named Tunnel;
2. token/Quick fallback tunnel;
3. optional `QS3D_MCP_PUBLIC_URL` environment fallback.

A copyable public endpoint must be an absolute HTTPS URL with no user-info, query, or fragment and with canonical path `/mcp`. Localhost/loopback and private, link-local, documentation, multicast, or otherwise non-public literal IP addresses are rejected. Hostname-based provider URLs remain supported.

Provider-resolved URLs are synchronized into the current process so `connector_info`, Agent Center, Ribbon helpers, and generated guidance report the same endpoint.

## Cloudflare tunnel ownership

Named-browser, token, and Quick Tunnel modes hand ownership to one another rather than intentionally running multiple forwarders for the same loopback MCP endpoint.

The account-managed Named Tunnel uses hostname-scoped ingress to `http://127.0.0.1:8765` and a final `http_status:404` rule. Reuse requires live provider tunnel identity plus matching local credentials; a stale local UUID alone is not trusted. DNS-route conflicts fail closed rather than treating a generic “already exists” response as success.

Long-lived `cloudflared` processes establish QS3D ownership before exit events are enabled. Quick Tunnel URLs are ephemeral; if the fallback process exits, its cached `trycloudflare.com` URL is cleared so Agent Center does not keep presenting a dead temporary endpoint.

## MCP transport and authentication

Local endpoints:

```text
MCP:    http://127.0.0.1:8765/mcp
Health: http://127.0.0.1:8765/healthz
```

`POST /mcp` requires `Authorization: Bearer <token>`. Token precedence is an explicit sufficiently long `QS3D_MCP_BEARER_TOKEN`, then the generated per-user token file, then an ephemeral in-process token only if the token file cannot be used.

The server supports MCP protocol `2025-06-18` with compatibility for `2025-03-26`. A client initializes, receives `Mcp-Session-Id`, sends `notifications/initialized`, then uses `ping`, `tools/list`, and `tools/call`. Session and concurrent-client counts are bounded, sessions expire, and `DELETE /mcp` closes the session.

HTTP request/header/body sizes are bounded. Security-sensitive duplicate headers and `Transfer-Encoding` are rejected, and Bearer comparison is constant-time.

## Agent Center local checks

The protocol check and read-only Agent self-test execute their local HTTP work on a worker thread rather than blocking the WPF button thread. CAD-observation tools can therefore marshal back through `Application.DocumentManager.ExecuteInApplicationContext` without the Agent Center synchronously occupying the host UI thread.

Observation self-tests are serialized so users cannot accidentally stack multiple local probes. Emergency Stop and ESC cancel remain deliberately outside that observation slot so recovery controls stay actionable while a read-only check is in progress.

The read-only self-test discovers the expected full tool surface and calls observation tools only; it must not mutate the drawing.

## Full CAD tool surface

Read/inspection:

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

Direct native CAD mutation:

- `cad_create_line`
- `cad_create_circle`
- `cad_create_polyline`
- `cad_create_text`
- `cad_entity_transform`
- `cad_entity_delete`
- `cad_layer`
- `qs3d_run_command`

Bounded native workflow:

- `cad_command_sequence`

BricsCAD-window-only UI fallback:

- `cad_ui_click`
- `cad_ui_type`
- `cad_ui_key`

Recovery/control:

- `cad_agent_stop`
- `cad_agent_resume`
- `cad_cancel_command`

Ordinary mutation/UI tools require `confirmMutation=true` and are refused while the Agent emergency-stop latch is active. Stop and cancel intentionally remain available without mutation confirmation.

## Security boundary

Direct database work uses document locks and Teigha/BricsCAD transactions. Geometry values must be finite; entity handles, layer names, text, snapshots, command inputs, and other externally supplied values are bounded.

`cad_command_sequence` accepts one explicit BricsCAD command from an allowlist plus bounded prompt inputs. It rejects control-character injection, command chaining after a blank terminator, and known CAD/QS3D command names injected as later prompt lines.

Mouse/keyboard fallback uses Windows `SendInput` only after verifying the target belongs to the current BricsCAD process. Coordinates are client-relative and checked before conversion to screen coordinates. Input stops if the foreground window changes; the MCP tools are not desktop-wide remote control.

The network MCP server exposes no arbitrary shell, process execution, tunnel setup, browser launch, or cloudflared installation tool.

## Local audit and recovery

Mutations are recorded to the bounded rotating local MCP audit JSONL file. Audit details are sanitized and typed UI text itself is not persisted as an audit detail.

Emergency Stop latches autonomous mutation/UI tools off and attempts ESC twice through CAD context, with a foreground-process-verified keyboard fallback when the application-context route is unavailable. Resume requires explicit mutation confirmation.

## Qualification boundary

Source implementation remains `PENDING_LOCAL`. Do not promote hosted source checks or compilation evidence into runtime `LOCAL_PASS`.

The exact candidate SHA must still be qualified on real Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT. The local matrix in `docs/MCP-FULL-CAD-AGENT.md` covers browser login, Named Tunnel, Quick Tunnel lifecycle, tool discovery, disposable-DWG creation/editing, bounded command workflows, BricsCAD-only mouse/keyboard fallback, emergency recovery, save/reopen, and plot.

Additional edge cases that must be exercised locally include:

- keep Agent Center responsive while the read-only self-test is active and prove Emergency Stop/ESC remains actionable;
- terminate a Quick Tunnel after its URL appears and prove the stale temporary public URL disappears;
- verify private/link-local literal values supplied through `QS3D_MCP_PUBLIC_URL` are rejected;
- verify a failed managed-cloudflared replacement preserves the previous working binary;
- verify starting one tunnel mode does not leave another QS3D-owned forwarder running.

Until that exact-SHA runtime matrix passes, report the feature as `SOURCE_READY / PENDING_LOCAL`, never `LOCAL_PASS`.
