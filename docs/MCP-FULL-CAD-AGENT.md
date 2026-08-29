# QS3D full CAD agent over MCP

Status: SOURCE TRACKED / `PENDING_LOCAL`  
Parent architecture issue: #4352  
OAuth integration issue: #4584  
Desktop/guided-control extension: #4629  
Merged OAuth PR: #4597  
Merged OAuth source: `d3e6e4d6e9f423efbd2d236d600110a6a2ced1f5`

QS3D embeds the MCP resource server, OAuth authorization server, Cloudflare onboarding, guided Agent Center, CAD runtime, bounded desktop automation, recovery and audit/status surfaces directly in the BricsCAD plugin. End users do not need a second MCP repository, Node runtime, PowerShell setup, or command-shell workflow.

The active transport is `McpEmbeddedServerV2.cs`; the legacy monolithic `McpEmbeddedServer.cs` is historical source and is excluded from active V25/V26 compilation. `McpCadAgentRuntime.cs` owns CAD database/editor operations, bounded native command dispatch, BricsCAD-process-only UI input, emergency recovery and the canonical mutation/audit gate. `McpDesktopAutomationRuntime.cs` adds an explicit `desktop_*` function-calling surface for the current interactive Windows session, guarded by `McpDesktopControlSession` local consent, without adding a remote shell/process-execution surface.

## End-user path — no terminal, no copied bearer

The canonical UI entry is `QS3DMCPAGENTCENTER` under **TOOL > MCP (AI)**. The Control Center has **Kết nối**, **Agent**, **Backup & khôi phục** and **Nâng cao** tabs.

1. Open BricsCAD with QS3D loaded and open MCP Agent Center.
2. In **Kết nối**, start/confirm the embedded MCP.
3. Use **Cài / cập nhật Cloudflare Tunnel** if needed. `McpCloudflaredBootstrapper` installs/updates the official Windows `cloudflared` binary with bounded download checks, Authenticode trust and Cloudflare signer verification.
4. Use **Đăng nhập Cloudflare**. Credentials are entered only on Cloudflare's provider-owned browser page; QS3D does not request or store the Cloudflare password.
5. Create/reuse the persistent `qs3d-bricscad` Named Tunnel and DNS route to `http://127.0.0.1:8765`.
6. Copy the validated public HTTPS `/mcp` URL from `McpPublicEndpointResolver` and click **Mở ChatGPT**. ChatGPT identity remains owned by the user's normal system browser; QS3D does not capture a ChatGPT password or scrape browser cookies.
7. In ChatGPT's basic custom MCP/app screen, paste only the URL and choose **OAuth**.
8. Leave Advanced OAuth untouched in the normal flow. QS3D advertises discovery and Dynamic Client Registration.
9. Approve the local BricsCAD OAuth prompt only for a connection you initiated.
10. ChatGPT completes authorization-code + PKCE S256, then scans and calls tools through `/mcp`.
11. Back in Agent Center, acknowledge that the MCP was added and run the protocol/read-only self-test.
12. Leave desktop-wide control OFF unless a workflow genuinely needs to cross application boundaries. Enable it locally from **Agent** only when required.

Quick Tunnel remains `Quick Tunnel · test only`. Its hostname can change, so resource-bound OAuth clients/tokens must be reconnected after the URL changes. A stable Named Tunnel is the production default.

The old **Copy Bearer Token / Copy URL + Authorization** helpers remain under **Nâng cao** for local/backward-compatible engineering use. Copied static bearer configuration is not the normal ChatGPT onboarding path.

## OAuth and MCP transport

The active service listens only on `127.0.0.1:8765` and exposes:

```text
GET  /healthz
GET  /.well-known/oauth-protected-resource
GET  /.well-known/oauth-protected-resource/mcp
GET  /.well-known/oauth-authorization-server
POST /oauth/register
GET  /oauth/authorize
POST /oauth/token
POST /mcp
DELETE /mcp
OPTIONS /mcp
```

OAuth contract:

- public clients use `token_endpoint_auth_method=none`;
- DCR accepts only the bounded ChatGPT connector callback family `https://chatgpt.com/connector/oauth/<id>`;
- authorization code uses PKCE S256;
- authorization codes are one-use and replay-protected;
- required MCP permission is `qs3d:mcp`;
- `offline_access` is optional and belongs to authorization-server scope negotiation only;
- protected-resource metadata and the `WWW-Authenticate` challenge expose only `qs3d:mcp`;
- refresh tokens are issued only when `offline_access` is granted;
- refresh tokens rotate; the old refresh token is consumed and replay is rejected;
- refresh cannot elevate the original grant;
- access/refresh credentials are bound to exact resource/client/scope;
- refresh credentials are process-bound, so restarting BricsCAD requires OAuth reauthorization;
- signed opaque credentials use HMAC-SHA256 and constant-time verification;
- explicit local BricsCAD approve/deny consent precedes authorization-code issuance.

`/mcp` accepts either a valid OAuth access token for the current public resource or the legacy static engineering bearer. The legacy bearer is backward compatibility, not the default ChatGPT UX.

