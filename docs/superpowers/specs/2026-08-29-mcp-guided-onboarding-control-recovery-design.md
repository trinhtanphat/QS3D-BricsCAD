# QS3D guided MCP onboarding, visible control and recovery design

Date: 2026-08-29

## Goal

A new QS3D user should be able to install/load QS3D, click one MCP entry point, complete the provider-owned browser authentication steps, connect ChatGPT to the embedded QS3D MCP server, and then operate BricsCAD through ChatGPT without a second repository, Node runtime or shell-driven setup.

The experience must also make powerful desktop automation visible and locally revocable, while keeping frequent project recovery copies so an interrupted or mistaken workflow can be recovered.

## Owner-approved completion decision

Two completion approaches were considered for the desktop-control layer.

### Approach A — Completion Pack — SELECTED

Approach A completes the existing explicit/bounded desktop tool model without adding a generic macro executor. It is the approved implementation target for Issue #4629 / PR #4632.

Additions:

1. `desktop_mouse_drag`
   - requires `confirmMutation=true` and local desktop consent;
   - requires an exact visible current-session `windowHandle`;
   - requires bounded start/end points inside the current target bounds;
   - focuses and revalidates exact foreground ownership before injection;
   - revalidates the target and mutation/consent generation during the drag so Esc×2 or another stop condition fails closed;
   - supports only explicit left/right/middle button drag with bounded duration.
2. `desktop_wait_for_window`
   - read-only polling helper for visible top-level windows in the current interactive session;
   - matches bounded non-secret window metadata such as title text and optional exact handle;
   - timeout is bounded to at most 15 seconds;
   - returns the validated matching window metadata or a bounded timeout result; it does not click or focus automatically.
3. `desktop_screenshot` crop support
   - optional crop rectangle for either the validated target window or virtual desktop;
   - crop is intersected with the selected source bounds and rejected if empty/invalid;
   - final image dimensions/output remain under the existing screenshot caps;
   - no screenshot pixels or crop contents are persisted to audit/status.
4. Desktop-consent idle expiry
   - local desktop consent expires after 10 minutes with no guarded desktop action;
   - each successfully-started guarded action refreshes the idle deadline;
   - expiry revokes consent and invalidates the consent generation before the next remote input can proceed;
   - restart still resets consent to OFF.
5. Local Pause/Resume UX
   - `Pause` is a local action that revokes desktop consent and performs the existing emergency-stop behavior;
   - `Resume` is also local-only and creates a new consent generation after the keyboard emergency hook is installed successfully;
   - MCP has no remote method that can resume or enable desktop consent;
   - Agent Center shows ON/OFF/paused state plus remaining idle time.
6. Enriched local action timeline
   - desktop actions carry a bounded local `actionId`, start/end time, duration and success/failure/cancelled status;
   - audit/timeline must still exclude typed text, clipboard contents, screenshot pixels, OAuth/bearer tokens and private DWG contents;
   - current action + next step are visible in Agent Center without scraping ChatGPT Web.
7. UX polish
   - explicit states for waiting, controlling, stopped/expired and local re-enable required;
   - preserve existing System/Dark/Light theme, toast path and blue active-control overlay;
   - recovery hints remain visible after emergency stop or failed desktop action.

### Approach B — Batch/Macro automation — DEFERRED

Approach B would add a higher-level `desktop_sequence` / `desktop_macro` style call that can execute multiple focus/click/type/wait/screenshot steps from one MCP request.

It is intentionally **not part of Issue #4629 / PR #4632** and must not appear in the current `tools/list`. It is deferred because a multi-step remote macro has a larger blast radius, more complex mid-sequence cancellation semantics, harder per-step target revalidation and less transparent audit behavior.

If pursued later, Approach B requires its own design/reservation after Approach A has been locally qualified. The future design must preserve per-step exact-window validation, local consent, emergency stop, bounded sequence length/time, per-step audit and fail-closed cancellation. No future macro design may become an arbitrary shell/process/script execution surface.

## Product decisions

### 1. System browser owns Cloudflare and ChatGPT identity

QS3D does not embed credential forms, scrape browser cookies, copy passwords or attempt to persist ChatGPT session cookies. `cloudflared tunnel login` opens Cloudflare's browser-owned authentication flow; ChatGPT is opened in the user's normal browser so the browser owns the ChatGPT login session.

QS3D persists only QS3D-owned settings plus the normal provider artifacts produced by cloudflared. The existing OAuth/DCR MCP server remains the preferred ChatGPT authentication path. Static engineering bearer copy remains an advanced compatibility path, not the primary onboarding UX.

### 2. One MCP Control Center

`QS3DMCPAGENTCENTER` is the canonical user surface. It presents four task-oriented tabs:

- **Kết nối** — embedded MCP, cloudflared, Cloudflare login, Named Tunnel and ChatGPT registration.
- **Agent** — local desktop-control consent, pause/resume, idle-expiry state, emergency stop, status/current action/next step and recent local events.
- **Backup & khôi phục** — BricsCAD autosave policy, versioned QS3D recovery copies, manual backup and restore-to-copy.
- **Nâng cao** — Quick Tunnel test fallback, bearer compatibility copy, protocol probe, self-test and audit folder.

The primary connect path is sequential and self-explanatory. Quick Tunnel remains explicitly test-only.

