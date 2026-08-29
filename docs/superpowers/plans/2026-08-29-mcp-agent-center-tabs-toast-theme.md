# MCP Agent Center Tabs, Toast, and Theme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. This file now records the actual execution state after PR #4634 plus the bounded post-merge audit follow-up #4647.

**Goal:** Redesign the BricsCAD V25 MCP Agent Center into a five-destination tabbed operations center with toast notifications and System/Dark/Light theming while preserving MCP/Cloudflare/Agent semantics.

**Architecture:** Keep the existing programmatic WPF window and business handlers in `McpAgentControlCenter.cs`. Replace the flat dashboard with a layered shell, QS3D-owned navigation buttons, semantic theme palettes, custom button templates/state triggers, a toast overlay, and a bounded in-memory activity log. Source-contract preflight is changed first and must fail before production code is changed.

**Tech Stack:** C# / .NET Framework 4.8, WPF programmatic controls/templates, Microsoft.Win32 system-theme registry, Microsoft.Win32.SystemEvents, Python source preflight, GitHub shared branch/PR CI.

**Spec:** `docs/superpowers/specs/2026-08-29-mcp-agent-center-tabs-toast-theme-design.md`

## Execution evidence

- Original source-contract TDD RED was established before the #4623 production implementation.
- Final #4623 canonical branch head: `22c58449a777642db86c518a570424b86f8bc702`.
- Final exact-head branch CI: run `33254203722` — `preflight` SUCCESS; Core build SUCCESS; deterministic smoke SUCCESS; trusted V25 references SUCCESS; V25 plugin build SUCCESS; final Build SUCCESS.
- Final protected/current-candidate PR CI: run `33254205416` — the same required gates SUCCESS.
- PR #4634 merged to `main` as `6976e0d6b2117cc3b98e5c10615569876600fba1`.
- Issue #4623 is `COMPLETED / MERGED_MAIN`.
- Post-merge audit #4647 found one bounded contract gap: the keyboard-focus trigger owns border/thickness but does not yet explicitly own foreground/background. The production source path is currently reserved earlier by active Issue #4629, so #4647 must not mutate it concurrently. The exact two-setter correction is handed off to #4629; #4647 owns the stricter source guard and close-out docs.
- Licensed BricsCAD rendering/HiDPI/live-theme qualification remains `LOCAL-024 / PENDING_LOCAL`; hosted CI is never `LOCAL_PASS`.

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

- [x] **Step 1: Replace old flat-dashboard requirements with the new contract**

The source contract requires `ThemeMode`, `System`, `Dark`, `Light`, `AppsUseLightTheme`, `SystemEvents.UserPreferenceChanged`, five page labels/builders, `_toastHost`, `ToastKind`, `MaxActivityEntries = 50`, `ControlTemplate`, hover/pressed/focus/disabled triggers, and `ShowToast`. Existing install/account/public-endpoint/protocol/self-test/emergency/cancel/resume/ThreadPool behavior remains guarded. Legacy `CreateActivityPanel()` and blocking Agent Center `MessageBox.Show` are forbidden.

- [x] **Step 2: Commit the test-only change**

The initial #4623 contract was committed test-first before production implementation.

- [x] **Step 3: Verify RED on the exact test-only branch head**

The original #4623 source contract was proven RED for the newly required UI architecture before production implementation; reservation/governance failures were not accepted as RED evidence.

---