MCP POST continues to require exact `application/json` media-type admission. The server retains bounded headers/body/sessions/concurrency, duplicate security-sensitive-header rejection, unsupported transfer-encoding rejection, Streamable-HTTP Origin/DNS-rebinding defense, negotiated MCP protocol/session checks and stale-session handling.

## Agent tool model

The production decision order is **direct CAD API first → bounded allowlisted native command workflow second → BricsCAD-process UI fallback where sufficient → explicit desktop tools only when a workflow genuinely crosses application boundaries**.

Read/observation tools include `connector_info`, `qs3d_status`, `cad_active_document`, `cad_selection`, `cad_database_snapshot`, `cad_entity_inspect`, `cad_view_state`, `cad_wait_idle`, `cad_sysvar`, `cad_command_catalog` and `cad_audit_tail`.

Direct mutation includes deterministic geometry/entity/layer operations such as line, circle, arc, polyline, DBText, MText, `cad_entity_transform`, delete and layer reassignment. Stable complex native workflows use `cad_command_sequence`; bounded QS3D commands use `qs3d_run_command`.

BricsCAD-only UI fallback remains `cad_ui_click`, `cad_ui_type`, and `cad_ui_key`. These tools are process-confined and remain preferable when the target is BricsCAD itself.

The desktop-wide namespace is intentionally separate and discoverable through the same MCP `tools/list` contract:

- observation: `desktop_cursor_position`, `desktop_window_list`, `desktop_foreground_window`;
- window/input mutation: `desktop_window_focus`, `desktop_mouse_move`, `desktop_mouse_click`, `desktop_mouse_scroll`, `desktop_type`, `desktop_key`;
- clipboard: `desktop_clipboard_read`, `desktop_clipboard_write`;
- visual observation: `desktop_screenshot`.

Desktop window handles are revalidated against visible top-level windows in the current interactive Windows session. Window listings expose bounded title/handle/bounds metadata, not process paths. Desktop typing and named-key input focus and revalidate the exact target window before injection. Mouse coordinates are bounded to the Windows virtual desktop. There is no arbitrary executable, shell or remote process-launch tool.

Clipboard text reads and screenshots require `confirmSensitiveRead=true`. Screenshots are bounded, downscaled when needed, PNG-encoded in memory and returned through MCP without writing image files to disk. Audit records store only operation metadata such as character counts, window handles and dimensions; clipboard text, typed text and screenshot pixels are not written to audit.

Recovery/control includes `cad_agent_stop`, `cad_cancel_command`, and confirmed `cad_agent_resume`. The same emergency-stop mutation epoch covers CAD and desktop mutation tools.

## Local desktop consent and visible control state

Network/tool confirmation is not enough for cross-application desktop mutation or sensitive reads.

- every ordinary desktop mutation still requires top-level `confirmMutation=true`;
- `desktop_clipboard_read` and `desktop_screenshot` still require `confirmSensitiveRead=true`;
- those mutation/sensitive-read tools additionally require **local desktop consent** enabled by the user in Agent Center;
- local desktop consent is process-memory-only, resets on every BricsCAD start and cannot be enabled by an MCP call;
- guarded calls display a click-through blue border/banner naming the active `desktop_*` tool;
- a physical **Esc ×2 within 1.2 seconds** revokes desktop consent, advances the mutation epoch, hides the overlay and requests CAD command cancellation;
- the user must locally enable desktop control again before cross-application automation can resume.

Read-only cursor/window observation remains available without enabling desktop mutation consent.

## Mutation and execution safety

Every ordinary CAD or desktop mutation requires top-level `confirmMutation=true`. Nested or duplicate confirmation must not authorize a mutation. Emergency stop/cancel remain usable without that confirmation so an operator can recover an active action.

Database mutation is marshalled through BricsCAD application context and document transactions/locks. Geometry and caller-controlled values are bounded/finite. CAD dispatch distinguishes cancelled-before-start from already-running/completion-uncertain work so clients do not blindly retry a mutation.

Emergency Stop advances/invalidate the mutation epoch. A queued mutation from an old epoch must remain stale even after Resume and must be resubmitted/reconfirmed. Desktop mutation dispatch reuses this same epoch gate and rechecks it immediately before injected input.

BricsCAD `cad_ui_*` input aborts when BricsCAD process/window ownership or foreground conditions cease to be valid. Desktop `desktop_*` input is broader by explicit design, but remains current-session/visible-window bounded and fail-closes when the exact focused target changes. PowerShell, `cmd.exe`, remote `Process.Start`, `CreateProcess`, arbitrary executable paths, legacy `mouse_event` injection and unrestricted native command execution remain outside the remote MCP boundary.

Mutations are locally audited with bounded/rotated sanitized records. Typed text, clipboard contents, screenshot pixels and secrets must not be persisted into audit details.

## Guided local status and recovery

ChatGPT stays the conversation UI. QS3D does not scrape ChatGPT Web or mirror arbitrary assistant prose. `McpAgentExperience` stores only bounded local operational metadata: onboarding state, current local action, next step, errors and recovery events. MCP tool results/errors return to ChatGPT normally through `tools/call`.

