# MCP Agent Center tabs, toast, and System/Dark/Light theme design

**Issue / Lane-Key:** #4623 / `issue-4623`  
**Baseline:** `main@d3e6e4d6e9f423efbd2d236d600110a6a2ced1f5`  
**Owner-approved direction:** tabbed MCP operations center, toast notifications, and a theme selector with `System` as the default plus explicit `Dark` and `Light` overrides.

## 1. Problem statement

The current `McpAgentControlCenterWindow` is a single scrollable two-column dashboard. Cloudflare setup, ChatGPT connector actions, Agent controls, system status, and the fixed recent-activity panel all compete at the same visual level. This makes the window harder to scan and pushes important actions below the fold.

The current programmatic WPF buttons set `Background`, `BorderBrush`, and `Foreground`, but they rely on the platform `Button` control template for interaction states. On Windows/WPF themes this can cause hover/pressed/focus state colors to override the intended palette and reduce text/background contrast. The fixed "Hoạt động gần nhất" panel also consumes permanent space for feedback that should normally be transient.

This redesign must reduce cognitive load without changing MCP, Cloudflare, or Agent semantics.

## 2. Goals

1. Make the primary flow understandable at a glance.
2. Separate setup, connector, Agent control, and logs into distinct navigation surfaces.
3. Use `System` theme mode by default and support explicit `Dark` and `Light` overrides.
4. Follow Windows app-theme changes while the window is open when `System` mode is selected.
5. Replace the fixed recent-activity panel with in-window toast notifications.
6. Retain a bounded activity history in a dedicated Logs tab.
7. Own all normal/hover/pressed/focus/disabled button colors in QS3D so WPF system styling cannot produce unreadable combinations.
8. Preserve existing actions, safety semantics, threading behavior, and public endpoint resolution.
9. Remain a BricsCAD-hosted V25 WPF plugin window with no new terminal/shell dependency.

## 3. Non-goals

- No changes to MCP transport, authentication, session lifecycle, tool schemas, mutation confirmation, audit semantics, or CAD execution safety.
- No changes to Cloudflare credential ownership or browser-login behavior.
- No arbitrary PowerShell, `cmd.exe`, shell, process-launch, desktop-control, or screenshot capability.
- No standalone QS3D desktop application shell.
- No persistent cross-machine settings service.
- No V26-specific UI redesign in this lane unless current compile composition proves the same source must be shared.
- No claim of licensed BricsCAD rendering/HiDPI runtime PASS from hosted CI.

## 4. Chosen architecture

### 4.1 One window, five top-level tabs

Keep one modal `McpAgentControlCenterWindow`, but replace the four equally weighted dashboard cards with a compact top navigation row and one active content page.

Tabs:

1. **Tổng quan** — connection summary, quick actions, and a short getting-started path.
2. **Cloudflare** — install/login/tunnel lifecycle and Cloudflare-specific state.
3. **ChatGPT Connector** — open ChatGPT, copy connection values, protocol check, and read-only self-test.
4. **Điều khiển Agent** — emergency/cancel/resume/audit actions with an isolated danger zone.
5. **Logs** — bounded in-window activity history, newest first.

The tabs are implemented as QS3D-owned navigation buttons plus a content host rather than relying on the default WPF `TabItem` visual template. This keeps hover/pressed/selected colors deterministic across Windows themes and is simpler to theme in a programmatic-WPF file.

### 4.2 Shell structure

The root is a `Grid` with two layers:

- main layer: header + status chips + navigation + active page + compact footer;
- overlay layer: right-aligned toast host with a higher Z-index.

The active page remains inside a vertical `ScrollViewer`. Horizontal scrolling is disabled.

The selected tab index is stored on the window so a theme rebuild preserves navigation context.

## 5. Theme model

### 5.1 Modes

Define a local enum:

```text
ThemeMode.System
ThemeMode.Dark
ThemeMode.Light
```

Initial mode is always `System` for a newly opened Agent Center window.

The header exposes a compact three-way selector: `System | Dark | Light`.

An explicit override lasts for the current Agent Center window/session. The redesign intentionally does not add a persistent configuration file merely to remember this UI preference.

### 5.2 System resolution

