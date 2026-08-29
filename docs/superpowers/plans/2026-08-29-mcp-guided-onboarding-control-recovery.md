# QS3D MCP Guided Onboarding / Control / Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete owner-approved Approach A for PR #4632 without adding the deferred generic batch/macro executor from Approach B.

**Architecture:** Keep `McpEmbeddedServerV2` as the only MCP transport and extend the existing explicit `desktop_*` runtime. Desktop actions remain individually bounded, locally consented, audited and emergency-stoppable. Add UI state and timeline metadata around the same local consent/session boundary rather than introducing another automation engine.

**Tech Stack:** C# / .NET Framework-compatible V25/V26 plugin code, WPF, Win32 `SendInput`, MCP JSON-RPC `tools/list` + `tools/call`, Python source/preflight contracts.

**Spec:** `docs/superpowers/specs/2026-08-29-mcp-guided-onboarding-control-recovery-design.md`

## Global Constraints

- Approach A is selected; Approach B (`desktop_sequence` / generic macro execution) is deferred and must not be exposed by this PR.
- No arbitrary shell/process-launch MCP tool or credential/browser-cookie scraping.
- Desktop mutation requires `confirmMutation=true` plus non-persistent local desktop consent.
- Clipboard reads/screenshots require `confirmSensitiveRead=true` plus local desktop consent.
- Click/scroll/drag use an exact visible current-session `windowHandle`, exact foreground ownership immediately before injection and point-within-window validation.
- Esc×2 and mutation/consent generation changes fail closed.
- Consent expires after 10 minutes of desktop-action inactivity and cannot be enabled/resumed remotely.
- Audit/status never persists typed text, clipboard text, screenshot pixels, OAuth/bearer tokens or private DWG contents.
- Real Windows/BricsCAD/ChatGPT execution remains `PENDING_LOCAL` until observed locally.

---

### Task 1: Contract-first Completion Pack A

**Files:**
- Modify: `scripts/test-mcp-guided-onboarding-control-recovery-source.py`
- Modify: `scripts/preflight-mcp-desktop-function-calling.py`

**Interfaces:**
- Consumes: existing 12 explicit `desktop_*` tools and local-consent source contract.
- Produces: source assertions for `desktop_mouse_drag`, `desktop_wait_for_window`, screenshot crop fields, 10-minute consent expiry, local pause/resume, action timeline metadata and the absence of `desktop_sequence`/`desktop_macro`.

- [ ] **Step 1: Add RED source assertions** requiring the new tool names/schema/routing plus consent/timeline symbols before production code exists.
- [ ] **Step 2: Run the focused source contract locally when an execution environment is available; expected result before production changes is FAIL on missing Approach A symbols.**
- [ ] **Step 3: Keep the guard semantic:** assert safety behavior/tokens, not fragile formatting or exact line order.
- [ ] **Step 4: Commit the contract separately from production implementation.**

### Task 2: Bounded drag, wait-for-window and screenshot crop

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs`
- Modify: `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` only if catalog assembly outside the runtime requires it.

**Interfaces:**
- Produces tool `desktop_mouse_drag(windowHandle,startX,startY,endX,endY,button,durationMs,confirmMutation)`.
- Produces tool `desktop_wait_for_window(titleContains?,windowHandle?,timeoutMs?,pollIntervalMs?)` returning bounded matching metadata or `{found:false}`.
- Extends `desktop_screenshot` with optional `cropX`, `cropY`, `cropWidth`, `cropHeight` relative to the selected source bounds.

- [ ] **Step 1: Add descriptors and dispatcher cases for drag/wait.**
- [ ] **Step 2: Refactor exact-window point validation into a shared helper used by click, scroll and drag.**
- [ ] **Step 3: Implement drag with bounded duration (50-3000 ms), bounded step cadence, exact foreground revalidation and mutation checks before button-down, every movement segment and button-up. Ensure a best-effort button-up occurs on cancellation without continuing the drag.**
- [ ] **Step 4: Implement wait-for-window with timeout <= 15000 ms and polling >= 50 ms, visible current-session top-level windows only, bounded title matching and no implicit focus/click.**
- [ ] **Step 5: Implement screenshot crop intersection/rejection before capture/scale/PNG encoding; preserve existing output byte/dimension caps.**
- [ ] **Step 6: Audit only handles, coordinates/dimensions, button/duration and result metadata; never screenshot pixels or window secrets.**

### Task 3: Idle-expiring local consent and Pause/Resume

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs`

