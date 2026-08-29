# QS3D MCP guided onboarding, desktop control and recovery

Status: SOURCE_TRACKED / `PENDING_LOCAL`  
Issue: #4629  
Parent MCP architecture: #4352  
OAuth integration: #4584 / PR #4597

## End-user goal

A normal user installs/loads QS3D inside BricsCAD, opens **TOOL > MCP (AI) > Agent Center**, follows the guided connection flow, and then uses ChatGPT as the conversation UI while QS3D executes bounded MCP actions inside the BricsCAD/Windows session.

No second MCP repository, Node runtime, PowerShell/CMD setup, ChatGPT password capture, Cloudflare password capture or browser-cookie scraping is part of the normal path.

## Completion Pack decision

The owner selected **Approach A** for #4629 / PR #4632 and asked that both choices remain documented for future agents.

### Approach A — selected/current

Approach A keeps every desktop capability explicit and individually bounded:

- `desktop_mouse_drag` — exact-window bounded drag with continuous target/emergency-stop revalidation;
- `desktop_wait_for_window` — bounded read-only wait for a visible current-session window;
- `desktop_screenshot` optional crop (`cropX`, `cropY`, `cropWidth`, `cropHeight`);
- local desktop consent auto-expires after 10 minutes without a new guarded desktop action;
- explicit local **Pause desktop** / **Resume desktop**;
- local timeline includes bounded Action ID, duration and terminal state;
- Agent Center shows waiting/active/paused/expired/re-enable guidance and recovery hints.

### Approach B — deferred/future

A generic `desktop_sequence` / `desktop_macro` executor could batch focus/click/type/wait/screenshot steps in one MCP call. It is intentionally **not exposed by this PR** because batching increases audit, cancellation and stale-UI risk. A future implementation needs its own design/reservation and must preserve per-step exact-target checks, bounded sequence/time, local consent, Esc×2, mutation-epoch checks and audit. It must never become arbitrary shell/process/script execution.

## Control Center

`QS3DMCPAGENTCENTER` is the canonical local control surface with four tabs:

1. **Kết nối** — embedded MCP, cloudflared, Cloudflare browser login, Named Tunnel, public MCP URL and ChatGPT OAuth registration.
2. **Agent** — local desktop-control state, idle countdown, Pause/Resume, Action ID/duration/result, current action/next step, Emergency Stop and CAD cancel.
3. **Backup & khôi phục** — BricsCAD autosave/BAK status, manual versioned snapshot, latest-snapshot recovery to a new file and backup-folder access.
4. **Nâng cao** — protocol/read-only self-test, Quick Tunnel test fallback, engineering bearer compatibility, timeline and audit-folder access.

The window follows the Windows app light/dark preference and explicitly sets normal/keyboard-focus/hover/pressed/disabled foreground/background/border states so buttons do not lose contrast when focused, hovered or clicked.

## Production connection path

```text
BricsCAD + QS3D
  -> embedded MCP on 127.0.0.1:8765
  -> install/verify cloudflared if needed
  -> Cloudflare provider-browser login
  -> persistent Named Tunnel + stable HTTPS hostname
  -> public https://<host>/mcp
  -> ChatGPT custom MCP using URL + OAuth
  -> QS3D OAuth/DCR + local authorization consent
  -> tools/list + tools/call
```

Cloudflare and ChatGPT identity stay in the system browser/provider flow. QS3D stores only QS3D-owned settings and normal cloudflared provider artifacts needed for reconnect.

**Named Tunnel is the production default.** `Quick Tunnel · test only` is a diagnostic fallback because its hostname may rotate and invalidate resource-bound OAuth credentials.

The static engineering bearer remains compatibility/debug functionality under **Nâng cao**. It is not the normal ChatGPT onboarding path.

## Desktop tool surface — Approach A

The explicit current desktop surface is 14 tools:

Read-only/observation:

- `desktop_cursor_position`
- `desktop_window_list`
- `desktop_foreground_window`
- `desktop_wait_for_window`

Mutation/input:

- `desktop_window_focus`
- `desktop_mouse_move`
- `desktop_mouse_click`
- `desktop_mouse_scroll`
- `desktop_mouse_drag`
- `desktop_type`
- `desktop_key`
- `desktop_clipboard_write`

Sensitive reads:

- `desktop_clipboard_read`
- `desktop_screenshot`

`desktop_wait_for_window` never focuses/clicks. Its timeout is bounded to 15 seconds and it only observes visible top-level windows in the current interactive Windows session.

Click/scroll/drag require an exact `windowHandle`; the target must still be visible/current-session, coordinates must remain inside current target bounds, and foreground ownership is revalidated immediately around input injection. Drag duration is bounded and the target/emergency-stop state is rechecked throughout the gesture.

Screenshot crop is relative to the selected source (window or virtual desktop). If any crop field is supplied, all four crop fields are required; invalid/empty intersections are rejected before capture, and the normal screenshot dimension/payload caps still apply.

## Local desktop consent

The `desktop_*` MCP namespace can cross application boundaries, so network confirmation alone is insufficient.

- desktop mutation still requires top-level `confirmMutation=true`;
- clipboard/screenshot sensitive reads still require `confirmSensitiveRead=true`;
- mutation and sensitive reads additionally require **local desktop consent** enabled by the user in the Agent tab;
- that consent is memory-only and resets every BricsCAD process start;
- it automatically becomes `EXPIRED` after 10 minutes without a newly started guarded desktop action;
- user can explicitly **Pause desktop** and **Resume desktop** locally;
- there is no MCP tool that can remotely enable/resume local consent.

