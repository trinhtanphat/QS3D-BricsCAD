# QS3D MCP — CANONICAL START HERE

**Status:** SOURCE MERGED / RUNTIME `PENDING_LOCAL`  
**Parent architecture issue:** `#4352`  
**OAuth integration issue:** `#4584`  
**Merged OAuth PR:** `#4597`  
**Merged OAuth source head:** `3f4cc36448b81dba15da741138807fe59793aa60`  
**Merged main commit:** `d3e6e4d6e9f423efbd2d236d600110a6a2ced1f5`  
**End-user model:** one QS3D install, click/browser-login setup, no PowerShell/CMD/Node/second MCP repository.

> **MCP AGENTS MUST START HERE.** The architecture established by #4352 remains the parent product contract. OAuth/DCR onboarding added by #4584/#4597 is now the canonical ChatGPT authentication path. Do not reconstruct the product from stale bearer-only docs or the historical second MCP repository.

## 1. Canonical architecture

```text
ChatGPT Web / custom MCP
  -> HTTPS public /mcp
  -> OAuth 2.1 access token
  -> Cloudflare Named Tunnel
  -> http://127.0.0.1:8765
       +-- /.well-known/oauth-protected-resource[/mcp]
       +-- /.well-known/oauth-authorization-server
       +-- /oauth/register
       +-- /oauth/authorize
       +-- /oauth/token
       +-- /mcp
  -> McpEmbeddedServerV2
  -> McpCadAgentRuntime
  -> BricsCAD API / bounded native command / BricsCAD-only UI fallback
```

There is **one shipping repository and one end-user installation**: `trinhtanphat/QS3D-BricsCAD`. The historical `QS3D-CAD-MCP` repository is reference/history only and must not return as a second runtime, Node service, clone, or user setup requirement.

The embedded server binds only loopback `127.0.0.1:8765`. Public access is delegated to the configured HTTPS Cloudflare tunnel.

## 2. Active source

Inspect these first:

1. `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` — active loopback HTTP/MCP transport, OAuth routing, legacy engineering-bearer compatibility, MCP sessions, schemas/results and admission limits.
2. `src/QS3D.BricsCAD.V25/McpOAuthAuthorizationServer.cs` — protected-resource/authorization-server discovery, DCR, authorization code, PKCE S256, access/refresh credentials, optional `offline_access`, refresh rotation/replay protection and resource binding.
3. `src/QS3D.BricsCAD.V25/McpOAuthConsent.cs` — bounded local BricsCAD approve/deny prompt.
4. `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs` — direct CAD operations, bounded command dispatch, BricsCAD-process-only UI fallback, emergency recovery and audit.
5. `src/QS3D.BricsCAD.V25/McpTopLevelJson.cs` — security-sensitive top-level JSON parsing and mutation confirmation.
6. `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs` — click-first `QS3DMCPAGENTCENTER` UI and self-test surface.
7. `src/QS3D.BricsCAD.V25/McpCloudflaredBootstrapper.cs` — managed verified `cloudflared` bootstrap.
8. `src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs` — provider-browser login, persistent Named Tunnel/DNS/autostart.
9. `src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs` — advanced token / Quick Tunnel fallback.
10. `src/QS3D.BricsCAD.V25/McpPublicEndpointResolver.cs` — one validated public HTTPS `/mcp` source of truth.

`src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs` is legacy historical source and is not the active compiled transport.

## 3. Normal end-user flow — URL + OAuth

The production path is click-first:

1. Open BricsCAD with QS3D loaded.
2. Open **TOOL > MCP (AI) > Agent Center** (`QS3DMCPAGENTCENTER`).
3. Confirm embedded MCP is running; run protocol and read-only Agent self-test.
4. Use the UI to install/update `cloudflared` if required.
5. Click Cloudflare login and authenticate only on Cloudflare's provider-owned browser page.
6. Create/reuse a persistent Named Tunnel and public hostname. Named Tunnel is the production default.
7. Copy only the canonical public HTTPS URL ending in `/mcp`.
8. In ChatGPT's basic custom MCP/app screen, enter the URL and choose **OAuth**.
9. Do not fill Advanced OAuth in the normal path. QS3D advertises OAuth discovery and Dynamic Client Registration.
10. When BricsCAD shows the local OAuth consent prompt, approve only the connection you initiated.
11. ChatGPT completes authorization-code + PKCE S256, receives an access token and scans/calls MCP tools.

The legacy static bearer token remains a local/backward-compatible engineering path. It is **not** the normal ChatGPT onboarding UX and should not be copied into normal user instructions.

## 4. OAuth contract

The embedded authorization server exposes:

```text
GET  /.well-known/oauth-protected-resource
GET  /.well-known/oauth-protected-resource/mcp
GET  /.well-known/oauth-authorization-server
POST /oauth/register
GET  /oauth/authorize
POST /oauth/token
```

Required invariants:

- public OAuth clients use `token_endpoint_auth_method=none`;
- ChatGPT DCR callback is restricted to `https://chatgpt.com/connector/oauth/<id>`;
- authorization code uses PKCE `S256`;
- authorization codes are short-lived, process-bound and one-use;
- MCP permission is `qs3d:mcp`;
- `offline_access` is optional authorization-server scope only;
- protected-resource metadata and `WWW-Authenticate` advertise `qs3d:mcp`, not `offline_access`;
- access tokens carry the MCP resource permission only;
- refresh tokens are issued only when `offline_access` was granted;
- successful refresh rotates the refresh token and consumes the previous token;
- consumed refresh-token replay is rejected;
- refresh cannot elevate scope;
- credentials are bound to the exact public MCP resource/client;
- refresh credentials are process-bound, so restarting BricsCAD requires reauthorization;
- signed opaque credentials use HMAC-SHA256 and constant-time secret comparison;
- no OAuth endpoint launches arbitrary processes or exposes credentials.

