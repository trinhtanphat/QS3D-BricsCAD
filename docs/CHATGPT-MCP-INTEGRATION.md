# ChatGPT Web ↔ QS3D embedded MCP

Status: source implementation for issue #4314. Licensed BricsCAD and end-to-end ChatGPT/tunnel qualification remain `LOCAL_ONLY`.

## Decision

`QS3D-BricsCAD` is the only repository/package required for the shipping integration. The MCP server and CAD-inspection tools are embedded directly in the BricsCAD plugin. A separate MCP repository, Node service, or development probe DLL is **not** an install or runtime dependency.

The product remains a BricsCAD-hosted plugin. The MCP listener is an optional plugin service; it does not turn QS3D into a standalone CAD application.

## Runtime architecture

The embedded service is private by default:

```text
custom MCP client / locally administered HTTPS ingress
        |
        | Authorization: Bearer <QS3D token>
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

For ChatGPT Web, do **not** treat `localhost` as a directly connectable remote MCP URL. Current OpenAI guidance for a private/on-premises/developer-machine MCP is Secure MCP Tunnel: `tunnel-client` runs inside the same trust boundary, reaches the private MCP endpoint locally, and opens an outbound HTTPS path to OpenAI. The ChatGPT developer-mode app then uses the tunnel connection rather than the loopback URL directly.

```text
ChatGPT Web developer-mode app
        |
        v
OpenAI-hosted Secure MCP Tunnel endpoint
        |
        | outbound HTTPS polling/response path
        v
tunnel-client on the CAD workstation/network
        |
        | private HTTP MCP
        v
QS3D embedded MCP -> BricsCAD
```

The current embedded endpoint uses static Bearer authentication. Current ChatGPT developer-mode documentation publicly describes OAuth, No Authentication, and Mixed Authentication. Therefore an end-to-end ChatGPT tunnel path must be locally qualified with an auth-compatible configuration before it is recorded as `LOCAL_PASS`; do not weaken the plugin's authentication or claim that static Bearer is natively supported by ChatGPT without evidence.

References to re-check at qualification time because the product surface can change:

- `https://help.openai.com/en/articles/12584461`
- `https://developers.openai.com/api/docs/guides/secure-mcp-tunnels`
- `https://developers.openai.com/api/docs/guides/developer-mode`

## Local endpoint and authentication

When the QS3D plugin loads, V25 and V26 start the same shared embedded MCP implementation fail-soft:

```text
MCP:    http://127.0.0.1:8765/mcp
Health: http://127.0.0.1:8765/healthz
```

`/mcp` requires `Authorization: Bearer <token>`.

Token precedence:

1. `QS3D_MCP_BEARER_TOKEN` environment variable, when explicitly supplied and sufficiently long.
2. Generated local token stored in `%APPDATA%\QS3D\mcp-bearer-token.txt`.
3. An ephemeral process token only when the local token file cannot be created.

The plugin never binds this service to `0.0.0.0`. Any public or remote reachability is delegated to a separately administered connector/tunnel rather than opening a LAN/public listener inside BricsCAD. `QS3D_MCP_PUBLIC_URL` is display-only; it does not create a tunnel or change listener security.

## TOOL > MCP (AI)

The existing TOOL Ribbon panel remains the owner-facing control surface:

- **Cài đặt MCP** — shows the embedded local endpoint, Bearer token source/value and optional display-only public URL.
- **Tài liệu MCP** — writes and opens the generated one-machine integration guide.
- **Bảng điều khiển AI** — shows server state plus a real MCP protocol probe.
- **Kiểm tra kết nối** — runs `initialize → notifications/initialized → tools/list`; this is not a TCP-only check.

Additional command-line service controls are available as `QS3DMCPSTART` and `QS3DMCPSTOP`.

## MCP protocol surface

The source implementation supports Streamable HTTP-style POST requests and the following protocol flow:

```text
initialize
-> notifications/initialized
-> ping / tools/list / tools/call
```

The server issues and validates `Mcp-Session-Id`, returns `MCP-Protocol-Version`, bounds request sizes, expires idle sessions, and closes each HTTP connection after the response. Network parsing runs off the CAD application context; CAD state access is marshalled back through `Application.DocumentManager.ExecuteInApplicationContext` with a bounded wait.

