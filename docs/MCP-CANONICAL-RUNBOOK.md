# QS3D MCP — CANONICAL START HERE

**Status:** SOURCE TRACKED / RUNTIME `PENDING_LOCAL`  
**Parent architecture issue:** `#4352`  
**OAuth integration issue:** `#4584`  
**Desktop/guided-control extension:** `#4629`  
**Background host-control extension:** `#4765`  
**Transport-provider extension:** `#4916`  
**Merged OAuth PR:** `#4597`  
**Merged OAuth source head:** `3f4cc36448b81dba15da741138807fe59793aa60`  
**End-user model:** one QS3D install, embedded loopback MCP, provider-aware tunnel onboarding, no PowerShell/CMD/Node/second MCP repository.

> **MCP AGENTS MUST START HERE.** The architecture established by #4352 remains the parent product contract. OAuth/DCR onboarding added by #4584/#4597 remains the canonical authentication path for public-URL transports. #4629 extends the embedded runtime with bounded Windows desktop tools, local desktop consent, visible emergency controls, guided onboarding and versioned recovery. #4765 adds the background BricsCAD-host control layer. #4916 adds a transport-provider layer: OpenAI Secure MCP Tunnel for a private/no-domain path, Cloudflare Named Tunnel for a stable public URL + OAuth path, and Cloudflare Quick Tunnel for test only. There is one QS3D plugin/runtime, no `desktop_macro` alias and no arbitrary remote shell/process/script surface.

## 1. Canonical architecture

The embedded MCP stays local. Agent Center selects one external transport for ChatGPT reachability:

```text
                                      ┌─ OpenAI Secure MCP Tunnel
                                      │    outbound tunnel-client
ChatGPT / OpenAI products ────────────┤    no user-owned public MCP hostname
                                      │
                                      ├─ Cloudflare Named Tunnel
                                      │    stable https://<host>/mcp + OAuth/DCR
                                      │
                                      └─ Cloudflare Quick Tunnel
                                           rotating trycloudflare URL · test only

                                      ↓
                         http://127.0.0.1:<port>/mcp
                                      ↓
                           McpEmbeddedServerV2
                                      ↓
                           McpCadAgentRuntime
                         /                   \
          API/background host           explicit desktop fallback
```

There is **one shipping repository and one end-user installation**: `trinhtanphat/QS3D-BricsCAD`. The historical `QS3D-CAD-MCP` repository is reference/history only and must not return as a second runtime, Node service, clone or user setup requirement.

The active embedded server binds loopback only. Port `8765` remains preferred, with the current bounded fallback-port behavior when the preferred port is occupied. OpenAI Secure MCP Tunnel reaches that exact resolved local endpoint through the official `tunnel-client`; Cloudflare transports expose the local endpoint through their existing public HTTPS path.

## 2. Active source

Inspect these first:

1. `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` — active loopback HTTP/MCP transport, OAuth routing, engineering-bearer compatibility, sessions, schemas/results and admission limits.
2. `src/QS3D.BricsCAD.V25/McpOAuthAuthorizationServer.cs` — public-URL OAuth discovery, DCR, authorization code, PKCE S256, access/refresh credentials, rotation/replay protection and resource binding.
3. `src/QS3D.BricsCAD.V25/McpOAuthConsent.cs` — bounded local BricsCAD approve/deny prompt for OAuth authorization.
4. `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs` — direct CAD operations, bounded command dispatch, mutation epoch/emergency recovery and audit.
5. `src/QS3D.BricsCAD.V25/McpBackgroundHostRuntime.cs` — same-process BricsCAD UI text observation and bounded background controls.
6. `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs` — explicit bounded `desktop_*` primitives plus the bounded single-target `desktop_sequence`.
7. `src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs` — process-memory-only local desktop consent with session-persistent auto-renew after local Resume, pause/resume, blue overlay and physical Esc×2 emergency stop.
8. `src/QS3D.BricsCAD.V25/McpDiagnosticHub.cs` — bounded/redacted MCP + QS3D + BricsCAD diagnostic bridge.
9. `src/QS3D.BricsCAD.V25/McpDirectDiagnosticsThemeRuntime.cs` — direct diagnostics tail/since/snapshot/wait and theme tools.
10. `src/QS3D.BricsCAD.V25/McpTopLevelJson.cs` — security-sensitive top-level JSON parsing and mutation confirmation.
11. `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs` — guided four-tab `QS3DMCPAGENTCENTER` UX and transport selector.
12. `src/QS3D.BricsCAD.V25/McpOpenAiSecureTunnel.cs` — transport coordinator plus OpenAI `tunnel-client` supervisor, binary trust verification, memory-only Runtime API key handling, readiness probing and bounded diagnostics.
13. `src/QS3D.BricsCAD.V25/McpLocalAgentClient.cs` — local loopback protocol/self-test/emergency-control client.
14. `src/QS3D.BricsCAD.V25/McpAgentExperience.cs` — bounded local onboarding/action/error timeline; operational metadata only.
15. `src/QS3D.BricsCAD.V25/McpProjectRecoveryService.cs` — autosave/BAK policy and bounded versioned DWG recovery-to-copy.
16. `src/QS3D.BricsCAD.V25/McpFirstRunExperience.cs` — rate-limited first-run onboarding toast.
17. `src/QS3D.BricsCAD.V25/McpCloudflaredBootstrapper.cs` — trusted cloudflared discovery/reuse, bounded download, Authenticode verification, progress/cancel and atomic managed install.
18. `src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs` — provider-browser login, persistent Named Tunnel/DNS/autostart.
19. `src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs` — advanced token / Quick Tunnel fallback with installer recovery controls.
20. `src/QS3D.BricsCAD.V25/McpPublicEndpointResolver.cs` — validated public HTTPS `/mcp` source for Cloudflare/public-URL transports only.

`src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs` is legacy historical source and is not the active compiled transport.

## 3. Normal end-user transport flow

Open BricsCAD + QS3D, then open **TOOL > MCP (AI) > Agent Center** (`QS3DMCPAGENTCENTER`). Confirm embedded MCP is running and choose one transport in **Kết nối**.

### 3.1 OpenAI Secure MCP Tunnel — preferred no-domain path on clean installs

Use this when the user wants ChatGPT/OpenAI products to reach a local/private QS3D MCP without publishing a user-owned public MCP hostname.

1. In OpenAI Platform, open **Tunnels** and create/reuse a tunnel. The runtime principal needs Tunnels **Read + Use**; operators that create/edit tunnels additionally need **Manage**.
2. Obtain the official `tunnel-client` from OpenAI Platform Tunnels or the official `openai/tunnel-client` release.
3. In Agent Center choose **OpenAI Secure Tunnel**, select `tunnel-client.exe`, and enter the `tunnel_...` ID. QS3D verifies `--version` and verifies the executable again immediately before launch.
4. Enter a **Runtime API key** for the current session, or supply `CONTROL_PLANE_API_KEY`/`OPENAI_API_KEY` in the Windows environment. QS3D does **not** persist the Runtime API key.
5. Start the Secure Tunnel and wait until the local `tunnel-client` readiness endpoint reports **READY**.
6. In ChatGPT connector/app settings choose **Connection = Tunnel** and select/paste the same Tunnel ID. This path does not require the QS3D public URL/OAuth screen.
7. Keep `tunnel-client` running while ChatGPT needs the MCP.

Tunnel-client trust is fail-closed. If the Windows executable has valid Authenticode, the signer must identify OpenAI. If an official OpenAI release is intentionally unsigned, pin the official release SHA-256 in the Windows environment before selecting it:

```text
QS3D_OPENAI_TUNNEL_CLIENT_SHA256=<64-hex-official-release-sha256>
```

QS3D computes SHA-256 itself and requires an exact match. A locally invented checksum file or filename is not treated as release provenance. Changing/replacing the executable after selection does not bypass the policy because trust is checked again immediately before every launch.

QS3D writes a non-secret `tunnel-client.yaml` that points to the exact local `McpEmbeddedServer.Endpoint`. The config stores only environment references for secrets. The child process receives:

- `CONTROL_PLANE_API_KEY` — Runtime API key;
- `QS3D_TUNNEL_MCP_AUTH` — `Bearer <local QS3D bearer>`;
- YAML `Authorization: env:QS3D_TUNNEL_MCP_AUTH` for MCP runtime/discovery requests;
- loopback-only health/admin binding.