A Quick Tunnel is testing/fallback only. Its hostname can rotate; because OAuth credentials are resource-bound, a changed Quick Tunnel URL requires a fresh ChatGPT connection. A persistent Named Tunnel avoids that churn in production.

## 5. MCP transport and safety

`/mcp` accepts a validated OAuth access token for the current public resource or the legacy static engineering bearer. An unauthenticated protected request returns HTTP `401` with an OAuth-aware `WWW-Authenticate: Bearer` challenge when a valid public endpoint is available.

The active transport must continue to preserve:

- loopback-only binding;
- exact `application/json` admission for MCP POST;
- bounded headers, body, sessions and concurrent clients;
- duplicate security-sensitive-header rejection;
- unsupported transfer-encoding rejection;
- Streamable-HTTP Origin/DNS-rebinding defense;
- negotiated MCP protocol/session lifecycle;
- `tools/list`, `tools/call`, ping, notification and session DELETE behavior;
- top-level `confirmMutation=true` for ordinary mutations;
- no arbitrary PowerShell, `cmd.exe`, shell/process launch or desktop-wide automation.

The `cad_entity_transform` schema must remain valid JSON and preserve its required boolean `confirmMutation` contract.

## 6. CAD agent model

Decision order is **direct CAD API first → bounded allowlisted native command second → BricsCAD-process-only mouse/keyboard fallback last**.

Representative inspection tools include `connector_info`, `qs3d_status`, `cad_active_document`, `cad_selection`, `cad_database_snapshot`, `cad_entity_inspect`, `cad_view_state`, `cad_wait_idle`, `cad_sysvar`, `cad_command_catalog`, and `cad_audit_tail`.

Representative direct mutations include line/circle/arc/polyline/text/MText creation, `cad_entity_transform`, delete/layer operations and `qs3d_run_command`. Complex native workflows use `cad_command_sequence`. UI-only fallback is confined to BricsCAD-owned windows. `cad_agent_stop`, `cad_cancel_command`, and confirmed `cad_agent_resume` preserve recovery.

## 7. Source verification

For OAuth transport changes, run/inspect at minimum:

- `scripts/preflight-mcp-oauth.py`
- `scripts/preflight-mcp-tools-list-json.py`
- `scripts/preflight-embedded-mcp.py`
- `scripts/preflight-mcp-full-agent.py`
- `scripts/preflight-mcp-production-hardening.py`
- `scripts/preflight-mcp-session-handoff.py`
- `scripts/preflight-mcp-loopback-readonly.py`
- deterministic Core smoke and trusted V25 plugin compile.

The merged #4584 source candidate `3f4cc36448b81dba15da741138807fe59793aa60` passed exact-head protected preflight + core CI before #4597 merged as `d3e6e4d6e9f423efbd2d236d600110a6a2ced1f5`.

## 8. LOCAL-024 — required runtime qualification

Hosted CI is not licensed BricsCAD/Cloudflare/ChatGPT runtime evidence. Runtime remains **`PENDING_LOCAL`** until a clean exact merged/release descendant is exercised on real Windows with licensed BricsCAD V25/V26.

The local matrix must cover:

1. exact DLL load in V25/V26 and `127.0.0.1:8765/healthz`;
2. Agent Center protocol check and read-only self-test;
3. stable public HTTPS `/mcp` through Cloudflare;
4. protected-resource and authorization-server discovery;
5. ChatGPT DCR using only basic URL + OAuth setup;
6. deny then approve local consent;
7. PKCE S256 token exchange and `tools/list`;
8. representative read-only calls;
9. one-use auth-code replay rejection;
10. no-refresh behavior without `offline_access`;
11. refresh issuance with `offline_access`, rotation and old-token replay rejection;
12. scope/resource mismatch rejection;
13. BricsCAD restart invalidating process-bound refresh credentials and requiring reauthorization;
14. legacy engineering-bearer compatibility;
15. Quick Tunnel URL invalidation and persistent Named Tunnel reconnect/autostart;
16. one confirmed disposable-DWG mutation plus audit/emergency-stop/cancel;
17. save/reopen and clean shutdown.

Never commit access/refresh tokens, static bearer secrets, Cloudflare credentials, private paths/DWGs, proprietary BricsCAD binaries or unsanitized screenshots.

## 9. Continuation rules

For future MCP work:

1. read repository governance first, then this file;
2. treat #4352 as parent architecture and #4584/#4597 as the merged OAuth onboarding integration;
3. resolve current `main` and current source before changing anything;
4. edit active V2/OAuth source, not the legacy monolith;
5. preserve one repo/runtime, click-first setup, OAuth basic-screen onboarding, API-first CAD control and BricsCAD-only UI boundaries;
6. do not open a competing MCP carrier for the same scope;
7. update `docs/CHATGPT-MCP-INTEGRATION.md`, `docs/MCP-FULL-CAD-AGENT.md` and the single LOCAL-024 handoff when behavior changes;
8. never promote hosted CI to `LOCAL_PASS`.

## 10. Definition of done

Source integration for #4584 is merged. Full runtime completion is achieved only when the exact intended merged/release descendant passes LOCAL-024 with real BricsCAD V25/V26 + Cloudflare + ChatGPT, sanitized evidence is recorded, and the issue can truthfully move from `PENDING_LOCAL` to completed.