## MCP tools

Read-oriented tools:

- `connector_info`
- `qs3d_status`
- `cad_active_document`
- `cad_selection`
- `cad_database_snapshot`

Mutation/control tools:

- `qs3d_run_command` — accepts only one command name matching `^QS3D[A-Za-z0-9_]*$` and requires `confirmMutation=true`. Interactive QS3D commands continue prompting in BricsCAD normally.
- `cad_cancel_command` — sends ESC to the active BricsCAD document.

The server intentionally does **not** expose PowerShell, `cmd.exe`, arbitrary process launch, arbitrary DLL load, arbitrary filesystem access, or unrestricted raw CAD command text.

`qs3d_run_command` is namespace-constrained plus explicitly confirmation-gated; it is not described as a complete semantic allowlist of every future `QS3D*` command. Any future expansion that exposes more mutation authority must tighten the command/tool contract rather than relying only on naming convention.

## Lifecycle safety

HTTP parsing and network I/O run on background threads. Any operation touching BricsCAD document/editor/database state is marshalled through `ExecuteInApplicationContext` and bounded by timeout.

The listener is owned by both host-major plugin entry points:

```text
V25 PluginEntry.Initialize -> McpEmbeddedServer.Start
V25 PluginEntry.Terminate  -> McpEmbeddedServer.Stop
V26 PluginEntry.Initialize -> McpEmbeddedServer.Start
V26 PluginEntry.Terminate  -> McpEmbeddedServer.Stop
```

MCP startup is fail-soft. A port/auth service failure must not prevent core QS3D CAD commands from loading. Ribbon command override initialization and teardown preserve the newer transactional/fail-soft Ribbon lifecycle from `main`.

## ChatGPT Web / Secure MCP Tunnel qualification

OpenAI currently documents Secure MCP Tunnel as the private-server connection path. `tunnel-client` runs where it can reach the private MCP over HTTP or stdio, uses outbound HTTPS to OpenAI, and ChatGPT creates a developer-mode app with **Tunnel** selected as the connection. For an HTTP MCP server, OpenAI documents `--mcp-server-url` as the local/private target form.

For QS3D, the candidate local target is:

```text
http://127.0.0.1:8765/mcp
```

Do not claim this path is ready merely because `/healthz` or the plugin's self-probe succeeds. The exact candidate must prove that the chosen Secure MCP Tunnel/app authentication configuration can reach the Bearer-protected endpoint without disabling the protection.

A separately administered public HTTPS proxy/tunnel remains a possible path for custom clients or future public deployment, but it is not the default ChatGPT-private-server recommendation and it must preserve authentication and the loopback-only QS3D listener boundary.

## Qualification

Remote/static CI can validate source contracts and compile host-major lanes where their normal CI supports them. It cannot prove a licensed BricsCAD process, Secure MCP Tunnel, workspace permissions, or ChatGPT Web runtime behavior.

Local qualification for the exact candidate should verify:

1. load the exact V25 build and exact V26 build where each host is available;
2. confirm MCP startup is fail-soft and `QS3DMCPSTOP` releases port 8765 cleanly;
3. TOOL > MCP buttons resolve to the embedded HTTP commands;
4. `/healthz` responds on loopback only;
5. authenticated `initialize`, `notifications/initialized`, and `tools/list` succeed;
6. read tools return the active disposable drawing state;
7. a confirmed safe QS3D command reaches the same active document;
8. the tunnel client can reach the private MCP without disabling required authentication;
9. ChatGPT developer-mode app discovery can list the exact candidate tools through the Secure MCP Tunnel connection;
10. an approved read call succeeds end-to-end, and any mutation test uses a disposable drawing plus explicit confirmation;
11. plugin unload/BricsCAD shutdown releases port 8765 cleanly.

Do not promote source/CI evidence to `LOCAL_PASS`; record exact-SHA local evidence separately.

## Release boundary

The older release `v0.1.0-preview.10239` predates this canonical #4314 integration carrier. The MCP source is present on the task branch until the same carrier passes exact-head CI, protected PR checks, and merges to `main`; only a later release built from a containing `main` SHA can truthfully be called a release with the embedded MCP implementation.
