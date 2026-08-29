# ChatGPT Web ↔ QS3D embedded MCP

Status: SOURCE_TRACKED / PENDING_LOCAL  
Parent MCP issue: #4352  
OAuth integration: #4584 / PR #4597  
Desktop/guided-control extension: #4629

`QS3D-BricsCAD` is the only shipping repository/package required for this integration. The MCP server, OAuth authorization server, Cloudflare onboarding, guided Agent Center, CAD tools, bounded desktop tools, safety controls, recovery service and local audit/status surfaces are embedded in the BricsCAD plugin. A second MCP repository, Node runtime, PowerShell, CMD, or manual terminal setup is not part of the normal end-user path.

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
       -> direct BricsCAD API
       -> bounded native commands
       -> BricsCAD-process UI fallback
       -> McpDesktopAutomationRuntime for explicit desktop_* tools
  -> McpDesktopControlSession local consent + blue overlay + Esc×2
```

The embedded service binds only `127.0.0.1:8765`. Public HTTPS is owned by the QS3D-managed Cloudflare tunnel.

Production uses a persistent **Named Tunnel**. `Quick Tunnel · test only` is supported as a temporary diagnostic fallback, but its `trycloudflare.com` hostname can change; OAuth clients/tokens are intentionally resource-bound, so a changed public URL requires a fresh ChatGPT connection.

## Normal ChatGPT setup — guided basic URL + OAuth

1. Open BricsCAD + QS3D.
2. Open **TOOL > MCP (AI) > Agent Center**.
3. In **Kết nối**, confirm/start the embedded MCP.
4. Install/update `cloudflared` from QS3D if required.
5. Click **Đăng nhập Cloudflare**. Complete credentials only in the provider-owned browser page opened by Cloudflare.
6. Create/reuse a stable **Named Tunnel** and public HTTPS hostname.
7. Copy the canonical public MCP URL, for example `https://qs3d.example.com/mcp`.
8. Click **Mở ChatGPT**. ChatGPT login remains owned by the user's normal system browser; QS3D never asks for a ChatGPT password and never scrapes browser cookies.
9. In ChatGPT's basic custom MCP/app screen, enter the public URL and choose **OAuth**.
10. Do **not** fill Advanced OAuth for the normal flow. QS3D advertises Dynamic Client Registration itself.
11. Create/connect the MCP. ChatGPT discovers OAuth metadata, dynamically registers its connector callback, starts authorization-code + PKCE S256, requests `qs3d:mcp` and may request `offline_access`, and sends the authorization request to QS3D.
12. QS3D displays a local approval prompt inside BricsCAD. Approve only when the ChatGPT connection is expected.
13. ChatGPT exchanges the one-time code for an access token and calls `/mcp` with `Authorization: Bearer <OAuth access token>`.
14. Back in Agent Center, click **Đã thêm MCP trong ChatGPT** and run protocol/read-only verification.
15. Leave desktop-wide permission **OFF** unless the task genuinely needs to interact with another Windows application. Enable it locally from **Agent** only when needed.

The legacy generated static bearer token remains available only under **Nâng cao** for local/backward-compatible engineering paths. It is **not** the normal ChatGPT authentication path.

## Agent Center UX

The Control Center is split into four task-focused tabs:

- **Kết nối** — guided MCP/cloudflared/Cloudflare/Named Tunnel/ChatGPT setup;
- **Agent** — local desktop permission, current local MCP action, next step, recent events, Emergency Stop/cancel/resume;
- **Backup & khôi phục** — autosave/BAK status, manual versioned backup, latest recovery-to-copy;
- **Nâng cao** — Quick Tunnel test fallback, engineering bearer compatibility, read-only self-test and audit access.

The UI follows Windows app light/dark preference and pins explicit normal/hover/pressed button colors so foreground/background contrast remains stable.

## OAuth discovery and protocol contract

The public tunnel exposes:

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
- required MCP permission `qs3d:mcp` plus optional authorization-server scope `offline_access`;
- protected-resource metadata and `WWW-Authenticate` advertise only `qs3d:mcp`, never `offline_access`;
- short-lived access tokens; refresh tokens are issued only when `offline_access` is granted;
- refresh-token rotation: every successful refresh consumes the old token and returns a new one; replay of the consumed token is rejected;
- refresh-token scope cannot be elevated during refresh;
- refresh tokens are process-bound; restarting BricsCAD invalidates old refresh tokens and requires ChatGPT to authorize again;
- token/client/code binding to the exact public MCP resource;
- signed opaque tokens with HMAC-SHA256 and constant-time verification;
- explicit local BricsCAD approval before a code is issued;
- strict duplicate/malformed form/query handling and bounded registration metadata.

## MCP tools and desktop permission boundary

The direct CAD/tool decision order is:

**direct CAD API → bounded native command workflow → BricsCAD-process UI fallback → explicit desktop tools only for required cross-application work**.

The desktop namespace includes read-only cursor/window observation plus explicit focus, mouse, keyboard, clipboard and screenshot tools. Desktop mutations still require `confirmMutation=true`; clipboard/screenshot reads still require `confirmSensitiveRead=true`.

