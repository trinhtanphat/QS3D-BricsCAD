# ChatGPT Web ↔ QS3D embedded MCP

Status: source implementation for issue #4314. Licensed BricsCAD/tunnel qualification is `LOCAL_ONLY`.

## Decision

`QS3D-BricsCAD` is the only repository/package required for the shipping integration. The useful MCP
protocol and CAD-inspection patterns are embedded directly in the BricsCAD plugin. The separate
`QS3D-CAD-MCP` repository remains development/reference history and is **not** an install or runtime
dependency.

The product remains a BricsCAD-hosted plugin. The MCP listener is an optional plugin service; it does
not turn QS3D into a standalone CAD application.

## Runtime architecture

```text
ChatGPT Web / custom MCP client
        |
        | HTTPS
        v
Cloudflare Tunnel (or equivalent HTTPS ingress)
        |
        | loopback forward
        v
http://127.0.0.1:8765/mcp
        |
        v
QS3D.BricsCAD.V25.dll / QS3D.BricsCAD.V26.dll
  embedded MCP server
        |
        | ExecuteInApplicationContext
        v
BricsCAD document/editor/database + QS3D commands
```

No Node process, no second Git checkout, and no separate probe DLL is required for this shipping path.

## Local endpoint and authentication

When the QS3D plugin loads, it starts an HTTP MCP listener on loopback only:

```text
MCP:    http://127.0.0.1:8765/mcp
Health: http://127.0.0.1:8765/healthz
```

`/mcp` requires `Authorization: Bearer <token>`.

Token precedence:

1. `QS3D_MCP_BEARER_TOKEN` environment variable, when explicitly supplied and sufficiently long.
2. Generated local token stored in `%APPDATA%\QS3D\mcp-bearer-token.txt`.
3. An ephemeral process token only when the local token file cannot be created.

The plugin never binds this service to `0.0.0.0`. Internet exposure is delegated to an authenticated
HTTPS tunnel rather than opening a LAN/public listener inside BricsCAD.

## ChatGPT / Cloudflare example

With BricsCAD running and the QS3D plugin loaded, an operator can expose the loopback MCP endpoint with
Cloudflare Tunnel:

```powershell
cloudflared tunnel --url http://127.0.0.1:8765 --http-host-header 127.0.0.1:8765
```

Use the generated HTTPS hostname plus `/mcp` as the custom MCP URL, for example:

```text
https://<generated-host>/mcp
```

Use the contents of `%APPDATA%\QS3D\mcp-bearer-token.txt` as the Bearer credential. For a stable named
Cloudflare Tunnel, map the same local origin and set `QS3D_MCP_PUBLIC_URL` only if the Ribbon dashboard
should display the stable public URL.

## TOOL > MCP (AI)

The existing TOOL Ribbon panel is the owner-facing control surface:

- **Cài đặt MCP** — shows the embedded local endpoint, Bearer token source/value and optional public URL.
- **Tài liệu MCP** — opens the generated one-machine setup guide.
- **Bảng điều khiển AI** — shows server state plus a real MCP protocol probe.
- **Kiểm tra kết nối** — runs `initialize → notifications/initialized → tools/list`; this is not a TCP-only check.

Additional command-line service controls are available as `QS3DMCPSTART` and `QS3DMCPSTOP`.

## MCP tools

Read-oriented tools:

- `connector_info`
- `qs3d_status`
- `cad_active_document`
- `cad_selection`
- `cad_database_snapshot`

Mutation/control tools:

- `qs3d_run_command` — accepts only one command name matching `^QS3D[A-Za-z0-9_]*$` and requires
  `confirmMutation=true`. Interactive QS3D commands continue prompting in BricsCAD normally.
- `cad_cancel_command` — sends ESC to the active BricsCAD document.

The server intentionally does **not** expose PowerShell, `cmd.exe`, arbitrary process launch, arbitrary
DLL load, arbitrary filesystem access, or unrestricted raw CAD command text.

## Threading and lifecycle safety

HTTP parsing and network I/O run on background threads. Any operation touching BricsCAD
document/editor/database state is marshalled through
`Application.DocumentManager.ExecuteInApplicationContext` and bounded by a timeout.

The listener is owned by plugin lifecycle:

```text
PluginEntry.Initialize -> McpEmbeddedServer.Start
PluginEntry.Terminate  -> McpEmbeddedServer.Stop
```

MCP startup is fail-soft. A port/auth service failure must not prevent core QS3D CAD commands from
loading.

## Qualification

Remote/static CI can validate source contracts and compile both host-major lanes where their normal CI
supports it. It cannot prove a licensed BricsCAD process, a real Cloudflare tunnel, or ChatGPT Web
runtime behavior.

The local qualification for the exact candidate should verify:

1. load the exact V25/V26 plugin build;
2. TOOL > MCP buttons resolve to the embedded commands;
3. `/healthz` responds on loopback;
4. authenticated `initialize`, `notifications/initialized`, and `tools/list` succeed;
5. read tools return the active disposable drawing state;
6. a confirmed safe QS3D command reaches the same active document;
7. Cloudflare forwards only to the loopback origin;
8. the custom MCP client can list/call tools through the HTTPS `/mcp` URL;
9. plugin unload/BricsCAD shutdown releases port 8765 cleanly.

Do not promote source/CI evidence to `LOCAL_PASS`; record exact-SHA local evidence separately.