### Task 2: Implement System/Dark/Light theme and deterministic button states

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`
- Test: `scripts/preflight-mcp-agent-center-uiux.py`

- [x] **Step 1: Add theme state/types and Windows system-theme resolution**

Implemented `ThemeMode { System, Dark, Light }`, semantic palettes, System default, `AppsUseLightTheme` lookup, and scoped `SystemEvents.UserPreferenceChanged` handling.

- [ ] **Step 2: Complete QS3D-owned button-state color ownership — FOLLOW-UP #4647 / SOURCE OWNER #4629**

The rounded QS3D `ControlTemplate`, normal/hover/pressed/disabled states, selected navigation/theme states and focus border are implemented and merged. Post-merge review found one remaining deterministic-state detail: `Button.IsKeyboardFocusedProperty` must also explicitly set `Control.BackgroundProperty` to computed `background` and `Control.ForegroundProperty` to computed `foreground`, in addition to `_palette.FocusBorder` and focus thickness. Active Issue #4629 reserved `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs` earlier, so this two-setter production correction is owned by #4629 and #4647 must not overwrite it.

- [x] **Step 3: Add the System/Dark/Light selector to the header**

Three compact theme-choice buttons are present; System responds to Windows theme events while explicit Dark/Light ignores later system-theme changes.

- [x] **Step 4: Run focused preflight for the original #4623 contract**

The #4623 UIUX source guard passed before PR #4634 merged. A stricter post-merge focus-color guard is being established under #4647 and must not be called GREEN until the #4629-owned source correction lands.

---

### Task 3: Replace the flat dashboard with five navigation destinations and toast/history UI

- [x] **Step 1: Build the layered shell and navigation**

Root Grid + high-Z right-aligned toast host, header/status chips, navigation, active-page host, vertical scrolling and compact footer are implemented.

- [x] **Step 2: Implement the five pages**

`Tổng quan`, `Cloudflare`, `ChatGPT Connector`, `Điều khiển Agent`, and `Logs` are implemented.

- [x] **Step 3: Add bounded activity history**

`MaxActivityEntries = 50`, newest-first timestamp/severity/message history and trimming are implemented; bearer-token values are not rendered into history.

- [x] **Step 4: Add toast overlay behavior**

Info/Success/Warning/Error, close action, bounded width, max four visible cards, DispatcherTimer lifetimes (4/4/7/8 seconds), and timer cleanup are implemented.

- [x] **Step 5: Remove the fixed recent-activity panel**

Legacy `_activity` / `CreateActivityPanel()` behavior was removed from the Agent Center window.

- [x] **Step 6: Run focused preflight**

The #4623 visual-architecture contract passed before protected merge.

---

### Task 4: Migrate operation feedback to toast/history without changing semantics

- [x] **Step 1: Convert install/tunnel feedback**
- [x] **Step 2: Convert copy/connector feedback**
- [x] **Step 3: Convert local-operation feedback while preserving serialization/emergency bypass**
- [x] **Step 4: Convert audit/open errors**
- [x] **Step 5: Run focused preflight and inspect source diff**

PR #4634 preserved the existing MCP/Cloudflare/Agent calls while replacing routine Agent Center feedback with toast/history UI. Protected CI passed on the exact final candidate.

---

### Task 5: Validate, reconcile, PR, and merge

- [x] **Step 1: Push final coherent implementation head and obtain exact-head branch CI** — run `33254203722` SUCCESS.
- [x] **Step 2: Refresh/reconcile current `main` non-force and rerun exact-head CI** — final head `22c58449a777642db86c518a570424b86f8bc702`.
- [x] **Step 3: Open one canonical PR** — PR #4634 with reservation metadata.
- [x] **Step 4: Require protected current-candidate checks** — run `33254205416` SUCCESS.
- [x] **Step 5: Merge same task PR and verify resulting main SHA** — merge `6976e0d6b2117cc3b98e5c10615569876600fba1`; Issue #4623 closed `COMPLETED / MERGED_MAIN`.

---

## Post-merge close-out — Issue #4647

- [x] Audit the approved design against current production source rather than relying only on old CI.
- [x] Identify focus-state explicit foreground/background ownership as the bounded remaining source-contract gap.
- [x] Add stricter test-first guard requirements in commit `0bfe98490b09086517c25d024e8b21a710d4b69a`.
- [x] Reject run `33256695063` attempt 1 as invalid RED because reservation failed before the feature guard.
- [x] Diagnose reservation root cause: earlier active Issue #4629 owns `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`.
- [x] Remove that production path from #4647 reservation and hand off the exact two-setter source correction to #4629 without concurrent mutation.
- [ ] Obtain a valid stricter-contract RED where reservation passes and the Agent Center feature guard fails specifically for missing focus background/foreground ownership.
- [ ] After #4629 lands/releases the source path, reconcile current `main` and verify the stricter Agent Center guard GREEN.
- [ ] Obtain fresh exact-head shared branch CI and protected PR CI for #4647, merge its guard/docs close-out, and record final SHA evidence.

## Licensed visual/runtime qualification — LOCAL-024

These rows remain intentionally open. Do not mark them complete from source inspection or hosted CI.

- [ ] **PENDING_LOCAL:** launch `QS3DMCPAGENTCENTER` in licensed BricsCAD on the exact frozen candidate.
- [ ] **PENDING_LOCAL:** verify System / Dark / Light, including live Windows app-theme switching in System mode and explicit override persistence.
- [ ] **PENDING_LOCAL:** verify 1040×780 and minimum 780×620, long URL wrapping, and no clipping/overlap.
- [ ] **PENDING_LOCAL:** verify 100% / 125% / 150% DPI where available.
- [ ] **PENDING_LOCAL:** visually exercise Primary / Secondary / Danger / Utility / Navigation / ThemeChoice across Normal / Hover / Pressed / KeyboardFocus / Disabled with readable foreground/background/border.
- [ ] **PENDING_LOCAL:** verify Info / Success / Warning / Error toast stack, close, max-four behavior, lifetimes, theme readability and bounded Logs history.
- [ ] **PENDING_LOCAL:** verify no bearer-token/Authorization secret value leaks to toast/history.
- [ ] **PENDING_LOCAL:** record exact Git SHA/ProductVersion/plugin identity, Windows theme/scaling and sanitized evidence; only a real licensed execution may produce `LOCAL_PASS`.
