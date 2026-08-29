# MCP Agent Center Tabs, Toast, and Theme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the BricsCAD V25 MCP Agent Center into a five-destination tabbed operations center with toast notifications and System/Dark/Light theming while preserving MCP/Cloudflare/Agent semantics.

**Architecture:** Keep the existing programmatic WPF window and business handlers in `McpAgentControlCenter.cs`. Replace the flat dashboard with a layered shell, QS3D-owned navigation buttons, semantic theme palettes, custom button templates/state triggers, a toast overlay, and a bounded in-memory activity log. Source-contract preflight is changed first and must fail before production code is changed.

**Tech Stack:** C# / .NET Framework 4.8, WPF programmatic controls/templates, Microsoft.Win32 system-theme registry, Microsoft.Win32.SystemEvents, Python source preflight, GitHub shared branch/PR CI.

**Spec:** `docs/superpowers/specs/2026-08-29-mcp-agent-center-tabs-toast-theme-design.md`

## Global Constraints

- The shipping target remains the BricsCAD V25 hosted plugin, not a standalone application.
- Initial logical theme mode is `System`; explicit `Dark` and `Light` overrides last for the current window session.
- Preserve existing Cloudflare install/login/tunnel semantics, canonical public endpoint resolution, protocol probe, read-only self-test, emergency stop/cancel/resume, and worker-thread behavior.
- Do not add PowerShell, cmd.exe, System.Windows.Forms, arbitrary shell/process execution, or credential exposure.
- Real BricsCAD visual/HiDPI qualification remains LOCAL_ONLY unless actually executed on the licensed Windows host.

---

### Task 1: Specify the new Agent Center source contract (TDD RED)

**Files:**
- Modify: `scripts/preflight-mcp-agent-center-uiux.py`
- Test target: `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`

**Interfaces:**
- Consumes: current Agent Center source.
- Produces: a source contract that requires the approved tabs/theme/toast/button-state design and preserves existing MCP actions.

- [ ] **Step 1: Replace old flat-dashboard requirements with the new contract**

Require source tokens for `ThemeMode`, `System`, `Dark`, `Light`, `AppsUseLightTheme`, `SystemEvents.UserPreferenceChanged`, five page labels/builders, `_toastHost`, `ToastKind`, `MaxActivityEntries = 50`, `ControlTemplate`, `IsMouseOver`, `IsPressed`, `IsKeyboardFocused`, `IsEnabled`, and `ShowToast`. Preserve requirements for install/account/public-endpoint/protocol/self-test/emergency/cancel/resume/ThreadPool.

Add a forbidden legacy token check for `CreateActivityPanel()` and keep forbidden terminal/UI dependencies.

- [ ] **Step 2: Commit the test-only change**

Commit message: `test(mcp): specify tabbed themed Agent Center contract`.

- [ ] **Step 3: Verify RED on the exact test-only branch head**

Run through automatic branch CI/source preflight. Expected: `preflight` fails specifically because current production source lacks the new theme/tab/toast/template contract. Confirm the failure is not reservation metadata, syntax, or unrelated CI damage.

---

### Task 2: Implement System/Dark/Light theme and deterministic button states

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`
- Test: `scripts/preflight-mcp-agent-center-uiux.py`

**Interfaces:**
- Consumes: `ThemeMode`, current WPF action handlers.
- Produces: `ThemePalette`, `ResolveEffectiveDarkTheme()`, `SetThemeMode(ThemeMode)`, `ApplyThemeAndRebuild()`, `CreateButtonStyle(...)`, custom `ControlTemplate`.

- [ ] **Step 1: Add theme state/types and Windows system-theme resolution**

Add `ThemeMode { System, Dark, Light }`, a semantic `ThemePalette` with light/dark factories, logical mode initialized to `System`, registry lookup at `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize` / `AppsUseLightTheme`, and `SystemEvents.UserPreferenceChanged` subscription/unsubscription scoped to the window lifetime.

- [ ] **Step 2: Add QS3D-owned button template/style**

Create a rounded Border + ContentPresenter `ControlTemplate`. Add explicit triggers for `Button.IsMouseOverProperty`, `Button.IsPressedProperty`, `Button.IsKeyboardFocusedProperty`, and `Button.IsEnabledProperty == false`. Set foreground/background/border in every state. Add selected-state styling for navigation/theme-choice buttons without relying on OS chrome.

- [ ] **Step 3: Add the System/Dark/Light selector to the header**

Render three compact theme-choice buttons and rebuild the shell after selection while retaining current selected page/history. In `System`, respond to Windows user-preference changes; explicit Dark/Light ignores later system-theme events.

- [ ] **Step 4: Run focused preflight**

Expected: theme/button requirements now pass; tab/toast requirements may remain failing until Task 3.

---

### Task 3: Replace the flat dashboard with five navigation destinations and toast/history UI

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`
- Test: `scripts/preflight-mcp-agent-center-uiux.py`

