# ChatGPT Web ↔ QS3D embedded MCP

Status: SOURCE_READY / PENDING_LOCAL  
Canonical issue: #4352  
OAuth integration issue: #4584  
Lane-Key: `issue-4584`

`QS3D-BricsCAD` is the only shipping repository/package required for this integration. The MCP server, OAuth authorization server, Cloudflare onboarding, Agent Center, CAD tools, safety controls, and local audit surface are embedded in the BricsCAD plugin. A second MCP repository, Node runtime, PowerShell, CMD, or manual terminal setup is not part of the normal end-user path.

The canonical Ribbon entry remains `QS3DMCPAGENTCENTER`; `McpPublicEndpointResolver` remains the single validated source of the public HTTPS `/mcp` endpoint.

## Runtime architecture

```text
ChatGPT Web / custom MCP
        |
        | HTTPS + OAuth 2.1 access token
        v
Cloudflare Named Tunnel
        |
        v
http://127.0.0.1:8765
        |
        +-- /.well-known/oauth-protected-resource[/mcp]
        +-- /.well-known/oauth-authorization-server
        +-- /oauth/register        (DCR)
        +-- /oauth/authorize       (authorization code + PKCE S256)
        +-- /oauth/token
        +-- /mcp                   (Streamable HTTP MCP)
                |
                v
QS3D.BricsCAD.V25.dll / QS3D.BricsCAD.V26.dll
  -> McpCadAgentRuntime
  -> BricsCAD document/editor/database
```

The embedded service binds only `127.0.0.1:8765`. Public HTTPS is owned by the QS3D-managed Cloudflare tunnel.

Production uses a persistent **Named Tunnel**. A Quick Tunnel is supported for temporary testing, but its `trycloudflare.com` hostname can change; OAuth clients/tokens are intentionally resource-bound, so a changed public URL requires a fresh ChatGPT connection.

## Normal ChatGPT setup — basic screen only

The target normal-user flow is:

1. Open BricsCAD + QS3D.
2. Open **TOOL > MCP (AI) > Agent Center**.
3. Confirm the embedded MCP is `RUNNING` and run the read-only MCP protocol/self-test checks.
4. Use **Cloudflare browser login + Named Tunnel** for production, or **Quick Tunnel** for a temporary local test.
5. Copy the current public HTTPS MCP URL, for example `https://qs3d.example.com/mcp`.
6. In ChatGPT's **basic custom MCP/plugin screen**, enter that URL and choose **OAuth**.
7. Do **not** open/fill Advanced OAuth for the normal flow. QS3D advertises Dynamic Client Registration itself.
8. Create/connect the plugin. ChatGPT discovers OAuth metadata, dynamically registers its connector callback, starts authorization-code + PKCE S256, and sends the authorization request to QS3D.
9. QS3D displays a local approval prompt inside BricsCAD. Approve only when the ChatGPT connection is expected.
10. ChatGPT exchanges the one-time code for an access token and then calls `/mcp` with `Authorization: Bearer <OAuth access token>`.

QS3D never asks for a ChatGPT password or Cloudflare password. Cloudflare credentials are entered only on Cloudflare's provider-owned browser page.

## OAuth discovery and protocol contract

The public tunnel exposes these endpoints through the same embedded listener:

```text
GET  /.well-known/oauth-protected-resource
GET  /.well-known/oauth-protected-resource/mcp
GET  /.well-known/oauth-authorization-server
POST /oauth/register
GET  /oauth/authorize
POST /oauth/token
```

The implementation supports:

- OAuth 2.1-style authorization-code flow;
- public clients (`token_endpoint_auth_method=none`);
- Dynamic Client Registration for the ChatGPT connector;
- PKCE `S256` only;
- exact ChatGPT connector callback allowlisting under `https://chatgpt.com/connector/oauth/<id>`;
- one-time authorization codes with replay protection;
- short-lived access tokens plus bounded refresh tokens;
- token/client/code binding to the exact public MCP resource and required `qs3d:mcp` scope;
- signed opaque tokens with constant-time verification;
- explicit local BricsCAD approval before a code is issued;
- strict duplicate/malformed form/query handling and bounded registration metadata.

