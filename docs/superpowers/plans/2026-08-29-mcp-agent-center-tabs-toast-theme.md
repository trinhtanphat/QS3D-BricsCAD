# MCP Agent Center Tabs, Toast, and Theme — Landing Record

**Status:** `SOURCE_MERGED / HOSTED_CI_PASS / LOCAL-024_PENDING_LOCAL`  
**Canonical source owner:** #4629 / PR #4632  
**UI closeout:** #4647 — completed  
**Local qualification parent:** #72 / `LOCAL-024`  
**Authoritative runtime docs:** `docs/MCP-CANONICAL-RUNBOOK.md`, `docs/CHATGPT-MCP-INTEGRATION.md`

## Purpose

This file began as the implementation plan for the 2026-08-29 MCP Agent Center redesign. The first plan described a five-destination navigation model. During the canonical #4629 integration, the owner-approved product architecture converged on four task-focused tabs and that four-tab model is now authoritative in production source and MCP documentation.

Do **not** use the historical five-destination wording as an implementation contract. The shipped/source-tracked layout is:

1. **Kết nối** — embedded MCP, cloudflared/Cloudflare, Named Tunnel, public `/mcp`, ChatGPT connector setup and protocol/read-only verification.
2. **Agent** — local desktop consent, idle state, Action ID/status, Pause/Resume, Emergency Stop/cancel and bounded activity state.
3. **Backup & khôi phục** — autosave/BAK status, versioned backup and recovery-to-copy behavior.
4. **Nâng cao** — Quick Tunnel test fallback, engineering bearer compatibility, diagnostics/self-test and audit access.

The normal production path is Named Tunnel + OAuth/DCR. Quick Tunnel and static bearer remain test/backward-compatible engineering paths.

## Final source architecture

The production implementation remains programmatic WPF in `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs` and preserves the existing MCP/Cloudflare/agent semantics. The final UI uses:

- `ThemeMode { System, Dark, Light }` with Windows `AppsUseLightTheme` tracking in System mode;
- QS3D-owned button templates and semantic palettes instead of depending on host button chrome;
- explicit keyboard focus foreground/background plus focus border/thickness;
- intentional trigger precedence `focus -> hover -> pressed -> disabled`, so focused buttons do not mask later hover/pressed/disabled colors;
- a high-Z toast overlay with Info/Success/Warning/Error states;
- bounded in-memory activity history (`MaxActivityEntries = 50`);
- a maximum of four visible toasts;
- retained `DispatcherTimer` handlers that are stopped and explicitly detached by the unified dismiss/cleanup path;
- theme rebuild and window close cleanup that clears visible toasts safely;
- no bearer/token rendering in logs or toast text;
- provider-owned browser authentication: QS3D does not collect Cloudflare/ChatGPT passwords or scrape browser cookies/conversation content.

## Completed implementation checklist

### Source contract and theme/button states

- [x] Replace the obsolete flat-dashboard contract with the task-oriented Agent Center contract.
- [x] Add System/Dark/Light theme state and Windows system-theme resolution.
- [x] Add QS3D-owned button template/style.
- [x] Add explicit hover, pressed, keyboard-focus and disabled foreground/background/border behavior.
- [x] Harden keyboard focus to own computed foreground/background plus focus border/thickness.
- [x] Preserve trigger precedence `focus -> hover -> pressed -> disabled`.

### Navigation, toast and activity UI

- [x] Replace the flat dashboard with the authoritative four task tabs: Kết nối / Agent / Backup & khôi phục / Nâng cao.
- [x] Add a layered root with high-Z right-aligned toast host.
- [x] Add bounded activity history with a 50-entry cap.
- [x] Add Info/Success/Warning/Error toast behavior with bounded visible-card count.
- [x] Retain and explicitly detach toast timer Tick handlers.
- [x] Route theme-rebuild/window-close toast cleanup through the unified dismiss path.
- [x] Remove stale architecture wording that claimed Approach B was not exposed.

### Operation feedback and MCP integration

- [x] Preserve Cloudflare install/login/tunnel actions and canonical public-endpoint resolution.
- [x] Preserve protocol check and read-only self-test.
- [x] Preserve local desktop consent, Pause/Resume, Emergency Stop/cancel and bounded status timeline.
- [x] Keep normal ChatGPT integration on public HTTPS `/mcp` + OAuth/DCR.
- [x] Keep Quick Tunnel test-only and static bearer under engineering compatibility.
- [x] Reflect Completion Pack A explicit desktop tools and owner-approved bounded `desktop_sequence`; no `desktop_macro` alias.

### Guard, CI and landing

- [x] Unify `scripts/preflight-mcp-agent-center-uiux.py` under canonical #4629 ownership instead of keeping the stale five-destination/direct-control assertions.
- [x] Preserve the #4647 focus-state, trigger-precedence and toast-cleanup hardening in the unified contract.
- [x] Exact-head Shared CI for PR #4632 completed successfully before merge.
- [x] PR #4632 merged to protected `main` as `ca824a0bf80a34d3502a0c9c9065b7b9fe3e12ae`.
- [x] V25 cloud preview `v0.1.0-preview.10253` was published from that exact merge commit.
- [x] #4647 source closeout was completed after verifying the corrections were present in the canonical landing.

## LOCAL-024 — still intentionally pending

Source/static/hosted CI is **not** licensed Windows/BricsCAD visual evidence. The following remain `LOCAL_ONLY / PENDING_LOCAL / DO_NOT_RETRY_REMOTE` until actually exercised on the exact intended candidate:

- [ ] BricsCAD V25/V26 render of all four tabs at the intended window sizes, including 1040×780 and 780×620 where applicable.
- [ ] System/Dark/Light visual verification plus live Windows theme switching while in System mode.
- [ ] 100% / 125% / 150% DPI coverage where the authorized local host supports it.
- [ ] URL wrapping, scrolling and minimum-size behavior without clipped or overlapping controls.
- [ ] Primary/secondary/danger/utility/navigation/theme-choice button focus, hover, pressed and disabled contrast/readability.
- [ ] Toast stacking, close action, automatic lifetimes, cleanup after theme rebuild/window close, and readable error/warning states.
- [ ] Bounded activity/log history and proof that tokens, credentials, clipboard contents, typed secrets and other sensitive payloads are not rendered or persisted.
- [ ] Real Cloudflare Named Tunnel + ChatGPT OAuth/DCR onboarding and local desktop-control interactions covered by the broader `LOCAL-024` matrix.

Record licensed results only in the canonical local evidence flow under #72 / `docs/LOCAL-AGENT-INBOX.md`. Hosted CI must never be promoted to `LOCAL_PASS`.

## Continuation rule

Do not reopen a duplicate Agent Center source redesign lane merely because this historical file once described five destinations. Current source plus the canonical MCP runbooks define implementation truth. A new source lane is warranted only if fresh runtime evidence or a current source review proves a distinct defect. Otherwise continue the existing `LOCAL-024` qualification for environment-dependent evidence.