While a guarded desktop action is active, QS3D shows a click-through **blue desktop border/banner** naming the current MCP tool and its bounded Action ID.

A physical **Esc twice within 1.2 seconds**:

1. changes desktop control to a stopped/paused state immediately;
2. advances the MCP emergency-stop epoch;
3. hides the control overlay;
4. requests cancellation of the active BricsCAD command;
5. requires a local user to Resume desktop before cross-application automation resumes.

After `PAUSED`, `EXPIRED`, cancellation or failure, the Agent tab tells the user to **Kiểm tra drawing/backup** before resuming desktop control.

## Status interaction with ChatGPT

ChatGPT remains the conversation UI. QS3D does **not** scrape ChatGPT Web or attempt to mirror arbitrary assistant prose.

Instead QS3D maintains a bounded local operational timeline for onboarding, tunnel, MCP action, recovery and error events. Desktop action entries can contain:

- bounded Action ID;
- UTC start/end-derived duration;
- terminal state (`running`, `success`, `failed`, `cancelled`);
- current action and next recommended step.

They must never persist typed text, clipboard contents, screenshot pixels, OAuth/bearer tokens or private DWG contents.

MCP tool results/errors are returned normally to ChatGPT through `tools/call`. `qs3d_status` and `cad_audit_tail` remain the canonical remote inspection surfaces.

## Backup and recovery

QS3D uses two independent recovery layers.

### BricsCAD native safety

At plugin startup the recovery service:

- preserves an already shorter `SAVETIME` interval;
- enables autosave or reduces an interval longer than five minutes to `5`;
- enables `ISAVEBAK=1` when needed.

### Versioned QS3D recovery copies

While BricsCAD is idle, QS3D can copy the last coherent saved DWG into:

```text
%LOCALAPPDATA%\QS3D\Backups\<drawing-key>\
```

Safety rules:

- source must be an existing saved `.dwg`;
- `CMDACTIVE` must be zero;
- source length and write timestamp are checked before/after copy;
- a source that changes during copying causes the intermediate copy to be discarded;
- retention is bounded to 30 snapshots per drawing;
- recovery always creates a new file below `Backups\Recovered`;
- the active/original DWG is never silently overwritten by the recovery helper.

## Startup and shutdown

Both BricsCAD V25 and V26 host entries start:

- embedded MCP;
- persistent Cloudflare reconnect attempt;
- public endpoint resolver;
- project recovery service;
- first-run onboarding toast.

During teardown, QS3D revokes desktop consent first, stops onboarding/recovery, then stops tunnel/MCP services. This prevents desktop injection from surviving the host lifecycle.

## First-run toast

When onboarding is incomplete, QS3D may show a non-blocking, rate-limited toast that opens the MCP Control Center. The toast describes the next safe setup step and never asks for credentials.

## Security invariants

- no password or browser-cookie scraping;
- no arbitrary remote shell/process-launch or generic desktop macro tool;
- `desktop_sequence` / `desktop_macro` are absent in Approach A;
- desktop input limited to visible top-level windows in the current interactive Windows session;
- exact target/foreground revalidation for targeted mouse/keyboard interaction;
- bounded cursor/window/text/click/scroll/drag/wait/screenshot/clipboard surfaces;
- single alphanumeric `desktop_key` values are audited as non-content `CHARACTER`, not the caller character;
- typed text, clipboard contents and screenshot pixels are not persisted in the MCP mutation audit;
- local desktop consent cannot be remotely enabled/resumed;
- Esc×2 remains a local emergency control;
- recovery does not overwrite the active source DWG automatically.

## LOCAL_ONLY qualification

Source/static/hosted validation cannot prove real Windows desktop input, licensed BricsCAD behavior, Cloudflare login/tunnel or ChatGPT connector behavior.

The exact intended merged/release SHA remains `PENDING_LOCAL` under `LOCAL-024` until a clean Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT pass covers at least:

1. exact V25/V26 plugin load and MCP health;
2. guided four-tab Agent Center and first-run toast;
3. Cloudflare provider-browser login + persistent Named Tunnel;
4. ChatGPT URL + OAuth/DCR connection;
5. `tools/list` including all 14 Approach A `desktop_*` tools and no `desktop_sequence`/macro;
6. read-only CAD plus cursor/window observation;
7. `desktop_wait_for_window` success + timeout behavior;
8. local desktop-consent OFF rejection, Resume, Pause and 10-minute idle-expiry behavior;
9. blue overlay with Action ID while guarded desktop tools run;
10. clipboard/screenshot sensitive-read gating plus cropped screenshot on disposable content;
11. exact-window click/scroll and bounded drag on disposable content;
12. desktop keyboard/type behavior with audit redaction;
13. physical Esc×2 emergency stop and local Resume requirement;
14. action ID/duration/terminal-state timeline without sensitive payloads;
15. BricsCAD autosave/BAK policy plus versioned backup/recovery-to-copy;
16. one disposable CAD mutation, audit, save/reopen and clean shutdown.

Do not record credentials, OAuth/static bearer tokens, private DWG paths/content, clipboard contents, typed secrets or unsanitized screenshots in committed evidence.