In addition, desktop mutation and sensitive reads require **local desktop consent** enabled by the user from QS3D. This permission:

- is process-memory-only;
- resets on every BricsCAD start;
- cannot be remotely enabled through MCP;
- displays a topmost click-through blue border/banner while a guarded desktop call is active.

A physical **Esc twice within 1.2 seconds** disables desktop consent, advances the MCP emergency-stop epoch, hides the overlay and requests BricsCAD command cancellation. Desktop control cannot resume until the user locally enables it again.

Desktop window handles are restricted to visible top-level windows in the current interactive Windows session and revalidated before input. The remote MCP surface still exposes no arbitrary PowerShell/cmd/shell/process launch or arbitrary executable path.

## ChatGPT ↔ local status boundary

ChatGPT remains the conversation interface. QS3D does **not** scrape or automate ChatGPT's web conversation to mirror assistant prose.

QS3D instead records a bounded local operational timeline: onboarding steps, MCP action state, next step, errors, desktop-control state and backup/recovery events. Tool results and errors return to ChatGPT normally through MCP `tools/call`; `qs3d_status` and `cad_audit_tail` remain the remote state/audit surfaces.

## Backup and recovery

Both V25 and V26 start `McpProjectRecoveryService`.

- QS3D preserves an already shorter BricsCAD `SAVETIME`; if disabled or longer than five minutes it sets the interval to 5.
- `ISAVEBAK` is enabled when needed.
- When CAD is idle, QS3D can create bounded coherent copies of the saved DWG under `%LOCALAPPDATA%\QS3D\Backups`.
- Source length/write timestamp are compared before and after copy; an unstable intermediate copy is discarded.
- Retention is capped at 30 snapshots per drawing.
- Recovery always creates a new `Recovered` copy and never silently overwrites the original/active DWG.

The first-run MCP toast is non-blocking and rate-limited. It links to the Control Center and never requests credentials.

## Local qualification — exact candidate SHA

Hosted CI can prove source guards and compilation only. Before claiming `LOCAL_PASS`, test the exact candidate on real Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT:

1. Load the exact candidate DLL in V25 and V26 and confirm MCP health/running state.
2. Verify the four-tab Agent Center and first-run toast.
3. Run Agent Center MCP protocol check and read-only self-test.
4. Complete Cloudflare provider-browser login and a persistent Named Tunnel; confirm displayed public URL is HTTPS and ends in `/mcp`.
5. Verify protected-resource and authorization-server discovery.
6. In ChatGPT create QS3D custom MCP using only basic URL + OAuth; leave Advanced OAuth untouched.
7. Deny then approve the local OAuth authorization prompt.
8. Verify DCR, PKCE S256 and `tools/list`, including all `desktop_*` descriptors.
9. Call representative read-only CAD tools and desktop cursor/window observation.
10. With desktop permission OFF, verify desktop mutation and clipboard/screenshot reads fail closed.
11. Enable desktop permission locally and verify the blue overlay appears during guarded desktop calls.
12. Verify clipboard read/screenshot require `confirmSensitiveRead=true` and remain bounded.
13. On disposable content, verify focus/mouse/type/key/clipboard-write with `confirmMutation=true`.
14. While a desktop action is active, press physical Esc×2 and verify desktop consent + mutation epoch stop, CAD cancel, overlay removal and local re-enable requirement.
15. Verify no-refresh behavior without `offline_access`; then refresh-token issue/rotation/replay rejection with `offline_access`.
16. Restart BricsCAD and verify process-bound refresh credentials and local desktop consent are invalidated.
17. Verify Quick Tunnel URL churn invalidates stale resource binding; verify Named Tunnel reconnect/autostart keeps its stable hostname.
18. Verify BricsCAD autosave/BAK policy and create a stable versioned recovery copy; recover the latest snapshot to a new file and confirm original DWG is unchanged.
19. On a disposable drawing, run one confirmed CAD mutation, inspect audit, save/reopen and shut down cleanly.
20. Verify the legacy static bearer remains limited to the documented engineering compatibility path.

Do not record or commit access/refresh tokens, legacy bearer, Cloudflare credentials, private paths, customer/private DWGs, clipboard contents, typed secrets, proprietary binaries or unsanitized screenshots.

## Source verification

Current source invariants include:

```text
python scripts/preflight-mcp-oauth.py
python scripts/preflight-mcp-tools-list-json.py
python scripts/preflight-mcp-desktop-function-calling.py
python scripts/test-mcp-guided-onboarding-control-recovery-source.py
```

The broader shared MCP guards continue to cover embedded transport, full-agent routing, production hardening, session handoff and loopback behavior.

## Qualification boundary

`SOURCE_IMPLEMENTED` means the MCP source/hosted contracts exist on the candidate branch. It is not licensed runtime proof.

`LOCAL_PASS` requires the exact tested SHA to pass the V25/V26 + Cloudflare + ChatGPT + Windows-desktop/recovery matrix with sanitized evidence in `LOCAL-024`.

`MERGED_MAIN` is a separate source-integration state. It requires the repository's protected PR/main policy and current required checks. Source may land while `LOCAL-024` remains `PENDING_LOCAL`; that never converts hosted evidence into licensed runtime proof.
