# QS3D MCP — CANONICAL START HERE

**Status:** SOURCE TRACKED / RUNTIME `PENDING_LOCAL`  
**Parent architecture issue:** `#4352`  
**OAuth integration issue:** `#4584`  
**Desktop/guided-control extension:** `#4629`  
**Background host-control extension:** `#4765`  
**Merged OAuth PR:** `#4597`  
**Merged OAuth source head:** `3f4cc36448b81dba15da741138807fe59793aa60`  
**End-user model:** one QS3D install, click/browser-login setup, no PowerShell/CMD/Node/second MCP repository.

> **MCP AGENTS MUST START HERE.** The architecture established by #4352 remains the parent product contract. OAuth/DCR onboarding added by #4584/#4597 is the canonical ChatGPT authentication path. #4629 extends that same embedded runtime with bounded Windows desktop tools, local desktop consent, visible emergency controls, guided onboarding and versioned recovery. #4765 adds the canonical background BricsCAD-host control layer so ordinary MCP work does not need to steal the user's foreground window/cursor/keyboard. Foreground desktop input remains an explicit locally consented fallback. There is no `desktop_macro` alias and no arbitrary shell/process/script surface. Do not reconstruct the product from stale bearer-only docs or the historical second MCP repository.

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
       +-- McpBackgroundHostRuntime
            +-- background_only default interaction policy
            +-- same-process UI text snapshot
            +-- bounded Button/Edit/RichEdit window messages
       +-- McpDesktopAutomationRuntime
            +-- read-only desktop observation / screenshot
            +-- explicit desktop_* foreground-input fallback
            +-- bounded single-target desktop_sequence fallback
  -> local desktop-consent / idle expiry / blue overlay / Esc×2 stop boundary