For `ThemeMode.System`, resolve the current Windows app theme using the standard per-user personalization value:

`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`

- `0` => dark palette;
- non-zero => light palette;
- missing/unreadable => fail soft to light palette.

When `System` is selected, subscribe to Windows user-preference changes while the window is alive. Dispatch any UI refresh back to the WPF Dispatcher and unsubscribe on close. A system-theme event rebuilds the visual shell only when the logical mode is still `System`.

### 5.3 Semantic palette

Use a `ThemePalette` object rather than scattered global brush constants. It contains semantic values for:

- window background;
- elevated/card background;
- subtle/hover surface;
- strong and subtle borders;
- primary/secondary/muted text;
- accent foreground/background/hover/pressed;
- success foreground/background/border;
- warning foreground/background/border;
- danger foreground/background/hover/pressed/border;
- disabled foreground/background/border;
- focus ring;
- toast info/success/warning/error surfaces.

Light and dark palettes provide the same semantic roles. Components consume roles, not hard-coded assumptions about light/dark colors.

### 5.4 Theme application strategy

Because this UI is generated programmatically and must run on `net48`, theme changes rebuild the Agent Center visual shell from current state instead of trying to mutate every nested brush in place.

State that survives a rebuild:

- selected tab index;
- logical theme mode;
- bounded activity history;
- current MCP/tunnel state from `RefreshStatus()`.

Transient toast visuals do not need to survive a theme rebuild; the theme action itself emits a fresh informational toast after the new shell is mounted.

## 6. Button interaction contract

### 6.1 Button kinds

Extend the visual hierarchy to explicit kinds:

- `Primary` — one main action per section;
- `Secondary` — normal operations;
- `Danger` — emergency/destructive controls;
- `Utility` — compact header/footer controls;
- `Navigation` — top tab selectors;
- `ThemeChoice` — System/Dark/Light selector.

### 6.2 QS3D-owned ControlTemplate

Every action/navigation/theme button uses a custom WPF `ControlTemplate` created in code. The template is a rounded `Border` containing a `ContentPresenter`; it binds to the button's own background, border, padding, alignment, and foreground.

Each style owns triggers for:

- normal;
- `IsMouseOver`;
- `IsPressed`;
- keyboard focus;
- `IsEnabled == false`;
- selected navigation/theme state where applicable.

The style sets both foreground and background for every interactive state. It must never depend on the OS button template to choose hover/pressed text colors.

Focus is represented with a deliberate visible border/focus ring rather than the default theme adorner.

Disabled state retains readable contrast and does not reuse hover/pressed colors.

### 6.3 Interaction safety

Button styling changes presentation only. Existing click handlers and mutation semantics remain unchanged. Emergency Stop and ESC/cancel continue to bypass the serialized read-only-operation slot as they do today.

## 7. Page designs

### 7.1 Tổng quan

Two-column responsive card layout when space permits, naturally stacking through the existing scroll container at smaller heights.

**Connection summary card**
- MCP embedded state;
- tunnel state;
- public MCP URL readiness;
- local endpoint;
- current public URL when available.

**Bắt đầu nhanh card**
- Cài / cập nhật Cloudflare Tunnel;
- Đăng nhập Cloudflare + tạo Named Tunnel;
- Mở ChatGPT;
- Copy MCP URL.

A short 1 → 2 → 3 helper line explains the preferred production flow without duplicating the full runbook.

### 7.2 Cloudflare

**Thiết lập & đăng nhập**
- install/update cloudflared;
- provider-browser login + Named Tunnel setup.

**Tunnel controls**
- start saved Named Tunnel;
- Quick Tunnel (visibly labeled test-only);
- stop all QS3D tunnels.

**Cloudflare status**
- cloudflared installed;
- browser login/auth state;
- Named Tunnel state;
- Quick Tunnel state;
- public MCP endpoint.

### 7.3 ChatGPT Connector

**Connector actions**
- open ChatGPT;
- copy MCP URL;
- copy bearer token;
- copy URL + Authorization.

**Validation**
- MCP protocol check;
- read-only Agent self-test.

**Current connection**
- canonical public URL or a clear not-ready message;
- local endpoint;
- security hint that bearer token is sensitive and should not be published.

The token itself is never rendered into the UI status surface.