### 3. Local consent for desktop-wide input

The existing `desktop_*` MCP tools remain separate from BricsCAD-confined `cad_ui_*` tools. Desktop mutation and sensitive desktop reads additionally require a **local, in-memory consent session** enabled from QS3D after each BricsCAD start.

This local consent is intentionally not persisted. A remote ChatGPT request cannot turn it on. Under the selected Completion Pack, an enabled session also expires after 10 minutes of desktop-action inactivity.

When a guarded desktop call is active:

- a topmost blue border/banner states that QS3D MCP is controlling/reading the Windows desktop;
- the current MCP tool is displayed;
- a low-level keyboard hook listens for **Esc twice within 1.2 seconds**;
- double-Esc disables local desktop consent, advances the MCP emergency-stop epoch and requests cancellation of the active BricsCAD command.

Stopping/pausing/expiring the desktop session prevents further desktop actions. Re-enabling is a local user action.

### 4. Status is mirrored, not scraped from ChatGPT Web

QS3D must not automate or scrape ChatGPT's web UI to mirror assistant prose. ChatGPT remains responsible for its own conversation display.

QS3D maintains a bounded local event timeline containing onboarding, tunnel, desktop action, backup and error events. Desktop events may include a bounded `actionId`, duration and terminal state but never sensitive payloads. The Control Center displays current action, last result, error and next step. MCP tool results/errors already return local execution outcomes to ChatGPT; `qs3d_status` and `cad_audit_tail` remain the canonical remote inspection surfaces.

This gives bidirectional operational interaction without pretending the MCP server can push arbitrary unsolicited text into the ChatGPT conversation UI.

### 5. Recovery uses two independent layers

Power-loss recovery and accidental-delete recovery need different mechanisms:

1. **BricsCAD autosave safety** — QS3D ensures `SAVETIME` is enabled at a conservative maximum interval of five minutes and `ISAVEBAK=1`, without increasing an already shorter user autosave interval.
2. **Versioned QS3D copies** — while BricsCAD is idle, QS3D periodically copies the last coherent on-disk DWG into `%LOCALAPPDATA%/QS3D/Backups/<project-key>/`. Copies are bounded by retention and are never used to overwrite the active drawing automatically.

Manual recovery restores a selected/latest backup to a new `Recovered` copy. The original file is never overwritten by the recovery helper.

### 6. Tunnel policy

The production path is a stable Cloudflare Named Tunnel with a stable HTTPS hostname and `/mcp` endpoint. Quick Tunnel remains a one-click diagnostic fallback only. Cloudflare authentication is completed in the provider browser; QS3D does not store the Cloudflare password.

### 7. ChatGPT capability boundary

The embedded MCP server exposes direct CAD API tools first, bounded native command workflows second, BricsCAD-only UI fallback third, and explicit Windows desktop tools only for cross-application workflows.

Arbitrary shell/process execution remains outside the remote MCP surface. Broad mouse/keyboard/window/clipboard/screenshot access is provided through explicit bounded tools, local consent, per-call mutation/sensitive-read acknowledgement, audit metadata and emergency stop. Approach A extends that explicit surface with drag, bounded window waiting and cropped screenshots. Approach B generic batch/macro execution remains deferred.

## Onboarding state model

The Control Center derives the next action from observable local state:

1. `EmbeddedServerStarting`
2. `CloudflaredMissing`
3. `CloudflareLoginRequired`
4. `NamedTunnelRequired`
5. `PublicEndpointReady`
6. `ChatGptRegistrationRequired`
7. `Ready`
8. `ErrorRecovery`

The state model never equates "ChatGPT browser opened" with "connector registered". The final registration remains user-visible in ChatGPT, while QS3D can verify the local MCP protocol and public endpoint prerequisites.

## First-run notification

After plugin startup, QS3D schedules a non-blocking toast when cloudflared or MCP onboarding is incomplete. The toast links to the MCP Control Center and is rate-limited so it does not appear on every document operation.

## Backup constraints

- copy only a real saved source file;
- skip while `CMDACTIVE != 0`;
- compare source length/write timestamp before and after copy and discard a copy if the source changed during the copy;
- bounded retention per drawing;
- do not include private DWG paths or file contents in MCP audit messages;
- recovery always writes a new file.

## Security invariants

- no browser-cookie scraping;
- no password capture;
- no remote method that enables/resumes the local desktop-consent session;
- no persistence of local desktop consent across BricsCAD restart;
- local desktop consent expires after 10 minutes of desktop-action inactivity;
- no arbitrary remote process/shell launch;
- no `desktop_sequence` / generic desktop macro in the selected Approach A scope;
- exact target-window validation is required for click/scroll/drag and other targeted input;
- double-Esc remains available while a desktop action is running;
- Quick Tunnel is not presented as the production path;
- recovery never overwrites the active DWG automatically;
- audit/status strings are bounded and must not include clipboard contents, typed secrets, screenshots or bearer/OAuth tokens.

## Runtime qualification

Source implementation can be completed in-repository, but exact Windows keyboard hook, topmost overlay, drag/window-wait/screenshot-crop behavior, consent idle expiry/pause/resume, BricsCAD autosave variables, DWG copy behavior, Cloudflare login/tunnel and ChatGPT connector behavior remain `PENDING_LOCAL` until exercised on the intended Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT environment.