The Runtime API key and local bearer are not written to QS3D config, status timeline or audit. On process restart, a user-entered Runtime API key is gone; automatic restart is possible only when a suitable runtime key already exists in the process environment.

The supervisor captures a bounded tail of `tunnel-client` stdout/stderr for troubleshooting, records the process exit code and exposes a sanitized diagnostic bundle. API-key-like values and Authorization values are redacted before being retained. Diagnostic capture is bounded and must never be promoted into a secret-bearing persistent log.

### 3.2 Cloudflare Named Tunnel — stable public URL + OAuth/DCR

Use this when a stable user-controlled public HTTPS MCP hostname is desired.

1. Install/update `cloudflared` from Agent Center if required.
2. Click **Đăng nhập Cloudflare + tạo Named Tunnel**. Credentials remain in Cloudflare's provider-owned browser flow.
3. Create/reuse a stable hostname and start the saved Named Tunnel.
4. Copy the canonical public HTTPS URL ending in `/mcp`.
5. In ChatGPT add the custom MCP/app using that URL and **OAuth**. Leave Advanced OAuth untouched in the normal flow; QS3D exposes discovery + Dynamic Client Registration.
6. Approve the expected local BricsCAD OAuth consent prompt.
7. ChatGPT completes authorization-code + PKCE S256, obtains an access token and scans/calls MCP tools.

Existing users with a saved Named Tunnel keep that provider selection on upgrade unless they explicitly switch in Agent Center.

### 3.3 Cloudflare Quick Tunnel — test only

Quick Tunnel remains a diagnostic fallback. It can be used without a domain/login, but the `trycloudflare.com` hostname can rotate. Because public-URL OAuth credentials are resource-bound, a changed URL requires a fresh ChatGPT connection. Quick Tunnel is never auto-started and must not be described as a stable production transport.

### 3.4 Cloudflare installer, progress, cancel and recovery

`McpCloudflaredBootstrapper` is single-flight. A second click while the download/Authenticode check is already running is **busy/informational**, not a failed installation. While busy, the install action is disabled, the UI shows download progress, and a **Cancel / Hủy cài Cloudflare Tunnel** action is available. Agent Center must not create a red “Cài Cloudflare thất bại” toast merely because another install is already running.

Before downloading a duplicate, QS3D searches trusted existing installations, including the WinGet link, Program Files, the explicit `QS3D_CLOUDFLARED_PATH` and PATH. A candidate is accepted only after Windows Authenticode verification with a Cloudflare signer. A trusted existing WinGet/system binary is reused directly.

The managed downloader is bounded: each attempt has an outer limit of **120 seconds**, bounded read/write timeouts, a maximum of three attempts, progress reporting and cancellation. Downloads go to a temporary path, are Authenticode-verified, then replace the managed binary atomically with rollback protection. Cancellation or failure cleans the temporary file and does not intentionally destroy the previous verified binary.

If the managed download is blocked or repeatedly times out, the canonical manual recovery is:

```powershell
winget install --id Cloudflare.cloudflared
cloudflared --version
```

Then return to Agent Center and **Refresh**. QS3D should discover the WinGet binary, verify the Cloudflare Authenticode signer and reuse it instead of downloading a second copy.

## 4. Authentication contracts

### Public-URL Cloudflare path

The embedded authorization server exposes:

```text
GET  /.well-known/oauth-protected-resource
GET  /.well-known/oauth-protected-resource/mcp
GET  /.well-known/oauth-authorization-server
POST /oauth/register
GET  /oauth/authorize
POST /oauth/token
```

Required invariants include public-client DCR, exact ChatGPT callback allowlisting, PKCE `S256`, one-use short-lived authorization codes, `qs3d:mcp`, optional `offline_access`, short-lived access tokens, refresh rotation/replay rejection, exact public-resource/client binding and process-bound refresh credentials.

### OpenAI Secure Tunnel path

Secure Tunnel does not require a QS3D public HTTPS resource. Transport identity/authorization is handled by the OpenAI tunnel control plane and workspace permissions. The local embedded QS3D MCP remains bearer-protected against unrelated local callers; the supervised local `tunnel-client` receives that bearer through a child environment reference and forwards it only to the loopback MCP target.

