# QS3D MCP guided onboarding, desktop control and recovery

Status: SOURCE_TRACKED / `PENDING_LOCAL`  
Issue: #4629  
Parent MCP architecture: #4352  
OAuth integration: #4584 / PR #4597

## End-user goal

A normal user installs/loads QS3D inside BricsCAD, opens **TOOL > MCP (AI) > Agent Center**, follows the guided connection flow, and then uses ChatGPT as the conversation UI while QS3D executes bounded MCP actions inside the BricsCAD/Windows session.

No second MCP repository, Node runtime, PowerShell/CMD setup, ChatGPT password capture, Cloudflare password capture or browser-cookie scraping is part of the normal path.

## Completion Pack decision

The owner approved both completion phases for #4629 / PR #4632 and asked that both decisions remain documented for future agents.

### Approach A — selected / source implemented

Approach A keeps each desktop primitive explicit and individually bounded:

- `desktop_mouse_drag` — exact-window bounded drag with continuous target/emergency-stop revalidation;
- `desktop_wait_for_window` — bounded read-only wait for a visible current-session window;
- `desktop_screenshot` optional crop (`cropX`, `cropY`, `cropWidth`, `cropHeight`);
- local desktop consent is process-memory-only and, after an explicit local Resume, stays ON with session-persistent auto-renew until Pause/Emergency Stop/Esc×2/host shutdown;
- explicit local **Pause desktop** / **Resume desktop**;
- local timeline includes bounded Action ID, duration and terminal state;
- Agent Center shows waiting/active/paused/re-enable guidance and recovery hints.

### Approach B — selected / bounded sequence

Approach B adds exactly one higher-level batching surface: **`desktop_sequence`**. It is a bounded convenience layer over the existing primitives, not a generic macro/scripting engine.

- one exact visible current-session `windowHandle` per sequence;
- maximum 12 steps;
- maximum 30 seconds total duration;
- maximum 2000 ms delay after any step;
- fail-fast on first error, no `continueOnError` and no atomic rollback;
- no recursion/nested sequence and no `desktop_macro` alias;
- no shell/process/script/plugin/filesystem dispatch;
- no clipboard read inside sequence;
- optional screenshot is forced to the bound target window, limited to one per sequence and requires outer `confirmSensitiveRead=true` before execution;
- Esc×2, Pause, consent revocation, target invalidation or mutation-epoch change aborts subsequent execution;
- partial completion is explicit in result/audit.

When a workflow needs a new dialog/application handle, ChatGPT obtains that handle with normal observation/wait tools and submits a new sequence. One sequence never silently switches target windows.

## Control Center

`QS3DMCPAGENTCENTER` is the canonical local control surface with four tabs:

1. **Kết nối** — embedded MCP, cloudflared, Cloudflare browser login, Named Tunnel, public MCP URL and ChatGPT OAuth registration.
2. **Agent** — local desktop-control state, AUTO-RENEW/session state, Pause/Resume, Action ID/duration/result, current action/next step, Emergency Stop and CAD cancel.
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

## Desktop tool surface — Approaches A + B

The current desktop surface is **15 tools**: 14 explicit Approach-A primitives plus the bounded `desktop_sequence` tool from Approach B.

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
- `desktop_sequence`

Sensitive reads:

- `desktop_clipboard_read`
- `desktop_screenshot`

`desktop_wait_for_window` never focuses/clicks. Its timeout is bounded to 15 seconds and it only observes visible top-level windows in the current interactive Windows session.

Click/scroll/drag require an exact `windowHandle`; the target must still be visible/current-session, coordinates must remain inside current target bounds, and foreground ownership is revalidated immediately around input injection. Drag duration is bounded and the target/emergency-stop state is rechecked throughout the gesture.

Screenshot crop is relative to the selected source (window or virtual desktop). If any crop field is supplied, all four crop fields are required; invalid/empty intersections are rejected before capture, and the normal screenshot dimension/payload caps still apply.

### `desktop_sequence` request shape

The top-level MCP argument object stays flat. `stepsJson` therefore contains the step array as a string; each step's `arguments` is also a flat JSON-object string.

```json
{
  "windowHandle": "1A02BC",
  "stepsJson": "[{\"tool\":\"desktop_mouse_click\",\"arguments\":\"{\\\"x\\\":500,\\\"y\\\":300,\\\"button\\\":\\\"left\\\"}\",\"delayAfterMs\":100},{\"tool\":\"desktop_key\",\"arguments\":\"{\\\"key\\\":\\\"TAB\\\"}\"}]",
  "maxDurationMs": 15000,
  "confirmMutation": true
}
```

Sequence step arguments cannot inject `windowHandle`, `confirmMutation` or `confirmSensitiveRead`; QS3D owns those security-sensitive properties. A screenshot step additionally requires outer `confirmSensitiveRead=true` and is forced to `scope=window` for the sequence target.