```

There is **one shipping repository and one end-user installation**: `trinhtanphat/QS3D-BricsCAD`. The historical `QS3D-CAD-MCP` repository is reference/history only and must not return as a second runtime, Node service, clone, or user setup requirement.

The embedded server binds only loopback `127.0.0.1:8765` on the currently merged baseline. Public access is delegated to the configured HTTPS Cloudflare tunnel. Connectivity/occupied-port recovery work is owned by #4689 and must be reconciled into this canonical description when that carrier lands.

## 2. Active source

Inspect these first:

1. `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` — active loopback HTTP/MCP transport, OAuth routing, legacy engineering-bearer compatibility, MCP sessions, schemas/results and admission limits.
2. `src/QS3D.BricsCAD.V25/McpOAuthAuthorizationServer.cs` — protected-resource/authorization-server discovery, DCR, authorization code, PKCE S256, access/refresh credentials, optional `offline_access`, refresh rotation/replay protection and resource binding.
3. `src/QS3D.BricsCAD.V25/McpOAuthConsent.cs` — bounded local BricsCAD approve/deny prompt for OAuth authorization.
4. `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs` — direct CAD operations, bounded command dispatch, canonical mutation epoch/emergency recovery and audit.
5. `src/QS3D.BricsCAD.V25/McpBackgroundHostRuntime.cs` — same-process BricsCAD UI text observation, bounded background Button/Edit/RichEdit control and the process-scoped interaction policy that defaults to `background_only`.
6. `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs` — explicit bounded `desktop_*` observation/input/clipboard/screenshot primitives plus the bounded single-target `desktop_sequence`; global foreground input is blocked while the background-only policy is active.
7. `src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs` — non-persistent local desktop consent, idle expiry, pause/resume state, blue active-control overlay and physical Esc×2 emergency stop.
8. `src/QS3D.BricsCAD.V25/McpDiagnosticHub.cs` — bounded/redacted MCP + QS3D + BricsCAD diagnostic bridge.
9. `src/QS3D.BricsCAD.V25/McpDirectDiagnosticsThemeRuntime.cs` — direct bounded diagnostics tail/since/snapshot/wait and theme get/set tools.
10. `src/QS3D.BricsCAD.V25/McpTopLevelJson.cs` — security-sensitive top-level JSON parsing and mutation confirmation.
11. `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs` — guided four-tab `QS3DMCPAGENTCENTER` UX.
12. `src/QS3D.BricsCAD.V25/McpLocalAgentClient.cs` — local loopback protocol/self-test/emergency-control client.
13. `src/QS3D.BricsCAD.V25/McpAgentExperience.cs` — bounded local onboarding/action/error timeline; operational metadata only.
14. `src/QS3D.BricsCAD.V25/McpProjectRecoveryService.cs` — autosave/BAK policy and bounded versioned DWG recovery-to-copy.
15. `src/QS3D.BricsCAD.V25/McpFirstRunExperience.cs` — rate-limited first-run onboarding toast.
16. `src/QS3D.BricsCAD.V25/McpCloudflaredBootstrapper.cs` — managed verified `cloudflared` bootstrap.
17. `src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs` — provider-browser login, persistent Named Tunnel/DNS/autostart.
18. `src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs` — advanced token / Quick Tunnel fallback.
19. `src/QS3D.BricsCAD.V25/McpPublicEndpointResolver.cs` — one validated public HTTPS `/mcp` source of truth.

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
13. Leave BricsCAD interaction in the default `background_only` policy for normal CAD work. Enable **desktop control** locally in the **Agent** tab only when a workflow genuinely needs foreground/global input or another application. The desktop session is local-only, pausable/resumable only by the user and expires after 10 minutes of desktop-action inactivity.

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

## 5. MCP transport, background-host boundary, desktop fallback and safety

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

### Background BricsCAD host controls — default path

#4765 establishes `background_only` as the process-start default. The goal is not to hide a shell or secretly drive the desktop; it is to avoid global input entirely wherever the CAD host can be controlled by API, application-context command dispatch or bounded same-process window messages.

Direct tools:

- `bricscad_interaction_policy_get`;
- `bricscad_interaction_policy_set` (`background_only` or `foreground_fallback`);
- `bricscad_ui_text_snapshot` (`all`, `commandline`, `popup`), requiring `confirmSensitiveRead=true`;
- `bricscad_ui_invoke`, standard same-process `Button` only;
- `bricscad_ui_set_text`, standard same-process `Edit`/`RichEdit*` only.

Same-process control invariants:

- every HWND/control is revalidated against the current BricsCAD process;
- visible controls only;
- UI text is bounded per control, total response and item count;
- returned UI text is not persisted into the MCP audit stream; only scope/count metadata is audited;
- Button/Edit mutations use bounded `SendMessageTimeout`, never `SetForegroundWindow`, `SetCursorPos` or `SendInput`;
- ordinary mutation confirmation and mutation-epoch/emergency-stop checks remain authoritative;
- enabling `foreground_fallback` requires current local desktop consent in addition to `confirmMutation=true`;
- the policy is process-memory-only and resets to `background_only` on restart.

While `background_only` is active, global mutation tools `desktop_window_focus`, `desktop_mouse_move`, `desktop_mouse_click`, `desktop_mouse_scroll`, `desktop_mouse_drag`, `desktop_type`, `desktop_key`, `desktop_clipboard_write` and `desktop_sequence` fail before global injection. Read-only desktop observation remains available under its existing consent/confirmation boundaries.

`bricscad_ui_text_snapshot` is the on-demand source for command-line/status/popup text when Win32 exposes ordinary HWND text. It is not OCR. Commands themselves should continue to use direct CAD/QS3D tools instead of typing into the command line. Custom-rendered/WPF surfaces may require the popup diagnostics observer or screenshot path.

### Desktop-wide tools — explicit fallback

The explicit desktop namespace for #4629 includes 14 Approach-A primitives:

- read-only observation/wait: `desktop_cursor_position`, `desktop_window_list`, `desktop_foreground_window`, `desktop_wait_for_window`;
- mutation/input: `desktop_window_focus`, `desktop_mouse_move`, `desktop_mouse_click`, `desktop_mouse_scroll`, `desktop_mouse_drag`, `desktop_type`, `desktop_key`, `desktop_clipboard_write`;
- sensitive reads: `desktop_clipboard_read`, `desktop_screenshot` with optional bounded crop.

Desktop mutation requires `foreground_fallback`, `confirmMutation=true` **and** local desktop consent. Clipboard/screenshot reads require `confirmSensitiveRead=true` **and** local desktop consent. Local desktop consent is process-memory-only, resets at every BricsCAD start, expires after 10 minutes of desktop-action inactivity and cannot be enabled/resumed by an MCP call.

A guarded desktop action displays a click-through blue border/banner. Physical **Esc ×2 within 1.2 seconds** revokes consent, advances the MCP emergency-stop epoch, hides the overlay and requests BricsCAD command cancellation. Local Pause uses the same fail-closed stop boundary. Resume is an explicit local user action that creates a new consent generation only after the emergency keyboard hook is available.

`desktop_wait_for_window` is read-only and bounded to a maximum 15-second timeout. It observes visible current-session top-level window metadata and never focuses/clicks automatically.

`desktop_mouse_drag` requires an exact visible `windowHandle`, start/end points inside current target bounds, foreground focus/revalidation immediately before injection and repeated stop/target checks during the drag. It must fail closed if the target, consent generation or mutation epoch changes.

`desktop_screenshot scope=window` renders the validated target off-screen through bounded `PrintWindow(PW_RENDERFULLCONTENT)` before crop/scale/PNG encoding; it does not focus the target or sample whatever unrelated application currently covers the target rectangle. `scope=screen` keeps the bounded virtual-desktop `BitBlt` behavior. `PrintWindow` is best-effort: minimized/GPU/custom-rendered windows can fail or render incomplete pixels, and the implementation must fail rather than silently stealing focus or substituting unrelated desktop pixels.

Desktop targeting is limited to visible top-level windows in the current interactive Windows session. Handles are revalidated before input. Text, click counts, wheel deltas, drag duration, window lists, wait timeout, clipboard payloads and screenshots are bounded. Typed text, clipboard contents and screenshot pixels must not be persisted into audit records.

Local desktop events may contain a bounded `actionId`, timestamps/duration and success/failure/cancelled state. They must not contain typed/clipboard/screenshot/token/DWG content.

### Approach B — selected bounded `desktop_sequence`

Approach B adds the 15th canonical desktop tool, **`desktop_sequence`**. It batches only a narrow allowlist of the existing desktop primitives and remains inside the same `McpCadAgentRuntime.Mutation(...)`, local-consent, blue-overlay, audit and Esc×2 boundaries. It additionally requires the process-scoped interaction policy to be `foreground_fallback`.

Sequence invariants:

- one exact visible current-session `windowHandle` per sequence; no target switching inside the sequence;
- maximum 12 steps and maximum 30 seconds total runtime;
- optional per-step delay is 0–2000 ms and is split into short cancellation-check slices;
- fail-fast on the first error; there is no `continueOnError`;
- no recursion/nested `desktop_sequence` and no `desktop_macro` alias;
- no atomic rollback: completed UI steps remain completed when a later step fails, and the result/error reports completed-step count/duration;
- `stepsJson` is a bounded string containing a JSON array of flat `{tool, arguments, delayAfterMs}` records; each `arguments` value is a bounded flat JSON-object string;
- step arguments may not provide `windowHandle`, `confirmMutation` or `confirmSensitiveRead`; the executor owns those values;
- allowed sequence primitives are target focus, mouse move/click/scroll/drag, Unicode type, named key/hotkey, clipboard write, wait-for-the-same-target and target-window screenshot;
- `desktop_clipboard_read`, observation/list tools, CAD/QS3D/plugin dispatch, filesystem, shell/process/script/eval and nested sequence are not allowed inside the sequence;
- sequence screenshot is forced to the bound target window, at most one screenshot is returned per sequence, and the outer call must explicitly set `confirmSensitiveRead=true` before step 1 executes;
- Esc×2, Pause, consent revocation, mutation-epoch change, invalid/hidden target or total-duration expiry aborts before the next input/delay segment;
- sequence audit records bounded step index/tool/status/completed-count/duration only, never typed text, clipboard text or screenshot pixels.

Example payload shape (the inner `arguments` values are JSON strings because top-level MCP arguments remain flat):

```json
{
  "windowHandle": "1A02BC",
  "stepsJson": "[{\"tool\":\"desktop_mouse_click\",\"arguments\":\"{\\\"x\\\":500,\\\"y\\\":300,\\\"button\\\":\\\"left\\\"}\",\"delayAfterMs\":100},{\"tool\":\"desktop_key\",\"arguments\":\"{\\\"key\\\":\\\"TAB\\\"}\"}]",
  "confirmMutation": true
}
```

When the workflow needs another dialog/application handle, ChatGPT must perform normal observation/wait against that new window and submit a new sequence. Do not broaden one sequence into cross-window generic scripting.

## 6. Agent decision model

Decision order is **direct CAD API first → bounded allowlisted native command second → same-process background host control third → explicit foreground desktop fallback only when genuinely required**.

Representative inspection tools include `connector_info`, `qs3d_status`, `cad_active_document`, `cad_selection`, `cad_database_snapshot`, `cad_entity_inspect`, `cad_view_state`, `cad_wait_idle`, `cad_sysvar`, `cad_command_catalog`, `cad_audit_tail`, `diagnostics_*`, `bricscad_ui_text_snapshot`, desktop window observation and bounded `desktop_wait_for_window`.

Representative direct mutations include line/circle/arc/polyline/text/MText creation, `cad_entity_transform`, delete/layer operations and `qs3d_run_command`. Complex native workflows use `cad_command_sequence`. Standard BricsCAD-owned buttons/edit controls may use the same-process background tools. Cross-application/custom UI workflows may use explicit desktop focus/move/click/scroll/drag/type/key/clipboard tools only after local consent and explicit foreground-fallback policy selection. `desktop_sequence` is appropriate only when several deterministic UI steps can remain bound to one exact window; use explicit observation between sequences whenever state/target may have changed. `cad_agent_stop`, `cad_cancel_command`, and confirmed `cad_agent_resume` preserve recovery.

A successful tool call is not proof the drawing or external application state is correct. Re-inspect state before consequential follow-up actions and before final save/plot.

## 7. Guided local status, diagnostics and recovery

QS3D does not scrape the ChatGPT web conversation. ChatGPT remains the conversation UI. Agent Center mirrors only bounded local operational metadata such as onboarding state, desktop consent/idle-expiry state, current MCP action, bounded action ID/duration/result, next step, errors and recovery events. Normal MCP results/errors flow back to ChatGPT through `tools/call`.

The direct diagnostic tools `diagnostics_log_tail`, `diagnostics_since`, `diagnostics_snapshot` and bounded `diagnostics_wait` expose the redacted unified MCP/QS3D/BricsCAD event stream without arbitrary filesystem access. `McpDiagnosticHub` must continue to redact token/secret/password-like values and bound stored/returned content. UI text captured by `bricscad_ui_text_snapshot` is intentionally not persisted in that stream.

The Agent tab must make desktop states visible: waiting, controlling, locally paused/stopped, idle-expired and local re-enable required. Existing System/Dark/Light behavior, toast UX and blue active-control overlay remain authoritative.

Recovery uses two layers:

1. preserve a shorter existing BricsCAD autosave interval, otherwise ensure `SAVETIME <= 5`, and enable `ISAVEBAK=1`;
2. while CAD is idle, keep bounded coherent on-disk DWG copies under `%LOCALAPPDATA%\QS3D\Backups`, maximum 30 per drawing.

Recovery verifies the source did not change during copying and always restores to a new `Recovered` copy. It does not silently overwrite the active/original DWG. After an emergency stop or failed desktop action, Agent Center should keep recovery guidance visible when relevant.

Both V25 and V26 host entries start embedded MCP, persistent tunnel reconnect, recovery and first-run onboarding. Teardown revokes desktop consent before network services stop.

## 8. Source verification

For the current MCP surface, run/inspect at minimum:

- `scripts/preflight-mcp-oauth.py`
- `scripts/preflight-mcp-tools-list-json.py`
- `scripts/preflight-embedded-mcp.py`
- `scripts/preflight-mcp-full-agent.py`
- `scripts/preflight-mcp-desktop-function-calling.py`
- `scripts/preflight-mcp-direct-diagnostics-theme.py`
- `scripts/preflight-mcp-background-host-control.py`
- `scripts/test-mcp-guided-onboarding-control-recovery-source.py`
- `scripts/preflight-mcp-production-hardening.py`
- `scripts/preflight-mcp-session-handoff.py`
- `scripts/preflight-mcp-loopback-readonly.py`
- deterministic Core smoke and trusted V25/V26 compilation where selected by repository CI.

Desktop/background source guards must cover: default no-global-input policy, local-consent-gated foreground fallback, exact same-process HWND ownership, bounded UI text response, Button/Edit/RichEdit class allowlists, bounded `SendMessageTimeout`, PrintWindow-based window screenshot, no arbitrary shell/process/file reader, and the historical Approach A/B desktop invariants. Hosted/source evidence does not replace licensed runtime qualification.

## 9. LOCAL-024 — required runtime qualification

Hosted CI is not licensed BricsCAD/Cloudflare/ChatGPT/Windows-desktop runtime evidence. Runtime remains **`PENDING_LOCAL`** until a clean exact intended merged/release descendant is exercised on real Windows with licensed BricsCAD V25/V26.

The local matrix must cover:

1. exact DLL load in V25/V26 and local MCP health endpoint;
2. guided four-tab Agent Center plus first-run toast;
3. stable public HTTPS `/mcp` through provider-browser Cloudflare login + Named Tunnel;
4. protected-resource and authorization-server discovery;
5. ChatGPT DCR using only basic URL + OAuth setup;
6. deny then approve local OAuth consent;
7. PKCE S256 token exchange and `tools/list`, including background host tools, all 14 explicit Approach-A desktop primitives plus `desktop_sequence`, with no `desktop_macro` alias;
8. representative read-only CAD calls plus direct diagnostics and desktop cursor/window observation;
9. `bricscad_interaction_policy_get` starts as `background_only` after process start;
10. global desktop mutation rejects while `background_only` even if a stale caller attempts it;
11. `bricscad_ui_text_snapshot` captures bounded ordinary command-line/status/popup text where controls expose Win32 text and does not persist that content in diagnostics;
12. disposable same-process standard Button `bricscad_ui_invoke` works without changing foreground/cursor;
13. disposable same-process Edit/RichEdit `bricscad_ui_set_text` works without global keyboard input;
14. target-window `desktop_screenshot` via PrintWindow remains target-specific while another window overlaps it; custom/minimized/GPU limitations fail clearly rather than substituting desktop pixels;
15. bounded `desktop_wait_for_window` success + timeout behavior;
16. local desktop-consent OFF rejection and local enable behavior;
17. remote attempt to enable `foreground_fallback` without local desktop consent rejects;
18. locally consented `foreground_fallback` enables the historical guarded global-input tools;
19. 10-minute idle expiry plus local Pause/Resume behavior and remaining-time UI;
20. blue overlay while guarded desktop input/sensitive reads/sequence run;
21. `desktop_clipboard_read` and `desktop_screenshot` rejection without acknowledgement, then bounded success on disposable content;
22. screenshot crop validation and bounded output;
23. confirmed disposable window/mouse/type/key/clipboard-write behavior only in foreground fallback mode;
24. exact-target `desktop_mouse_drag` plus mid-drag Esc×2/fail-closed behavior on disposable UI;
25. local action timeline IDs/duration/terminal state with no sensitive payload persistence;
26. `desktop_sequence` success on a disposable single-window workflow with <=12 steps and bounded delays;
27. `desktop_sequence` rejection of target switching, nested sequence, clipboard read, unauthorized screenshot and oversized/overlong sequences;
28. mid-sequence Esc×2/Pause/target-loss cancellation with explicit partial-completion reporting and no implicit rollback;
29. physical Esc×2 emergency stop, CAD cancel and required local re-enable;
30. one-use auth-code replay rejection;
31. no-refresh behavior without `offline_access`;
32. refresh issuance with `offline_access`, rotation and old-token replay rejection;
33. scope/resource mismatch rejection;
34. BricsCAD restart invalidating process-bound refresh credentials, local desktop consent and resetting interaction policy to `background_only`;
35. legacy engineering-bearer compatibility;
36. Quick Tunnel URL invalidation and persistent Named Tunnel reconnect/autostart;
37. autosave/BAK policy, versioned snapshot retention and recovery-to-new-copy on a disposable drawing;
38. one confirmed disposable-DWG mutation plus audit/emergency-stop/cancel;
39. save/reopen and clean process shutdown.

Never commit access/refresh tokens, static bearer secrets, Cloudflare credentials, private paths/DWGs, clipboard contents, typed secrets, proprietary BricsCAD binaries or unsanitized screenshots.

## 10. Continuation rules

For future MCP work:

1. read repository governance first, then this file;
2. treat #4352 as parent architecture, #4584/#4597 as merged OAuth onboarding, #4629 as desktop/guided-control extension, and #4765 as the background-host/direct-diagnostics extension;
3. resolve current `main` and current source before changing anything;
4. edit active V2/OAuth/runtime source, not the legacy monolith;
5. preserve one repo/runtime, click-first setup, system/provider-browser identity ownership, OAuth basic-screen onboarding and API-first CAD control;
6. preserve `background_only` as the process-start default; do not make global cursor/focus/keyboard injection the normal CAD path;
7. preserve local desktop consent + idle expiry + visible active-control + Esc×2 emergency boundaries for desktop-wide fallback automation;
8. preserve both owner-approved completion phases on #4629: Approach A explicit primitives and Approach B bounded single-target `desktop_sequence`; do not add a `desktop_macro` alias or broaden sequence into arbitrary scripting;
9. never add arbitrary PowerShell/CMD/shell/process/script execution merely to make automation invisible; use CAD APIs, application-context commands and bounded same-process controls instead;
10. do not open a competing MCP carrier for the same scope;
11. update canonical specialist runbooks and the single LOCAL-024 handoff when behavior changes;
12. never promote hosted CI to `LOCAL_PASS`.

## 11. Definition of done

Source integration for OAuth (#4584/#4597) and the historical desktop/guided-control package (#4629) are already merged. #4765 is source-complete only when direct diagnostics/theme plus the background-host policy/tools and target-window PrintWindow contract satisfy fresh exact-candidate source/build gates and the canonical protected PR lands on current `main`.

Full runtime completion is separate: the exact intended merged/release descendant must pass LOCAL-024 with real BricsCAD V25/V26 + Cloudflare + ChatGPT + Windows behavior, sanitized evidence must be recorded, and the runtime item can then move from `PENDING_LOCAL` to completed.