Do not persist the OpenAI Runtime API key or local QS3D bearer. Do not put either secret on the process command line. Do not write them into YAML, timeline, logs, screenshots or committed evidence.

## 5. MCP transport, background-host boundary, desktop fallback and safety

`/mcp` accepts a validated OAuth access token for the current public resource or the existing static bearer path used by bounded local/engineering integrations such as the supervised Secure Tunnel target. The active server remains loopback-only and keeps the existing request/session/admission defenses.

Transport choice does **not** broaden the CAD/desktop authority model. The direct decision order remains:

**direct CAD API → bounded native command workflow → same-process background host control → explicit foreground desktop fallback only when genuinely required**.

The current safety invariants remain authoritative:

- exact `application/json` admission for MCP POST;
- bounded headers/body/sessions/concurrent clients;
- duplicate security-sensitive-header rejection;
- Streamable-HTTP Origin/DNS-rebinding defense;
- negotiated MCP protocol/session lifecycle;
- `tools/list`, `tools/call`, ping, notification and session DELETE behavior;
- top-level `confirmMutation=true` for ordinary mutations;
- local desktop consent for guarded foreground input/sensitive reads;
- process-start `background_only` interaction policy;
- no arbitrary PowerShell, `cmd.exe`, shell/process launch, arbitrary executable path or unrestricted scripting surface exposed through MCP.

The local Agent Center may launch only the user-selected official `tunnel-client` path under the dedicated Secure Tunnel manager; that supervisor is not an MCP tool and does not create a generic remote process-launch surface.

### Background BricsCAD host controls — default path

#4765 establishes `background_only` as the process-start default. Same-process controls remain restricted to validated current BricsCAD HWNDs and bounded Button/Edit/RichEdit operations. `bricscad_ui_text_snapshot` remains bounded and its returned text is not persisted into MCP diagnostics.

While `background_only` is active, global mutation tools `desktop_window_focus`, `desktop_mouse_move`, `desktop_mouse_click`, `desktop_mouse_scroll`, `desktop_mouse_drag`, `desktop_type`, `desktop_key`, `desktop_clipboard_write` and `desktop_sequence` fail before global injection.

### Desktop-wide tools — explicit fallback

The explicit desktop namespace keeps the 14 Approach-A primitives plus the bounded single-target `desktop_sequence`. Desktop mutation requires `foreground_fallback`, `confirmMutation=true` and local desktop consent. Clipboard/screenshot reads require `confirmSensitiveRead=true` and local consent. Consent is process-memory-only; after an explicit local Resume it remains ON with session-persistent auto-renew for the current BricsCAD process and has no idle-expiry countdown. It still cannot be enabled/resumed by MCP, and Pause desktop, Emergency Stop, physical Esc×2 or BricsCAD/QS3D shutdown revoke it immediately.

Physical **Esc ×2 within 1.2 seconds** revokes desktop consent, advances the emergency-stop epoch, hides the overlay and requests CAD command cancellation. Sequence/drag/input paths must fail closed when the target, consent generation or mutation epoch changes.

## 6. Agent decision model

Representative inspection tools include `connector_info`, `qs3d_status`, `cad_active_document`, `cad_selection`, `cad_database_snapshot`, `cad_entity_inspect`, `cad_view_state`, `cad_wait_idle`, `cad_sysvar`, `cad_command_catalog`, `cad_audit_tail`, `diagnostics_*`, `bricscad_ui_text_snapshot`, desktop observation and bounded `desktop_wait_for_window`.

Representative direct mutations include line/circle/arc/polyline/text/MText creation, `cad_entity_transform`, delete/layer operations and `qs3d_run_command`. Complex native workflows use `cad_command_sequence`. Cross-application/custom UI workflows use explicit desktop tools only after local consent and foreground-fallback selection. `desktop_sequence` remains one exact target window, <=12 steps, <=30 seconds and fail-fast.

A successful tool call is not proof the drawing or external application state is correct. Re-inspect state before consequential follow-up actions and before final save/plot.

## 7. Guided local status, diagnostics and recovery

QS3D does not scrape the ChatGPT web conversation. Agent Center mirrors bounded local operational metadata only.

Transport status is provider-aware:

- OpenAI: process `RUNNING` versus health `READY`, selected Tunnel ID, binary-trust summary, last process exit/error and user registration acknowledgement; readiness alone is not claimed as proof of a ChatGPT `tools/call`;
- Cloudflare: tunnel/public URL, user registration acknowledgement and actual recent authenticated OAuth MCP traffic remain distinct states;
- Cloudflare installer: trusted source/path plus bounded progress/cancel status are separate from tunnel READY state;
- Quick Tunnel is explicitly marked test-only.

Both V25 and V26 start embedded MCP and then auto-start only the preferred persistent transport. On clean installs the preference is OpenAI Secure Tunnel; existing saved Named Tunnel users retain Named Tunnel as the inferred preference until they explicitly switch. Quick Tunnel never auto-starts. Host teardown stops the selected/supervised tunnel processes before the embedded MCP stops.

Recovery remains two-layered: native BricsCAD autosave/BAK plus bounded versioned QS3D snapshots under `%LOCALAPPDATA%\QS3D\Backups`. Restore always writes a new `Recovered` copy and never silently overwrites the active/original DWG.

## 8. Source verification

For the current MCP surface, run/inspect at minimum:

- `scripts/preflight-mcp-transport-providers.py`
- `scripts/preflight-mcp-oauth.py`
- `scripts/preflight-mcp-tools-list-json.py`
- `scripts/preflight-embedded-mcp.py`
- `scripts/preflight-mcp-full-agent.py`
- `scripts/preflight-mcp-agent-center-uiux.py`
- `scripts/preflight-mcp-desktop-function-calling.py`
- `scripts/preflight-mcp-direct-diagnostics-theme.py`
- `scripts/preflight-mcp-background-host-control.py`
- `scripts/test-mcp-guided-onboarding-control-recovery-source.py`
- `scripts/preflight-mcp-production-hardening.py`
- `scripts/preflight-mcp-session-handoff.py`
- `scripts/preflight-mcp-loopback-readonly.py`
- deterministic Core smoke and trusted V25/V26 compilation where selected by repository CI.

`preflight-mcp-transport-providers.py` must keep the three-provider selector, memory-only Runtime API key/local-bearer boundary, dynamic loopback endpoint, OpenAI readiness + binary-trust + bounded stdout/stderr diagnostic contracts, V25/V26 startup/teardown wiring, trusted cloudflared reuse, WinGet recovery, bounded timeout/retry/progress/cancel, and Cloudflare busy-state UX source-guarded.

Hosted/source evidence does not replace real OpenAI/Cloudflare/ChatGPT/licensed-BricsCAD runtime qualification.

## 9. LOCAL-024 — required runtime qualification

Hosted CI is not licensed BricsCAD/OpenAI Tunnel/Cloudflare/ChatGPT/Windows-desktop runtime evidence. Runtime remains **`PENDING_LOCAL` / `LOCAL_ONLY`** until a clean exact intended merged/release descendant is exercised on real Windows with licensed BricsCAD V25/V26.

The local matrix must cover at least:

1. exact DLL load in V25/V26 and local MCP health;
2. four-tab Agent Center with all three transport choices;
3. clean-install default OpenAI provider and upgrade preservation of an existing Named Tunnel preference;
4. official Windows `tunnel-client` selection/version check plus Authenticode signer validation or official SHA-256 pin via `QS3D_OPENAI_TUNNEL_CLIENT_SHA256` for an unsigned release;
5. replacement/tampering after tunnel-client selection is rejected by the pre-launch trust re-check;
6. valid/invalid `tunnel_...` validation;
7. Runtime API key used only in memory/child environment and absent from QS3D config/timeline/audit;
8. local QS3D bearer used through the environment reference and absent from YAML/logs;
9. Secure Tunnel starts against the exact local fallback/preferred MCP port and reaches `/readyz`;
10. ChatGPT **Connection = Tunnel** using the same Tunnel ID, with representative `tools/list` and read-only MCP call;
11. restart behavior: user-entered Runtime API key is not persisted; environment-provided runtime key may auto-start the preferred Secure Tunnel;
12. Secure Tunnel process stdout/stderr capture is bounded/sanitized; forced failure records useful exit/error diagnostics without exposing Runtime API key/local bearer;
13. Secure Tunnel process exit/restart/host-teardown behavior;
14. Cloudflare installer disables repeat-install action, visibly advances progress and permits Cancel without reporting a synthetic red busy failure;
15. managed cloudflared download timeout/retry path is bounded at 120 seconds per attempt and cleanup preserves the prior verified binary;
16. a trusted WinGet `cloudflared` installation is discovered, Authenticode-verified and reused without downloading a duplicate managed binary;
17. an untrusted or non-Cloudflare-signed `cloudflared.exe` is rejected;
18. Cloudflare Named Tunnel login/stable hostname/public `/mcp` + OAuth/DCR path;
19. Cloudflare authorization deny/approve, PKCE S256 and representative tool scan;
20. Quick Tunnel test-only URL churn and required reconnect;
21. provider switching does not cause a non-selected running transport to be reported as selected READY;
22. public OAuth code/token/refresh replay/resource-binding invariants on the Cloudflare path;
23. `background_only` startup and same-process background controls;
24. local desktop-consent OFF rejection, local Resume/Pause, AUTO-RENEW remaining ON beyond 10 minutes of idle time, and blue overlay while guarded actions run;
25. bounded screenshot/clipboard/mouse/drag/type/key behavior only under existing consent/confirmation rules;
26. bounded `desktop_sequence` success/rejection/cancellation contracts;
27. physical Esc×2 emergency stop and CAD cancel;
28. versioned backup/recovery-to-new-copy;
29. one confirmed disposable-DWG mutation plus audit/save/reopen;
30. clean V25/V26 process shutdown with tunnel processes stopped.

Never commit Runtime API keys, OpenAI admin keys, access/refresh tokens, static bearer secrets, Cloudflare credentials, private paths/DWGs, clipboard contents, typed secrets, proprietary BricsCAD binaries or unsanitized screenshots.

## 10. Continuation rules

For future MCP work:

1. read repository governance first, then this file;
2. treat #4352 as parent architecture, #4584/#4597 as public-URL OAuth onboarding, #4629 as desktop/guided-control, #4765 as background host control, and #4916 as transport-provider scope;
3. resolve current `main` and current source before changing anything;
4. edit active V2/runtime/provider source, not the legacy monolith;
5. preserve one repo/runtime and loopback-only embedded MCP;
6. preserve all three intentional provider semantics: OpenAI Secure Tunnel = private/no-domain, Cloudflare Named = stable public URL + OAuth, Cloudflare Quick = test only;
7. never persist OpenAI Runtime API keys or the local bearer; use environment references for the supervised tunnel-client;
8. preserve fail-closed binary trust: Cloudflare Authenticode signer verification and OpenAI Authenticode-or-official-SHA-256 verification before launch;
9. preserve bounded cloudflared install timeout/retry/progress/cancel and trusted WinGet/system reuse;
10. preserve bounded/sanitized OpenAI stdout/stderr diagnostics; do not put secrets into copied diagnostics;
11. do not silently convert Secure Tunnel readiness into proof of actual ChatGPT tool traffic;
12. preserve `background_only` as the process-start default and keep global desktop input locally consented;
13. preserve Approach A explicit primitives and Approach B bounded single-target `desktop_sequence`; do not add `desktop_macro` or arbitrary scripting;
14. do not expose generic shell/process execution through MCP merely to manage transports;
15. update this runbook and LOCAL-024 whenever provider/runtime behavior changes;
16. never promote hosted CI to `LOCAL_PASS`.

## 11. Definition of done

For #4916, source completion requires the provider-aware Agent Center, OpenAI tunnel-client supervisor with trust + bounded diagnostics, Cloudflare trusted binary reuse plus bounded timeout/progress/Cancel/WinGet recovery, V25/V26 lifecycle wiring, canonical source guard and docs to pass fresh protected-PR CI on current `main`.

Full runtime completion is separate. The exact intended merged/release descendant must pass LOCAL-024 with real Windows + licensed BricsCAD V25/V26 + OpenAI Secure MCP Tunnel + Cloudflare fallback + ChatGPT. Until that evidence exists, the runtime state remains `PENDING_LOCAL` and no hosted check may be cited as `LOCAL_PASS`.