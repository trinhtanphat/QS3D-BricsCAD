# QS3D MCP — CANONICAL START HERE

**Status:** SOURCE TRACKED / RUNTIME `PENDING_LOCAL`  
**Parent architecture issue:** `#4352`  
**OAuth integration issue:** `#4584`  
**Desktop/guided-control extension:** `#4629`  
**Merged OAuth PR:** `#4597`  
**Merged OAuth source head:** `3f4cc36448b81dba15da741138807fe59793aa60`  
**End-user model:** one QS3D install, click/browser-login setup, no PowerShell/CMD/Node/second MCP repository.

> **MCP AGENTS MUST START HERE.** The architecture established by #4352 remains the parent product contract. OAuth/DCR onboarding added by #4584/#4597 is the canonical ChatGPT authentication path. #4629 extends that same embedded runtime with bounded Windows desktop tools, local desktop consent, visible emergency controls, guided onboarding and versioned recovery. Do not reconstruct the product from stale bearer-only docs or the historical second MCP repository.

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
       +-- direct BricsCAD API
       +-- bounded native command workflows
       +-- BricsCAD-process UI fallback
       +-- McpDesktopAutomationRuntime (explicit desktop_* only)
  -> local desktop-consent / blue overlay / Esc×2 stop boundary
```

There is **one shipping repository and one end-user installation**: `trinhtanphat/QS3D-BricsCAD`. The historical `QS3D-CAD-MCP` repository is reference/history only and must not return as a second runtime, Node service, clone, or user setup requirement.

The embedded server binds only loopback `127.0.0.1:8765`. Public access is delegated to the configured HTTPS Cloudflare tunnel.

## 2. Active source

Inspect these first:

1. `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` — active loopback HTTP/MCP transport, OAuth routing, legacy engineering-bearer compatibility, MCP sessions, schemas/results and admission limits.
2. `src/QS3D.BricsCAD.V25/McpOAuthAuthorizationServer.cs` — protected-resource/authorization-server discovery, DCR, authorization code, PKCE S256, access/refresh credentials, optional `offline_access`, refresh rotation/replay protection and resource binding.
3. `src/QS3D.BricsCAD.V25/McpOAuthConsent.cs` — bounded local BricsCAD approve/deny prompt for OAuth authorization.
4. `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs` — direct CAD operations, bounded command dispatch, BricsCAD-process UI fallback, canonical mutation epoch/emergency recovery and audit.
5. `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs` — explicit bounded `desktop_*` observation/input/clipboard/screenshot tools for the current interactive Windows session.
6. `src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs` — non-persistent local desktop consent, blue active-control overlay and physical Esc×2 emergency stop.
7. `src/QS3D.BricsCAD.V25/McpTopLevelJson.cs` — security-sensitive top-level JSON parsing and mutation confirmation.
8. `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs` — guided four-tab `QS3DMCPAGENTCENTER` UX.
9. `src/QS3D.BricsCAD.V25/McpLocalAgentClient.cs` — local loopback protocol/self-test/emergency-control client.
10. `src/QS3D.BricsCAD.V25/McpAgentExperience.cs` — bounded local onboarding/action/error timeline; operational metadata only.
11. `src/QS3D.BricsCAD.V25/McpProjectRecoveryService.cs` — autosave/BAK policy and bounded versioned DWG recovery-to-copy.
12. `src/QS3D.BricsCAD.V25/McpFirstRunExperience.cs` — rate-limited first-run onboarding toast.
13. `src/QS3D.BricsCAD.V25/McpCloudflaredBootstrapper.cs` — managed verified `cloudflared` bootstrap.
14. `src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs` — provider-browser login, persistent Named Tunnel/DNS/autostart.
15. `src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs` — advanced token / Quick Tunnel fallback.
16. `src/QS3D.BricsCAD.V25/McpPublicEndpointResolver.cs` — one validated public HTTPS `/mcp` source of truth.

`src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs` is legacy historical source and is not the active compiled transport.

## 3. Normal end-user flow — guided URL + OAuth

The production path is click-first:

1. Open BricsCAD with QS3D loaded.
2. Open **TOOL > MCP (AI) > Agent Center** (`QS3DMCPAGENTCENTER`).
3. In **Kết nối**, confirm embedded MCP is running.
4. Install/update `cloudflared` from QS3D if required.
5. Click **Đăng nhập Cloudflare** and authenticate only on Cloudflare's provider-owned browser page. QS3D never asks for the Cloudflare password.
6. Create/reuse a persistent **Named Tunnel** and stable public hostname. Named Tunnel is the production default.
7. Copy only the canonical public HTTPS URL ending in `/mcp`.
8. Click **Mở ChatGPT**. ChatGPT identity remains owned by the normal system browser; QS3D does not capture the ChatGPT password or scrape browser cookies.
9. In ChatGPT's basic custom MCP/app screen, enter the URL and choose **OAuth**. Do not fill Advanced OAuth in the normal path; QS3D advertises discovery + Dynamic Client Registration.
10. When BricsCAD shows the local OAuth consent prompt, approve only the connection you initiated.
11. ChatGPT completes authorization-code + PKCE S256, receives an access token and scans/calls MCP tools.
12. Return to Agent Center, acknowledge that the MCP was added, then run protocol/read-only verification.
13. Enable **desktop control** locally in the **Agent** tab only when a workflow genuinely needs to cross outside BricsCAD.

The legacy static bearer token remains a local/backward-compatible engineering path under **Nâng cao**. It is **not** the normal ChatGPT onboarding UX.

`Quick Tunnel · test only` remains a temporary diagnostic fallback. Its hostname can rotate; because OAuth credentials are resource-bound, a changed Quick Tunnel URL requires a fresh ChatGPT connection. A persistent Named Tunnel avoids that churn in production.

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

## 5. MCP transport, desktop boundary and safety

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
- no arbitrary PowerShell, `cmd.exe`, shell/process launch, arbitrary executable path or unrestricted native-command surface.

The `cad_entity_transform` schema must remain valid JSON and preserve its required boolean `confirmMutation` contract.

### Desktop-wide tools

The explicit desktop namespace is:

- read-only observation: `desktop_cursor_position`, `desktop_window_list`, `desktop_foreground_window`;
- mutation/input: `desktop_window_focus`, `desktop_mouse_move`, `desktop_mouse_click`, `desktop_mouse_scroll`, `desktop_type`, `desktop_key`, `desktop_clipboard_write`;
- sensitive reads: `desktop_clipboard_read`, `desktop_screenshot`.

Desktop mutation requires both `confirmMutation=true` **and** local desktop consent. Clipboard/screenshot reads require `confirmSensitiveRead=true` **and** local desktop consent. Local desktop consent is process-memory-only, resets at every BricsCAD start and cannot be enabled by an MCP call.

A guarded desktop action displays a click-through blue border/banner. Physical **Esc ×2 within 1.2 seconds** revokes consent, advances the MCP emergency-stop epoch, hides the overlay and requests BricsCAD command cancellation. Re-enabling desktop control is a local user action.

Desktop targeting is limited to visible top-level windows in the current interactive Windows session. Handles are revalidated before input. Text, click counts, wheel deltas, window lists, clipboard payloads and screenshots are bounded. Typed text, clipboard contents and screenshot pixels must not be persisted into audit records.

## 6. Agent decision model

Decision order is **direct CAD API first → bounded allowlisted native command second → BricsCAD-process UI fallback third → explicit desktop tools only for genuinely cross-application workflows**.

Representative inspection tools include `connector_info`, `qs3d_status`, `cad_active_document`, `cad_selection`, `cad_database_snapshot`, `cad_entity_inspect`, `cad_view_state`, `cad_wait_idle`, `cad_sysvar`, `cad_command_catalog`, `cad_audit_tail` and read-only desktop observation.

Representative direct mutations include line/circle/arc/polyline/text/MText creation, `cad_entity_transform`, delete/layer operations and `qs3d_run_command`. Complex native workflows use `cad_command_sequence`. BricsCAD UI fallback stays confined to BricsCAD-owned windows. `cad_agent_stop`, `cad_cancel_command`, and confirmed `cad_agent_resume` preserve recovery.

A successful tool call is not proof the drawing or external application state is correct. Re-inspect state before consequential follow-up actions and before final save/plot.

## 7. Guided local status and recovery

QS3D does not scrape the ChatGPT web conversation. ChatGPT remains the conversation UI. Agent Center mirrors only bounded local operational metadata such as onboarding state, current MCP action, next step, errors and recovery events. Normal MCP results/errors flow back to ChatGPT through `tools/call`.

Recovery uses two layers:

1. preserve a shorter existing BricsCAD autosave interval, otherwise ensure `SAVETIME <= 5`, and enable `ISAVEBAK=1`;
2. while CAD is idle, keep bounded coherent on-disk DWG copies under `%LOCALAPPDATA%\QS3D\Backups`, maximum 30 per drawing.

Recovery verifies the source did not change during copying and always restores to a new `Recovered` copy. It does not silently overwrite the active/original DWG.

Both V25 and V26 host entries start embedded MCP, persistent tunnel reconnect, recovery and first-run onboarding. Teardown revokes desktop consent before network services stop.

## 8. Source verification

For the current MCP surface, run/inspect at minimum:

- `scripts/preflight-mcp-oauth.py`
- `scripts/preflight-mcp-tools-list-json.py`
- `scripts/preflight-embedded-mcp.py`
- `scripts/preflight-mcp-full-agent.py`
- `scripts/preflight-mcp-desktop-function-calling.py`
- `scripts/test-mcp-guided-onboarding-control-recovery-source.py`
- `scripts/preflight-mcp-production-hardening.py`
- `scripts/preflight-mcp-session-handoff.py`
- `scripts/preflight-mcp-loopback-readonly.py`
- deterministic Core smoke and trusted V25/V26 compilation where selected by repository CI.

Hosted/source evidence does not replace licensed runtime qualification.

## 9. LOCAL-024 — required runtime qualification

Hosted CI is not licensed BricsCAD/Cloudflare/ChatGPT/Windows-desktop runtime evidence. Runtime remains **`PENDING_LOCAL`** until a clean exact intended merged/release descendant is exercised on real Windows with licensed BricsCAD V25/V26.

The local matrix must cover:

1. exact DLL load in V25/V26 and `127.0.0.1:8765/healthz`;
2. guided four-tab Agent Center plus first-run toast;
3. stable public HTTPS `/mcp` through provider-browser Cloudflare login + Named Tunnel;
4. protected-resource and authorization-server discovery;
5. ChatGPT DCR using only basic URL + OAuth setup;
6. deny then approve local OAuth consent;
7. PKCE S256 token exchange and `tools/list`, including the complete `desktop_*` catalog;
8. representative read-only CAD calls plus desktop cursor/window observation;
9. local desktop-consent OFF rejection and local enable behavior;
10. blue overlay while guarded desktop input/sensitive reads run;
11. `desktop_clipboard_read` and `desktop_screenshot` rejection without acknowledgement, then bounded success on disposable content;
12. confirmed disposable window/mouse/type/key/clipboard-write behavior;
13. physical Esc×2 emergency stop, CAD cancel and required local re-enable;
14. one-use auth-code replay rejection;
15. no-refresh behavior without `offline_access`;
16. refresh issuance with `offline_access`, rotation and old-token replay rejection;
17. scope/resource mismatch rejection;
18. BricsCAD restart invalidating process-bound refresh credentials and local desktop consent;
19. legacy engineering-bearer compatibility;
20. Quick Tunnel URL invalidation and persistent Named Tunnel reconnect/autostart;
21. autosave/BAK policy, versioned snapshot retention and recovery-to-new-copy on a disposable drawing;
22. one confirmed disposable-DWG mutation plus audit/emergency-stop/cancel;
23. save/reopen and clean process shutdown.

Never commit access/refresh tokens, static bearer secrets, Cloudflare credentials, private paths/DWGs, clipboard contents, typed secrets, proprietary BricsCAD binaries or unsanitized screenshots.

## 10. Continuation rules

For future MCP work:

1. read repository governance first, then this file;
2. treat #4352 as parent architecture, #4584/#4597 as merged OAuth onboarding, and #4629 as the desktop/guided-control extension;
3. resolve current `main` and current source before changing anything;
4. edit active V2/OAuth/runtime source, not the legacy monolith;
5. preserve one repo/runtime, click-first setup, system/provider-browser identity ownership, OAuth basic-screen onboarding and API-first CAD control;
6. preserve local desktop consent + visible active-control + Esc×2 emergency boundaries for desktop-wide automation;
7. do not open a competing MCP carrier for the same scope;
8. update `docs/CHATGPT-MCP-INTEGRATION.md`, `docs/MCP-FULL-CAD-AGENT.md`, `docs/MCP-GUIDED-ONBOARDING-RECOVERY.md` and the single LOCAL-024 handoff when behavior changes;
9. never promote hosted CI to `LOCAL_PASS`.

## 11. Definition of done

Source integration for OAuth (#4584/#4597) is already merged. Source integration for #4629 is complete only after its canonical protected PR lands in `main` with current required checks satisfied.

Full runtime completion is separate: the exact intended merged/release descendant must pass LOCAL-024 with real BricsCAD V25/V26 + Cloudflare + ChatGPT + Windows desktop behavior, sanitized evidence must be recorded, and the runtime item can then move from `PENDING_LOCAL` to completed.