Allowed sequence steps are focus, mouse move/click/scroll/drag, type, key/hotkey, clipboard write, wait for the same target and target-window screenshot. `desktop_clipboard_read`, nested sequence, generic observation tools, CAD/QS3D/plugin dispatch, filesystem and shell/process/script operations are rejected.

## Local desktop consent

The `desktop_*` MCP namespace can cross application boundaries, so network confirmation alone is insufficient.

- desktop mutation still requires top-level `confirmMutation=true`;
- clipboard/screenshot sensitive reads still require `confirmSensitiveRead=true`;
- mutation and sensitive reads additionally require **local desktop consent** enabled by the user in the Agent tab;
- that consent is memory-only and resets every BricsCAD process start;
- after an explicit local Resume it stays `ON` with AUTO-RENEW for the current BricsCAD process and does not expire because of idle time;
- user can explicitly **Pause desktop** and **Resume desktop** locally;
- Emergency Stop, physical Esc×2, or BricsCAD/QS3D shutdown revoke it immediately;
- there is no MCP tool that can remotely enable/resume local consent.

While a guarded desktop action or sequence is active, QS3D shows a click-through **blue desktop border/banner** naming the current MCP tool and its bounded Action ID.

A physical **Esc twice within 1.2 seconds**:

1. changes desktop control to a stopped/paused state immediately;
2. advances the MCP emergency-stop epoch;
3. hides the control overlay;
4. requests cancellation of the active BricsCAD command;
5. prevents subsequent sequence steps/input;
6. requires a local user to Resume desktop before cross-application automation resumes.

After `PAUSED`, Emergency Stop, cancellation or failure, the Agent tab tells the user to **Kiểm tra drawing/backup** before resuming desktop control.

## Status interaction with ChatGPT

ChatGPT remains the conversation UI. QS3D does **not** scrape ChatGPT Web or attempt to mirror arbitrary assistant prose.

Instead QS3D maintains a bounded local operational timeline for onboarding, tunnel, MCP action/sequence, recovery and error events. Desktop action entries can contain:

- bounded Action ID;
- UTC start/end-derived duration;
- terminal state (`running`, `success`, `failed`, `cancelled`);
- current action and next recommended step;
- bounded sequence step index/tool/completed-count/duration metadata.

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
- no arbitrary remote shell/process/script launch or unrestricted plugin dispatch;
- one bounded `desktop_sequence`; no `desktop_macro` alias or nested sequence;
- no `desktop_clipboard_read` inside sequence;
- sequence is one target window, <=12 steps, <=30 seconds, <=2000 ms per delay and fail-fast;
- sequence screenshot is target-window-only and requires explicit sensitive-read acknowledgement before execution;
- desktop input limited to visible top-level windows in the current interactive Windows session;
- exact target/foreground revalidation for targeted mouse/keyboard interaction;
- bounded cursor/window/text/click/scroll/drag/wait/screenshot/clipboard surfaces;
- single alphanumeric `desktop_key` values are audited as non-content `CHARACTER`, not the caller character;
- typed text, clipboard contents and screenshot pixels are not persisted in MCP mutation audit;
- local desktop consent cannot be remotely enabled/resumed;
- Esc×2 remains a local emergency control and aborts future sequence steps;
- recovery does not overwrite the active source DWG automatically.

## LOCAL_ONLY qualification

Source/static/hosted validation cannot prove real Windows desktop input, licensed BricsCAD behavior, Cloudflare login/tunnel or ChatGPT connector behavior.

The exact intended merged/release SHA remains `PENDING_LOCAL` under `LOCAL-024` until a clean Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT pass covers at least:

1. exact V25/V26 plugin load and MCP health;
2. guided four-tab Agent Center and first-run toast;
3. Cloudflare provider-browser login + persistent Named Tunnel;
4. ChatGPT URL + OAuth/DCR connection;
5. `tools/list` including all 15 current desktop tools and no `desktop_macro` alias;
6. read-only CAD plus cursor/window observation;
7. `desktop_wait_for_window` success + timeout behavior;
8. local desktop-consent OFF rejection, local Resume/Pause, AUTO-RENEW remaining ON beyond 10 minutes of idle time, and explicit revocation by safety controls;
9. blue overlay with Action ID while guarded desktop tools/sequence run;
10. clipboard/screenshot sensitive-read gating plus cropped screenshot on disposable content;
11. exact-window click/scroll and bounded drag on disposable content;
12. desktop keyboard/type behavior with audit redaction;
13. bounded single-target `desktop_sequence` success on disposable UI;
14. sequence rejection for >12 steps, >30 seconds, target injection/switching, nested sequence, clipboard read and screenshot without sensitive acknowledgement;
15. physical Esc×2 mid-sequence with fail-fast partial-completion reporting and local Resume requirement;
16. action ID/duration/terminal-state timeline without sensitive payloads;
17. BricsCAD autosave/BAK policy plus versioned backup/recovery-to-copy;
18. one disposable CAD mutation, audit, save/reopen and clean shutdown.

Do not record credentials, OAuth/static bearer tokens, private DWG paths/content, clipboard contents, typed secrets or unsanitized screenshots in committed evidence.