**Interfaces:**
- Produces `ConsentState`, `IdleRemaining`, `PauseFromLocalUser(...)`, `ResumeFromLocalUser()` and generation-safe guarded scopes.
- Consumes: existing keyboard emergency hook, overlay and `McpCadAgentRuntime.StopAutomation()`.

- [ ] **Step 1: Add a 10-minute inactivity deadline that is refreshed only when a guarded desktop action successfully starts.**
- [ ] **Step 2: Check/expire the deadline synchronously in consent reads and guarded-action entry so a stale timer can never permit a remote action.**
- [ ] **Step 3: Add a lightweight local dispatcher timer only for UX/countdown refresh; correctness must not depend on the timer firing.**
- [ ] **Step 4: Implement local Pause as consent revocation + generation advance + emergency stop.**
- [ ] **Step 5: Implement local Resume/Enable so the emergency keyboard hook must be installed successfully before consent becomes usable. MCP must have no routing path to these local methods.**
- [ ] **Step 6: Preserve restart/shutdown reset-to-OFF and Esc×2 fail-closed semantics.**

### Task 4: Action IDs, duration and terminal state

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpAgentExperience.cs`
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs`

**Interfaces:**
- Produces bounded `actionId`, UTC start/end, duration milliseconds and terminal state (`running`, `success`, `failed`, `cancelled`) for local desktop timeline entries.

- [ ] **Step 1: Extend event records with optional action metadata while preserving bounded retention and existing callers.**
- [ ] **Step 2: Create one action ID per guarded desktop call and record start before overlay display.**
- [ ] **Step 3: Complete the same action ID as success/failure/cancelled on scope disposal; cancellation includes consent-generation change/Esc×2.**
- [ ] **Step 4: Ensure exception messages/status details remain bounded and scrubbed of typed/clipboard/screenshot/token payloads.**

### Task 5: Agent Center state + recovery guidance

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`

**Interfaces:**
- Consumes: `McpDesktopControlSession` consent state/remaining idle time and `McpAgentExperience` timeline.
- Produces: local-only Pause/Resume controls plus visible waiting/controlling/paused/expired/re-enable-required states.

- [ ] **Step 1: Keep the existing four task tabs, System/Dark/Light theme, wrapper controls and toast path.**
- [ ] **Step 2: Replace ambiguous enable/disable copy with local `Bật/Resume`, `Pause`, and `Emergency Stop` semantics; never imply ChatGPT can remotely re-enable consent.**
- [ ] **Step 3: Display consent state and idle remaining time without creating a second source of truth.**
- [ ] **Step 4: Surface current action/action ID, duration/result and next step from the bounded local timeline.**
- [ ] **Step 5: After emergency stop/expiry/failure, show a recovery hint: verify drawing state/recovery copy, then explicitly re-enable desktop permission locally if desired.**

### Task 6: Documentation and source verification

**Files:**
- Modify: `docs/MCP-CANONICAL-RUNBOOK.md`
- Modify: `docs/MCP-GUIDED-ONBOARDING-RECOVERY.md`
- Modify: `docs/CHATGPT-MCP-INTEGRATION.md` only if its public tool inventory is stale.

- [ ] **Step 1: Document Approach A as current and Approach B as deferred future work.**
- [ ] **Step 2: Document drag/wait/crop schemas, 10-minute consent expiry, Pause/Resume and timeline behavior.**
- [ ] **Step 3: State the exact high-privilege desktop boundary and the absence of arbitrary shell/process/macro execution.**
- [ ] **Step 4: Re-read the final exact branch head and inspect the diff for secrets, sensitive audit payloads, stale tool counts and accidental Approach B exposure.**
- [ ] **Step 5: Run source/static/build verification available in the current environment. Do not fabricate LOCAL_PASS for Windows/BricsCAD/ChatGPT behavior.**

## Existing onboarding/recovery work retained

The earlier tasks on this carrier remain part of the same architecture: bounded `McpAgentExperience`, local desktop consent + overlay + Esc×2, `McpProjectRecoveryService`, first-run cloudflared toast, four-tab Agent Center, `McpLocalAgentClient`, V25/V26 lifecycle parity, OAuth-first/Named-Tunnel onboarding and canonical documentation. Completion Pack A extends those pieces; it does not replace them.