### 7.4 Điều khiển Agent

Split visually into two sections.

**Khẩn cấp**
- `EMERGENCY STOP AGENT` as a high-visibility danger action;
- cancel current BricsCAD command (`ESC x2`).

**Phục hồi & audit**
- Resume Agent;
- open MCP audit folder.

Danger actions remain directly accessible and are not disabled merely because a serialized protocol/self-test operation is active.

### 7.5 Logs

Show a bounded list of recent UI activity entries, newest first. Each entry contains:

- local timestamp;
- severity (`Info`, `Success`, `Warning`, `Error`);
- short title/message.

Keep at most 50 history entries in memory for the window lifetime. This is UI activity history, not a replacement for the existing MCP audit log.

## 8. Toast notification system

### 8.1 Host

Create `_toastHost` as a right-aligned vertical stack in the root overlay layer. Toasts never affect normal document layout or push content downward.

### 8.2 Toast kinds

- `Info`
- `Success`
- `Warning`
- `Error`

Each toast has:

- semantic title;
- short wrapped message;
- close button;
- palette-specific surface/border/text treatment.

### 8.3 Lifetime

Default lifetimes:

- Info: ~4 seconds;
- Success: ~4 seconds;
- Warning: ~7 seconds;
- Error: ~8 seconds.

A caller can request sticky behavior for a message that must remain until explicitly dismissed.

Keep at most four visible toast cards. Adding a fifth dismisses the oldest visible toast while retaining its history entry.

Timers are `DispatcherTimer` instances owned by the window. All timers are stopped and detached when the window closes.

### 8.4 Activity migration

Replace the fixed `_activity.Text` feedback model with `ShowToast(...)` plus `AddActivityEntry(...)`.

Examples:

- install starts => Info toast;
- install succeeds => Success toast;
- install fails => Error toast;
- Quick Tunnel waiting => Info toast;
- public URL becomes available => Success toast;
- copy URL/token/config => Success toast;
- missing public URL => Warning toast;
- protocol/self-test result => Success or Error based on result text/exception;
- overlapping serialized local check => Warning toast;
- open audit folder exception => Error toast.

Routine Agent Center feedback should not use blocking `MessageBox.Show`. The separate Cloudflare account setup window may keep its own interaction model; this lane does not redesign that window.

## 9. Async and error behavior

Existing worker-thread behavior is preserved:

- install callback returns through Dispatcher;
- protocol/self-test/control operations use `ThreadPool.QueueUserWorkItem`;
- serialized observation checks continue to use `_localOperationActive`;
- emergency stop/cancel remain callable while observation is busy.

`RunLocalOperation` will classify completion for toast presentation:

- thrown exception => Error;
- returned result containing known failure prefixes such as `FAIL`/`ERROR` => Error;
- otherwise => Success.

Status is refreshed after completion in all cases.

No toast logic is allowed to swallow the underlying operation result from the Logs history.

## 10. Status model

`RefreshStatus()` remains the single point that reads current MCP/Cloudflare state. It updates:

- header chips;
- whichever status containers exist on the active page;
- overview/connector endpoint summaries.

Theme or tab navigation rebuilds call `RefreshStatus()` after mounting the new page.

Public endpoint always comes from `McpPublicEndpointResolver.Resolve()`; the redesign does not invent a second endpoint source.

## 11. Accessibility and layout

- Keep `UseLayoutRounding` and `SnapsToDevicePixels`.
- Maintain keyboard focus visibility on every actionable button.
- Use minimum button heights of roughly 38 px and 44 px for danger/emergency controls.
- Keep foreground/background contrast explicit in every button state.
- Avoid relying on color alone for online/offline state; status chips retain text labels and a dot marker.
- Keep window minimum dimensions compatible with the current BricsCAD-hosted modal workflow.
- All long status values and URLs wrap rather than forcing horizontal scrolling.
- Toast messages wrap and have a bounded maximum width.

## 12. Source structure

Keep this lane focused on the existing source file rather than introducing a new WPF/XAML subsystem.

`McpAgentControlCenter.cs` will gain focused helpers/types such as:

- `ThemeMode`
- `ThemePalette`
- `ToastKind`
- `ActivityEntry`
- `CreateDashboardShell()` / `CreateHeaderBar()`
- `CreateTabNavigation()`
- `CreateOverviewPage()`
- `CreateCloudflarePage()`
- `CreateConnectorPage()`
- `CreateAgentControlPage()`
- `CreateLogsPage()`
- `CreateActionButton()` with QS3D-owned style/template
- `CreateButtonStyle()`
- `SetSelectedTab(...)`
- `SetThemeMode(...)`
- `ResolveSystemTheme()`
- `ApplyThemeAndRebuild(...)`
- `ShowToast(...)`
- `AddActivityEntry(...)`
- `DismissToast(...)`

The exact helper names may vary slightly during implementation, but the behavior and boundaries above are contract-level requirements.

## 13. Preflight contract

Update `scripts/preflight-mcp-agent-center-uiux.py` before production implementation so the existing source guard specifies the new behavior.

The guard should require source evidence for:

- System/Dark/Light mode enum or equivalent;
- Windows system-theme resolution;
- five named navigation destinations;
- toast host and toast kinds;
- bounded activity history;
- custom button `ControlTemplate`;
- explicit hover, pressed, focus, and disabled triggers/state handling;
- removal of the fixed `CreateActivityPanel()` contract;
- preserved existing MCP actions and worker-thread semantics;
- no `powershell.exe`, `cmd.exe`, or `System.Windows.Forms` dependency.

The preflight remains a source contract, not pixel-perfect screenshot validation.

## 14. Validation strategy

### Remote/source-safe

1. Run the updated `scripts/preflight-mcp-agent-center-uiux.py` in RED before production implementation.
2. Implement the UI changes.
3. Run the focused UIUX preflight to GREEN.
4. Run relevant aggregate/source preflight selected by repository policy.
5. Build the V25 project using the repository's trusted managed-reference path when available through CI.
6. Push the exact canonical branch head and require shared branch CI `preflight` + `core` success before opening the PR.
7. Reconcile current `main` non-force if it moved, then revalidate the new exact head.
8. Open one canonical PR with the lane metadata and require protected current-candidate checks before merge.

### LOCAL_ONLY

Real BricsCAD rendering, actual Windows theme switching inside the licensed host, DPI behavior, and mouse-level visual inspection are LOCAL_ONLY evidence. Hosted CI must not claim those runtime checks were executed.

The source design should nevertheless be testable statically: state colors and templates are owned by QS3D rather than inherited from OS button visuals.

## 15. Acceptance criteria

The lane is source-complete when all of the following are true:

- the old four-card flat dashboard is replaced by the five-destination tab/navigation model;
- the default logical theme is System;
- Dark and Light can be selected explicitly;
- System mode resolves Windows app theme and responds to user-preference changes while the window is open;
- button foreground/background/border states are QS3D-owned for hover, pressed, focus, disabled, and selected navigation/theme states;
- the fixed "Hoạt động gần nhất" panel is gone;
- routine operation feedback appears as in-window toasts;
- Logs retains bounded activity history;
- Emergency Stop/cancel remain obvious and callable during a serialized self-test;
- the existing MCP/Cloudflare/connector actions still call the same underlying managers/runtime functions;
- source guard passes;
- exact branch CI and protected PR checks are green before merge;
- no unsupported LOCAL_PASS claim is made.

## 16. Alternatives considered

### Keep the two-column dashboard and only recolor it

Rejected. It would address cosmetics but not the information hierarchy or the permanent activity panel that the owner explicitly found confusing.

### Native WPF `TabControl` with default templates

Rejected. The default `TabItem` and `Button` templates reintroduce platform-theme state behavior that this task specifically needs to control. A small QS3D-owned navigation surface is more deterministic in the existing programmatic-WPF architecture.

### XAML/resource-dictionary migration

Deferred. XAML could make theme definitions cleaner, but introducing a new resource compilation surface expands this UI-only lane and raises packaging/build risk. The current file is already the canonical Agent Center implementation, and programmatic styles/templates can satisfy this request without changing project architecture.

### Persist theme override across BricsCAD sessions

Deferred. The owner requested System mode plus Dark/Light choices, not a new settings persistence contract. Defaulting each new Agent Center to System is predictable and avoids hidden configuration state.