`McpProjectRecoveryService` adds a second safety layer beside BricsCAD native recovery:

- preserve a shorter existing `SAVETIME`, otherwise ensure it is enabled and no longer than five minutes;
- enable `ISAVEBAK=1` if necessary;
- while CAD is idle, snapshot only coherent on-disk saved DWGs;
- compare source length/write time before and after copy and discard unstable copies;
- retain at most 30 snapshots per drawing under `%LOCALAPPDATA%\QS3D\Backups`;
- restore only to a new file under `Recovered`, never silently overwrite the active/original DWG.

Both V25 and V26 start embedded MCP, persistent tunnel reconnect, recovery and first-run onboarding. Teardown revokes desktop consent before tunnel/MCP shutdown.

## Cloudflare ownership

`McpCloudflareAccountOnboarding` owns provider-browser login, Named Tunnel creation/reuse, DNS/config and reconnect. `McpCloudflareOnboarding` owns advanced token/Quick fallback. Modes must hand ownership to one another rather than intentionally running competing forwarders for the same loopback endpoint.

`McpPublicEndpointResolver` is the single canonical source for displayed/copied public URL. A public endpoint must be HTTPS, have no user-info/query/fragment, and canonicalize to `/mcp`. Loopback/private literal addresses are not public candidates.

## Full-drawing and cross-application workflow

```text
inspect document/model/view
-> plan layers/styles/geometry
-> direct API tools for deterministic changes
-> bounded native commands for hatch/dimension/block/xref/layout/viewport/plot/save
-> BricsCAD-only UI fallback when sufficient
-> explicit desktop tools only for required cross-application interaction and only after local consent
-> wait for CAD idle where required
-> re-inspect database/entity/view/external state
-> correct/undo when needed
-> save
-> plot/export
-> reopen/verify when requested
```

A successful tool call is not proof the drawing or external application state is correct; re-read state before consequential follow-up actions.

## Source verification

OAuth/DCR and MCP changes must keep these source checks coherent:

- `scripts/preflight-mcp-oauth.py`
- `scripts/preflight-mcp-tools-list-json.py`
- `scripts/preflight-embedded-mcp.py`
- `scripts/preflight-mcp-full-agent.py`
- `scripts/preflight-mcp-desktop-function-calling.py`
- `scripts/test-mcp-guided-onboarding-control-recovery-source.py`
- `scripts/preflight-mcp-production-hardening.py`
- `scripts/preflight-mcp-session-handoff.py`
- `scripts/preflight-mcp-loopback-readonly.py`
- `scripts/test-mcp-loopback-readonly.py`

Hosted/source validation must never be relabeled licensed runtime evidence.

## LOCAL_ONLY qualification — LOCAL-024

Hosted CI cannot prove a licensed desktop runtime. Overall runtime truth therefore remains **`PENDING_LOCAL`** until an exact intended merged/release descendant is tested on real Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT.

The matrix must prove:

1. exact V25/V26 plugin load and local `/healthz`;
2. four-tab Agent Center + first-run toast;
3. protocol check and read-only self-test;
4. Cloudflare provider-browser login + stable public HTTPS Named Tunnel;
5. protected-resource and authorization-server discovery;
6. ChatGPT basic-screen DCR with URL + OAuth only;
7. local OAuth deny and approve consent paths;
8. PKCE S256 exchange and tools discovery, including the `desktop_*` catalog;
9. representative read-only CAD calls plus desktop cursor/window observation;
10. desktop mutation/sensitive-read rejection while local desktop consent is OFF;
11. local enable + visible blue overlay;
12. `desktop_clipboard_read` and `desktop_screenshot` rejection without `confirmSensitiveRead=true`, then bounded success on disposable content;
13. confirmed disposable desktop move/click/type/key/clipboard-write behavior;
14. physical Esc×2 interruption, CAD cancel and local re-enable requirement;
15. authorization-code replay rejection;
16. no refresh token without `offline_access`;
17. refresh token with `offline_access`, rotation and replay rejection;
18. scope/resource mismatch rejection;
19. BricsCAD restart invalidating process-bound refresh and local desktop consent;
20. legacy static engineering-bearer compatibility;
21. Quick Tunnel URL change invalidating stale resource binding;
22. stable Named Tunnel restart/autostart/reconnect;
23. autosave/BAK + versioned backup + recovery-to-new-copy;
24. one confirmed disposable-DWG mutation, audit, `cad_agent_stop`/cancel/recovery;
25. save/reopen and clean process shutdown.

Do not commit access/refresh tokens, bearer secrets, Cloudflare credentials, private paths, private/customer DWGs, clipboard contents, typed secrets, proprietary BricsCAD binaries or unsanitized screenshots.

The exact runtime result belongs in the single LOCAL-024 item in `docs/LOCAL-AGENT-INBOX.md`. Source review/static preflight/cloud CI must never be relabeled `LOCAL_PASS`.
