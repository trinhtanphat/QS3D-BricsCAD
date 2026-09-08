# Agent Center + Update Center Info Tooltips Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce verbose inline copy in Agent Center and Update Center to compact, accessible `i` controls that reveal the original full text on hover or keyboard focus.

**Architecture:** Add a shared UI-only WPF bootstrap that decorates only `McpAgentControlCenter` and `UpdateCenterWindow` after load. Original text elements stay as the dynamic source of truth but are collapsed; compact replacement rows bind tooltips/accessibility text to those originals. Agent Center re-rendering is handled by a bounded dispatcher re-apply after window button clicks.

**Tech Stack:** C#, WPF, existing V25/V26 shared source compilation, Python feature preflight.

**Spec:** `docs/superpowers/specs/2026-09-08-ui-info-tooltips.md`

## Global Constraints

- Reservation issue: #6154.
- Keep operational status/version/progress/actions visible.
- Full explanatory text must remain available by mouse hover and keyboard focus.
- No transport, updater, package, credential, file-I/O, or process-control behavior changes.
- No new dependency and no `.github/workflows` edits.
- Production helper must remain valid for the repository's V25 and V26 compilation paths.

---

### Task 1: Pin the UI tooltip behavior RED

**Files:**
- Create: `scripts/preflight-ui-info-tooltips.py`
- Create: `docs/superpowers/specs/2026-09-08-ui-info-tooltips.md`
- Create: `docs/superpowers/plans/2026-09-08-ui-info-tooltips.md`
- Test: `scripts/preflight-ui-info-tooltips.py`

**Interfaces:**
- Consumes: approved design and reservation #6154.
- Produces: a fail-closed static regression gate requiring `src/QS3D.BricsCAD.V25/UiInfoTooltipBootstrap.cs` and its accessibility/UI-only contract.

- [ ] **Step 1: Write the failing feature preflight**

Require the future helper to contain module initialization, exact two-window targeting, button-click re-apply, tooltip timing, keyboard-accessible automation properties, visual-tree traversal, all approved copy matchers, and a matcher for the Update Center state detail. Forbid transport/update/process/file mutation tokens.

- [ ] **Step 2: Run the preflight and verify RED**

Run:

```bash
python scripts/preflight-ui-info-tooltips.py
```

Expected: exit `1` with `missing src/QS3D.BricsCAD.V25/UiInfoTooltipBootstrap.cs`.

- [ ] **Step 3: Commit the RED guard and design artifacts**

The first pushed commit must carry reservation trailers for issue #6154 and all four expected paths.

---

### Task 2: Implement the shared UI-only decorator GREEN

**Files:**
- Create: `src/QS3D.BricsCAD.V25/UiInfoTooltipBootstrap.cs`
- Test: `scripts/preflight-ui-info-tooltips.py`

**Interfaces:**
- Consumes: existing WPF visual trees and original text values from `McpAgentControlCenter` / `UpdateCenterWindow`.
- Produces: `UiInfoTooltipBootstrap.Initialize()`, targeted load handling, `CreateInfoButton(...)`, compact replacement rows, source-bound WPF tooltips, and duplicate-decoration markers.

- [ ] **Step 1: Add module-initializer compatibility**

Provide a net48-compatible `System.Runtime.CompilerServices.ModuleInitializerAttribute` only when `BRICSCAD_V26` is not defined, then register a WPF `Window.Loaded` class handler from a parameterless static initializer.

- [ ] **Step 2: Limit decoration to the two requested windows**

Match only concrete type names `McpAgentControlCenter` and `UpdateCenterWindow`. Mark hooked windows with an attached dependency property and attach a handled-events-too `ButtonBase.ClickEvent` handler that dispatches one re-apply after Agent Center page-changing clicks.

- [ ] **Step 3: Compact Agent Center explanatory text**

Match the approved long-copy prefixes and the dynamic `Tiếp theo: ` block. Keep concise contextual labels and collapse only the original paragraph while preserving it as the binding source.

- [ ] **Step 4: Compact Update Center explanatory text**

Match build/DLL identity, the uniquely styled state-detail paragraph, update-on-close helper copy, and the read-only Consolas release-note TextBox. Keep the state and release-note headings visible.

- [ ] **Step 5: Add accessible hover/focus info controls**

Create a 20px rounded `i` button with source-colored foreground/border. Bind wrapped tooltip content plus `AutomationProperties.HelpText` to the original source; set `AutomationProperties.Name`; configure tooltip delay/duration; explicitly open/close on keyboard focus.

- [ ] **Step 6: Run the feature preflight and verify GREEN**

Run:

```bash
python scripts/preflight-ui-info-tooltips.py
```

Expected: exit `0` and PASS message for both windows.

- [ ] **Step 7: Commit production implementation**

Commit only the new shared helper with issue/reservation references.

---

### Task 3: Integrate, review, and merge safely

**Files:**
- Verify only the four reserved paths changed.

**Interfaces:**
- Consumes: GREEN feature branch from Tasks 1-2.
- Produces: reviewable PR against current protected `main`; merged result only after required checks are green/current and the PR is mergeable.

- [ ] **Step 1: Re-read protected main and reconcile drift**

Fetch `main` immediately before PR/merge. If protected main moved, inspect overlap with the four reserved paths and reconcile before relying on checks.

- [ ] **Step 2: Review exact branch diff**

Confirm no production transport/updater source or workflow files changed and the helper is presentation-only.

- [ ] **Step 3: Open PR referencing #6154**

Describe RED→GREEN evidence, both-window coverage, accessibility behavior, and non-goals.

- [ ] **Step 4: Wait for current-head required checks**

Use PR check/status APIs. Do not merge while checks are pending/failing or mergeability is stale/blocked.

- [ ] **Step 5: Merge to `main` and close reservation**

Merge only when the exact PR head is current, checks are green, and GitHub reports it mergeable. Verify the resulting protected `main` contains the merge, then close issue #6154 as completed.