**Interfaces:**
- Consumes: current action handlers, `ThemePalette`, styled button factory.
- Produces: `CreateTabNavigation()`, `CreateOverviewPage()`, `CreateCloudflarePage()`, `CreateConnectorPage()`, `CreateAgentControlPage()`, `CreateLogsPage()`, `ShowToast(...)`, `AddActivityEntry(...)`.

- [ ] **Step 1: Build the layered shell and navigation**

Use a root `Grid` with main content plus high-Z right-aligned `_toastHost`. Main content contains header/status chips, navigation, an active-page host, and compact footer. Keep vertical scrolling and disable horizontal scrolling.

- [ ] **Step 2: Implement the five pages**

`Tổng quan`: connection summary + quick path. `Cloudflare`: setup/tunnel actions + state. `ChatGPT Connector`: copy/open/test actions + safe endpoint summary. `Điều khiển Agent`: isolated emergency section plus recovery/audit. `Logs`: newest-first activity history.

- [ ] **Step 3: Add bounded activity history**

Add `MaxActivityEntries = 50`, timestamp/severity/message entries, trimming after insertion, and logs page rendering. Never render the bearer token itself.

- [ ] **Step 4: Add toast overlay behavior**

Add Info/Success/Warning/Error types, close action, bounded width, maximum four visible cards, automatic DispatcherTimer dismissal (4/4/7/8 seconds), and close-time timer cleanup.

- [ ] **Step 5: Remove the fixed recent-activity panel**

Delete `_activity` UI field usage and `CreateActivityPanel()` from the production window.

- [ ] **Step 6: Run focused preflight**

Expected: new visual-architecture contract passes while preserved behavior checks remain green.

---

### Task 4: Migrate operation feedback to toast/history without changing semantics

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`
- Test: `scripts/preflight-mcp-agent-center-uiux.py`

**Interfaces:**
- Consumes: existing InstallCloudflared/OpenAccountSetup/StartNamedTunnel/StartQuickTunnel/StopTunnels/Copy*/CheckProtocol/RunReadOnlySelfTest/InvokeControlTool/OpenAuditFolder handlers.
- Produces: toast/history feedback for every routine Agent Center operation.

- [ ] **Step 1: Convert install/tunnel feedback**

Use Info while pending, Success on successful state changes/public URL, Warning for not-ready conditions, and Error for exceptions/failures. Refresh status after state-changing callbacks exactly as before.

- [ ] **Step 2: Convert copy/connector feedback**

Copy actions show success toast/history; missing public URL shows warning instead of a routine blocking MessageBox. Bearer secret is copied but never displayed/logged.

- [ ] **Step 3: Convert local-operation feedback**

Keep `_localOperationActive` serialization and `ThreadPool.QueueUserWorkItem`. Emergency stop/cancel continue to bypass the serialized observation slot. Classify thrown exceptions or returned `FAIL`/`ERROR` prefixes as Error; otherwise Success; retain full returned result in bounded UI history.

- [ ] **Step 4: Convert audit/open errors**

Use error toast/history for Agent Center failures while preserving the underlying path/open behavior.

- [ ] **Step 5: Run focused preflight and inspect source diff**

Expected: `PASS MCP Agent Center UIUX contract`; no MCP transport/tool semantics changed; only reserved paths differ from baseline.

---

### Task 5: Validate, reconcile, PR, and merge

**Files:**
- Validate all changed reserved paths.

**Interfaces:**
- Consumes: exact canonical branch head.
- Produces: green exact-head branch CI, current protected PR candidate, and merged `main`.

- [ ] **Step 1: Push final coherent implementation head and wait for exact-head branch CI**

Required: shared branch `preflight` and `core` SUCCESS on the exact current SHA. If red, diagnose and fix the same carrier before proceeding.

- [ ] **Step 2: Refresh current `main` and compare scope**

If main moved, reconcile the canonical branch non-force, preserving all intended changes and avoiding unrelated carriers. Re-run exact-head branch CI after any new head.

- [ ] **Step 3: Open one canonical PR**

PR metadata must include `Lane-Key: issue-4623`, canonical owner/session, canonical carrier, and `Supersedes: none`.

- [ ] **Step 4: Require protected current-candidate checks**

Wait for protected `preflight` and `core` SUCCESS on the current candidate; re-sync/revalidate if strict freshness requires it.

- [ ] **Step 5: Merge the same task PR and verify resulting main SHA**

Use the owner standing same-task merge authorization. Do not bypass protection. Update Issue #4623 to `COMPLETED` / `MERGED_MAIN` with exact branch head, PR, CI evidence, resulting main SHA, and LOCAL_ONLY rendering caveat.