The legacy generated static bearer token remains available only for local/backward-compatible engineering paths. It is **not** the normal ChatGPT plugin authentication path and is never exposed through an OAuth token.

## MCP transport

Local endpoints:

```text
MCP:    http://127.0.0.1:8765/mcp
Health: http://127.0.0.1:8765/healthz
```

`/mcp` accepts either:

- a validated OAuth access token for the current public MCP resource; or
- the existing static engineering bearer for backward compatibility.

An unauthenticated MCP request returns HTTP `401` with a `WWW-Authenticate: Bearer` challenge containing protected-resource metadata when a valid public endpoint is available. That lets ChatGPT discover the OAuth server automatically.

The server continues to enforce bounded HTTP headers/body/session/concurrency state, exact JSON media-type admission, duplicate security-sensitive-header rejection, transfer-encoding rejection, Streamable-HTTP Origin/DNS-rebinding defense, MCP session/protocol validation, and top-level mutation confirmation.

## Local qualification — what must be tested on the exact candidate SHA

Hosted CI can prove source guards and compilation only. Before claiming `LOCAL_PASS`, test the exact candidate on real Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT:

1. Load the exact candidate DLL in V25 and V26 and confirm MCP reports `RUNNING`.
2. Run Agent Center **MCP protocol check** and **read-only Agent self-test**; both must pass.
3. Start the public tunnel and confirm the displayed public URL ends in `/mcp` and is HTTPS.
4. Request `https://<public-host>/.well-known/oauth-protected-resource/mcp`; verify it names the current public `/mcp` resource and authorization server.
5. Request `https://<public-host>/.well-known/oauth-authorization-server`; verify authorization, token, registration and PKCE S256 metadata are present.
6. In ChatGPT create the QS3D custom MCP using only **URL + OAuth** on the basic screen; leave Advanced OAuth untouched.
7. Verify ChatGPT DCR succeeds and the local BricsCAD OAuth approval dialog appears.
8. Deny once and verify authorization fails without granting MCP access.
9. Retry, approve, and verify ChatGPT completes OAuth then discovers `tools/list` successfully.
10. Call read-only tools such as `connector_info`, `qs3d_status`, `cad_active_document`, and `cad_database_snapshot`.
11. On a disposable drawing, test one confirmed mutation, then verify audit + Emergency Stop/cancel boundaries.
12. Restart a Quick Tunnel and verify the old resource-bound OAuth connection no longer silently authorizes the new URL; reconnect. For production, repeat with a stable Named Tunnel and verify restart/autostart preserves the hostname.
13. Exercise refresh-token behavior and confirm expired/invalid/resource-mismatched tokens are rejected.
14. Save/reopen the disposable drawing and shut BricsCAD down cleanly.

Do not record or commit access tokens, refresh tokens, the legacy bearer, Cloudflare credentials, private paths, customer/private DWGs, proprietary binaries, or unsanitized screenshots.

## Source verification

OAuth/DCR source invariants are guarded by:

```text
python scripts/preflight-mcp-oauth.py
python scripts/preflight-mcp-tools-list-json.py
```

The first guard verifies discovery, DCR, PKCE, token binding, callback allowlisting, local consent, transport wiring and net48 compatibility. The second reconstructs the generated MCP `tools/list` payload and parses it as JSON, including the `cad_entity_transform.confirmMutation` schema regression.

## Qualification boundary

`SOURCE_IMPLEMENTED` means the OAuth/MCP source and hosted contracts exist on the candidate branch. It is not licensed runtime proof.

`LOCAL_PASS` requires the exact candidate SHA to pass the V25/V26 + Cloudflare + ChatGPT matrix above with sanitized evidence in `LOCAL-024`.

`MERGED_MAIN` additionally requires the repository's protected PR/main policy. Do not merge by bypassing the local-runtime requirement or protected-main checks.
