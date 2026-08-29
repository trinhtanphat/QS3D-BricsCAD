# QS3D MCP guided onboarding, desktop control and recovery

Status: SOURCE_TRACKED / `PENDING_LOCAL`  
Issue: #4629  
Parent MCP architecture: #4352  
OAuth integration: #4584 / PR #4597

## End-user goal

A normal user installs/loads QS3D inside BricsCAD, opens **TOOL > MCP (AI) > Agent Center**, follows the guided connection flow, and then uses ChatGPT as the conversation UI while QS3D executes bounded MCP actions inside the BricsCAD/Windows session.

No second MCP repository, Node runtime, PowerShell/CMD setup, ChatGPT password capture, Cloudflare password capture or browser-cookie scraping is part of the normal path.

## Control Center

`QS3DMCPAGENTCENTER` is the canonical local control surface with four tabs:

1. **Kết nối** — embedded MCP, cloudflared, Cloudflare browser login, Named Tunnel, public MCP URL, ChatGPT OAuth registration and protocol verification.
2. **Agent** — local desktop-control consent, current action/next step/recent event timeline, Emergency Stop, CAD cancel and resume.
3. **Backup & khôi phục** — BricsCAD autosave/BAK status, manual versioned snapshot, latest-snapshot recovery to a new file and backup-folder access.
4. **Nâng cao** — Quick Tunnel test fallback, engineering bearer compatibility, read-only self-test and audit-folder access.

The window follows the Windows app light/dark preference and explicitly sets normal/hover/pressed foreground/background states so buttons do not lose contrast when hovered or clicked.

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

## Local desktop consent

The `desktop_*` MCP namespace can cross application boundaries, so network confirmation alone is insufficient.

- desktop mutation still requires top-level `confirmMutation=true`;
- clipboard/screenshot sensitive reads still require `confirmSensitiveRead=true`;
- mutation and sensitive reads additionally require **local desktop consent** enabled by the user in the Agent tab;
- that local consent is memory-only and resets every BricsCAD process start;
- there is no MCP tool that can remotely enable it.

While a guarded desktop action is active, QS3D shows a click-through **blue desktop border/banner** naming the current MCP tool.

A physical **Esc twice within 1.2 seconds**:

1. disables local desktop consent immediately;
2. advances the MCP emergency-stop epoch;
3. hides the control overlay;
4. requests cancellation of the active BricsCAD command;
5. requires a local user to re-enable desktop control before cross-application automation resumes.

## Status interaction with ChatGPT

ChatGPT remains the conversation UI. QS3D does **not** scrape ChatGPT Web or attempt to mirror arbitrary assistant prose.

Instead QS3D maintains a bounded local operational timeline for onboarding, tunnel, MCP action, recovery and error events. The Control Center shows:

- current local action;
- next recommended step;
- recent bounded events;
- embedded MCP/tunnel/desktop-consent status.

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
- no arbitrary remote shell/process-launch tool;
- desktop input limited to visible top-level windows in the current interactive Windows session;
- exact target revalidation before keyboard input;
- bounded cursor/window/text/click/scroll/screenshot/clipboard surfaces;
- typed text, clipboard contents and screenshot pixels are not persisted in the MCP mutation audit;
- local desktop consent cannot be remotely enabled;
- Esc×2 remains a local emergency control;
- recovery does not overwrite the active source DWG automatically.

## LOCAL_ONLY qualification

Source/static/hosted validation cannot prove real Windows desktop input, licensed BricsCAD behavior, Cloudflare login/tunnel or ChatGPT connector behavior.

The exact intended merged/release SHA remains `PENDING_LOCAL` under `LOCAL-024` until a clean Windows + licensed BricsCAD V25/V26 + Cloudflare + ChatGPT pass covers at least:

1. exact V25/V26 plugin load and MCP health;
2. guided four-tab Agent Center and first-run toast;
3. Cloudflare provider-browser login + persistent Named Tunnel;
4. ChatGPT URL + OAuth/DCR connection;
5. `tools/list` including the complete `desktop_*` catalog;
6. read-only CAD and desktop observation;
7. local desktop-consent deny/allow behavior;
8. blue overlay while guarded desktop tools run;
9. clipboard/screenshot sensitive-read gating;
10. desktop mouse/keyboard/window/clipboard-write on disposable content;
11. physical Esc×2 emergency stop and local re-enable requirement;
12. BricsCAD autosave/BAK policy plus versioned backup/recovery-to-copy;
13. one disposable CAD mutation, audit, save/reopen and clean shutdown.

Do not record credentials, OAuth/static bearer tokens, private DWG paths/content, clipboard contents, typed secrets or unsanitized screenshots in committed evidence.
