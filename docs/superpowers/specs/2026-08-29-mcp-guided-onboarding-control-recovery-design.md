# QS3D guided MCP onboarding, visible control and recovery design

Date: 2026-08-29

## Goal

A new QS3D user should be able to install/load QS3D, click one MCP entry point, complete provider-owned browser authentication, connect ChatGPT to the embedded QS3D MCP server, and operate BricsCAD through ChatGPT without a second repository, Node runtime or shell-driven setup.

The experience must make powerful desktop automation visible and locally revocable, preserve frequent project recovery copies, and support bounded multi-step UI workflows without turning MCP into an arbitrary script/shell surface.

## Owner-approved completion decision

The owner approved both completion phases in order: **Approach A first, then Approach B on the same canonical #4629/#4632 carrier**.

### Approach A — Completion Pack — SELECTED / SOURCE IMPLEMENTED

Approach A completes the explicit/bounded desktop tool model.

1. `desktop_mouse_drag`
   - `confirmMutation=true` plus local desktop consent;
   - exact visible current-session `windowHandle`;
   - bounded start/end points inside current target bounds;
   - exact foreground ownership and mutation/consent revalidation during injection;
   - left/right/middle button only, bounded duration, best-effort button-up on cancellation.
2. `desktop_wait_for_window`
   - read-only visible top-level current-session polling;
   - bounded title substring and/or exact handle matching;
   - timeout <= 15 seconds;
   - never implicitly focuses/clicks.
3. `desktop_screenshot` crop
   - optional crop rectangle relative to the selected source;
   - crop intersected with source bounds and rejected when invalid/empty;
   - existing image/output caps remain.
4. Desktop-consent idle expiry
   - 10 minutes with no newly started guarded desktop action;
   - restart resets consent to OFF.
5. Local Pause/Resume
   - Pause revokes consent and emergency-stops mutation;
   - Resume is local-only and requires successful emergency keyboard-hook setup;
   - MCP cannot remotely enable/resume consent.
6. Enriched local action timeline
   - bounded action ID, duration and terminal status;
   - no typed text, clipboard contents, screenshot pixels, OAuth/bearer token or DWG content in audit/status.
7. Agent Center UX
   - waiting/controlling/stopped/expired/re-enable-required states;
   - System/Dark/Light, toast UX and blue active-control overlay retained;
   - recovery hints remain visible after stop/failure.

### Approach B — Bounded batch/macro automation — SELECTED

Approach B adds **one canonical tool, `desktop_sequence`**. There is no separate `desktop_macro` alias because duplicate generic dispatch surfaces make capability review and audit harder.

#### Sequence boundary

A sequence is a bounded convenience layer over existing explicit desktop primitives, not a new scripting engine.

- The call binds to exactly one visible current-session `windowHandle` before execution.
- The sequence may not switch target handles internally.
- When a new dialog/application requires another handle, ChatGPT must use normal observation/wait tooling to obtain that handle and submit a new sequence.
- Maximum **12 steps**.
- Maximum **30 seconds** wall-clock execution.
- Optional delay after a step is bounded to **0–2000 ms** and is checked in short slices so Esc×2/emergency-stop is observed quickly.
- Execution is **fail-fast**. There is no `continueOnError` mode.
- No nested `desktop_sequence` and no recursion.
- There is **no atomic rollback**. If step 4 fails, steps 1–3 may already have affected the UI. Results/audit must make partial completion explicit.

#### Transport shape

The existing MCP top-level argument object remains flat. To preserve that invariant, `desktop_sequence` accepts:

- `windowHandle` — exact target handle, required;
- `stepsJson` — bounded string containing a JSON array of step records;
- `maxDurationMs` — optional, 1000–30000, default 15000;
- `confirmMutation` — required true;
- `confirmSensitiveRead` — required true only when a sequence includes a screenshot step.

`stepsJson` is decoded locally. Each array element is a flat record:

```json
{"tool":"desktop_mouse_click","arguments":"{\"x\":500,\"y\":300,\"button\":\"left\"}","delayAfterMs":100}
```

`arguments` is itself a bounded **flat JSON-object string** for the selected primitive. Nested arrays/objects are not accepted. Sequence step arguments must not supply `windowHandle`, `confirmMutation` or `confirmSensitiveRead`; those security-sensitive values are owned/injected by the sequence executor.

#### Sequence allowlist

Allowed primitives are intentionally narrow:

- `desktop_window_focus`
- `desktop_mouse_move`
- `desktop_mouse_click`
- `desktop_mouse_scroll`
- `desktop_mouse_drag`
- `desktop_type`
- `desktop_key`
- `desktop_clipboard_write`
- `desktop_wait_for_window` (bound to the same handle, useful for waiting for title/state changes)
- `desktop_screenshot` (forced to the bound target window)

Not allowed inside a sequence:

- `desktop_clipboard_read`;
- `desktop_window_list`, `desktop_foreground_window`, `desktop_cursor_position`;
- `desktop_sequence` itself;
- any `cad_*`, `qs3d_*`, plugin command, filesystem command, shell/process/script/eval operation;
- any target handle supplied by a step.

For workflows that need observation data or a new handle, ChatGPT performs a normal explicit read tool call between sequences.

#### Sensitive screenshot rule

A sequence can contain target-window screenshots for verification, but only when the sequence call has `confirmSensitiveRead=true`. Sequence screenshot steps are forced to `scope=window` and the bound `windowHandle`; they cannot silently expand capture to the virtual desktop. Clipboard read remains excluded from sequence batching.

#### Cancellation and target safety

- The whole sequence requires `confirmMutation=true` and current local desktop consent.
- The canonical mutation epoch is checked before each step and throughout delays/long-running drag/wait operations.
- Esc×2, Pause, consent revocation or agent stop aborts before the next injected input.
- Target-bound input keeps exact current-session window visibility/bounds/foreground checks from the underlying primitive.
- `desktop_mouse_move` in a sequence additionally requires its point to be within the bound target window and focuses/revalidates that target before moving.
- Each completed/failed step produces only bounded metadata in audit; typed/clipboard/screenshot payload content is never audited.

## Product decisions

### 1. System browser owns Cloudflare and ChatGPT identity

QS3D does not embed credential forms, scrape browser cookies, copy passwords or persist ChatGPT session cookies. `cloudflared tunnel login` opens Cloudflare's provider-owned browser authentication; ChatGPT opens in the user's normal browser so the browser owns its login session.

QS3D persists only QS3D-owned settings plus normal provider artifacts produced by cloudflared. Existing OAuth/DCR remains the preferred ChatGPT authentication path. Static bearer copy remains an advanced compatibility path.

### 2. One MCP Control Center

`QS3DMCPAGENTCENTER` is canonical and uses four task-oriented tabs:

- **Kết nối** — embedded MCP, cloudflared, Cloudflare login, Named Tunnel and ChatGPT registration.
- **Agent** — desktop consent, pause/resume, idle-expiry state, emergency stop, current action/next step and recent local events.
- **Backup & khôi phục** — autosave, versioned recovery copies, manual backup and restore-to-copy.
- **Nâng cao** — Quick Tunnel test fallback, bearer compatibility copy, protocol probe, self-test and audit folder.

### 3. Local consent for desktop-wide input

Desktop mutation and sensitive reads require a local, process-memory-only consent session. Remote ChatGPT cannot enable it. Enabled consent expires after 10 minutes of desktop-action inactivity.

While a guarded action is active, a topmost blue border/banner is visible and a low-level keyboard hook listens for **Esc twice within 1.2 seconds**. Double-Esc revokes consent, advances the mutation emergency-stop epoch and requests CAD cancellation. Stopping/pausing/expiry prevents further desktop actions until the user locally resumes.

### 4. Status is mirrored, not scraped from ChatGPT Web

QS3D does not scrape ChatGPT's web UI. It keeps a bounded local event timeline for onboarding, tunnel, desktop actions/sequences, backup and errors. MCP tool results/errors return execution outcomes to ChatGPT; `qs3d_status` and `cad_audit_tail` remain canonical remote inspection surfaces.

### 5. Recovery uses two independent layers

1. **BricsCAD autosave safety** — ensure `SAVETIME` is enabled with a conservative maximum interval of five minutes and `ISAVEBAK=1`, without increasing an already shorter user interval.
2. **Versioned QS3D copies** — while BricsCAD is idle, periodically copy a coherent on-disk DWG into `%LOCALAPPDATA%/QS3D/Backups/<project-key>/` with bounded retention.

Recovery restores to a new `Recovered` copy; it never silently overwrites the active/original DWG.

### 6. Tunnel policy

Production uses a stable Cloudflare Named Tunnel + HTTPS hostname + `/mcp`. Quick Tunnel remains test-only. Browser/provider owns Cloudflare authentication.

### 7. ChatGPT capability boundary

Preferred order remains direct CAD API tools, bounded native command workflows, BricsCAD-confined UI fallback, then explicit Windows desktop tools. `desktop_sequence` only batches its narrow allowlist of existing desktop primitives. It never exposes arbitrary process/shell/script/plugin dispatch.

## Onboarding state model

1. `EmbeddedServerStarting`
2. `CloudflaredMissing`
3. `CloudflareLoginRequired`
4. `NamedTunnelRequired`
5. `PublicEndpointReady`
6. `ChatGptRegistrationRequired`
7. `Ready`
8. `ErrorRecovery`

Opening ChatGPT is not treated as proof that connector registration succeeded.

## First-run notification

After plugin startup, QS3D schedules a rate-limited non-blocking toast when cloudflared/MCP onboarding is incomplete. The toast opens MCP Control Center.

## Backup constraints

- copy only a real saved source file;
- skip while `CMDACTIVE != 0`;
- compare source length/write timestamp before and after copy and discard a changed-in-flight copy;
- bounded retention per drawing;
- no private DWG path/content in MCP audit;
- recovery always writes a new file.

## Security invariants

- no browser-cookie scraping or password capture;
- no remote method enables/resumes local desktop consent;
- no persistence of consent across BricsCAD restart;
- 10-minute desktop-action idle expiry;
- no arbitrary remote process/shell/script launch;
- `desktop_sequence` is single-target, bounded, fail-fast and non-recursive;
- no `desktop_macro` alias;
- no `desktop_clipboard_read` inside a sequence;
- sequence screenshot is bound-window-only and requires explicit sensitive-read confirmation;
- exact target validation remains required around injected input;
- double-Esc remains available during sequence execution;
- Quick Tunnel is not production;
- recovery never automatically overwrites the active DWG;
- audit/status never includes clipboard contents, typed content, screenshots or bearer/OAuth tokens.

## Runtime qualification

Source implementation can be completed in-repository, but Windows keyboard hook/overlay, drag/wait/crop, idle expiry/pause/resume, bounded sequence timing/cancellation, BricsCAD autosave/recovery, Cloudflare tunnel and ChatGPT connector behavior remain `PENDING_LOCAL` until exercised on the intended Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT environment.
