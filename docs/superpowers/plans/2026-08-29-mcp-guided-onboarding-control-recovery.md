# QS3D MCP Guided Onboarding / Control / Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete owner-approved Approach A for PR #4632 without adding the deferred generic batch/macro executor from Approach B.

**Architecture:** Keep `McpEmbeddedServerV2` as the only MCP transport and extend the existing explicit `desktop_*` runtime. Desktop actions remain individually bounded, locally consented, audited and emergency-stoppable. Add UI state and timeline metadata around the same local consent/session boundary rather than introducing another automation engine.

**Tech Stack:** C# / .NET Framework-compatible V25/V26 plugin code, WPF, Win32 `SendInput`, MCP JSON-RPC `tools/list` + `tools/call`, Python source/preflight contracts.

**Spec:** `docs/superpowers/specs/2026-08-29-mcp-guided-onboarding-control-recovery-design.md`

## Execution status — 2026-08-29

- `SOURCE_IMPLEMENTED`: Tasks 2–5 and documentation portions of Task 6 are implemented on the canonical #4629/#4632 carrier.
- Contract-first source assertions were committed before the Completion Pack A production implementation; the current contract was later aligned from the old “Bật quyền desktop” label to the selected local Resume/Pause UX.
- `scripts/preflight-mcp-desktop-function-calling.py` is intentionally not being edited by this feature pass because another authorized agent is handling CI/guard remediation concurrently. Do not overwrite that concurrent work.
- `SOURCE_VERIFICATION_PENDING`: no fresh local command/build result is claimed by this plan update. Hosted/protected CI is handled separately and real Windows + licensed BricsCAD + Cloudflare + ChatGPT remains `PENDING_LOCAL`.
- Approach B remains deferred; the current runtime must not expose `desktop_sequence` or `desktop_macro`.

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
- Coordinate, do not overwrite concurrent owner: `scripts/preflight-mcp-desktop-function-calling.py`

**Interfaces:**
- Consumes: existing explicit `desktop_*` tools and local-consent source contract.
- Produces: source assertions for `desktop_mouse_drag`, `desktop_wait_for_window`, screenshot crop fields, 10-minute consent expiry, local pause/resume, action timeline metadata and the absence of `desktop_sequence`/`desktop_macro`.

- [x] **Step 1: Add RED source assertions** requiring the new tool names/schema/routing plus consent/timeline symbols before production code exists.
- [ ] **Step 2: Run the focused source contract locally when an execution environment is available.** No fresh command-output claim is recorded here.
- [x] **Step 3: Keep the source contract semantic:** assert safety behavior/tokens, not fragile formatting or exact line order.
- [x] **Step 4: Commit the contract separately from production implementation.** Contract commit precedes the Completion Pack A source commits.

### Task 2: Bounded drag, wait-for-window and screenshot crop

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs`
- `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` remains the single transport/catalog integration path; no second server was added.

**Interfaces:**
- Produces tool `desktop_mouse_drag(windowHandle,startX,startY,endX,endY,button,durationMs,confirmMutation)`.
- Produces tool `desktop_wait_for_window(titleContains?,windowHandle?,timeoutMs?,pollIntervalMs?)` returning bounded matching metadata or `{found:false}`.
- Extends `desktop_screenshot` with optional `cropX`, `cropY`, `cropWidth`, `cropHeight` relative to the selected source bounds.

- [x] **Step 1: Add descriptors and dispatcher cases for drag/wait.**
- [x] **Step 2: Refactor exact-window point validation into a shared helper used by click, scroll and drag.**
- [x] **Step 3: Implement drag with bounded duration (50–3000 ms), bounded step cadence, exact foreground revalidation and mutation checks before button-down, movement segments and button-up, with best-effort button-up on cancellation.**
- [x] **Step 4: Implement wait-for-window with timeout <= 15000 ms and polling >= 50 ms, visible current-session top-level windows only, bounded title matching and no implicit focus/click.**
- [x] **Step 5: Implement screenshot crop intersection/rejection before capture/scale/PNG encoding while preserving output caps.**
- [x] **Step 6: Audit bounded handles/coordinates/dimensions/button/duration/result metadata only; single alphanumeric key audit is redacted as `CHARACTER`.**

### Task 3: Idle-expiring local consent and Pause/Resume

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs`

**Interfaces:**
- Produces `ConsentState`, `IdleRemaining`, `PauseFromLocalUser(...)`, `ResumeFromLocalUser()` and generation-safe guarded scopes.
- Consumes: existing keyboard emergency hook, overlay and `McpCadAgentRuntime.StopAutomation()`.

- [x] **Step 1: Add a 10-minute inactivity deadline refreshed when a guarded desktop action starts.**
- [x] **Step 2: Check/expire the deadline synchronously in consent reads and guarded-action entry so UI timer delivery is not the correctness boundary.**
- [x] **Step 3: Reuse the Agent Center one-second live refresh for countdown UX; correctness remains synchronous in `McpDesktopControlSession`.**
- [x] **Step 4: Implement local Pause as consent revocation + generation advance + emergency stop.**
- [x] **Step 5: Implement local Resume/Enable so the emergency keyboard hook must be installed before consent becomes usable; MCP has no routing path to these local methods.**
- [x] **Step 6: Preserve restart/shutdown reset-to-OFF and Esc×2 fail-closed semantics.**

### Task 4: Action IDs, duration and terminal state

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpAgentExperience.cs`
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs`

**Interfaces:**
- Produces bounded `actionId`, UTC start time, duration milliseconds and terminal state (`running`, `success`, `failed`, `cancelled`) for local desktop timeline entries.

- [x] **Step 1: Extend event records with optional action metadata while preserving bounded retention and existing callers.**
- [x] **Step 2: Create one Action ID per guarded desktop call and record start before overlay display.**
- [x] **Step 3: Complete the same Action ID as success/failure/cancelled on scope disposal; consent-generation change/Esc×2 is cancellation.**
- [x] **Step 4: Keep event/status fields bounded and avoid typed/clipboard/screenshot/token/DWG payloads.**

### Task 5: Agent Center state + recovery guidance

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`

**Interfaces:**
- Consumes: `McpDesktopControlSession` consent state/remaining idle time and `McpAgentExperience` timeline.
- Produces: local-only Pause/Resume controls plus visible waiting/controlling/paused/expired/re-enable-required states.

- [x] **Step 1: Use the canonical four task tabs — `Kết nối / Agent / Backup & khôi phục / Nâng cao` — while preserving System/Dark/Light theme, custom wrapper controls and toast cleanup.**
- [x] **Step 2: Use local `Resume desktop`, `Pause desktop`, and `Emergency Stop` semantics; UI explicitly states ChatGPT cannot remotely re-enable consent.**
- [x] **Step 3: Display consent state and `Idle còn` from `McpDesktopControlSession`, with no second consent source of truth.**
- [x] **Step 4: Surface current action/Action ID/duration/result/next step from the bounded local timeline.**
- [x] **Step 5: After stop/expiry/failure, show `Kiểm tra drawing/backup` guidance before local Resume.**

### Task 6: Documentation and source verification

**Files:**
- `docs/MCP-CANONICAL-RUNBOOK.md`
- `docs/MCP-GUIDED-ONBOARDING-RECOVERY.md`
- `docs/CHATGPT-MCP-INTEGRATION.md` remains compatible because it does not pin a stale desktop-tool count.

- [x] **Step 1: Document Approach A as current and Approach B as deferred future work.**
- [x] **Step 2: Document drag/wait/crop schemas, 10-minute consent expiry, Pause/Resume and timeline behavior.**
- [x] **Step 3: State the exact high-privilege desktop boundary and absence of arbitrary remote shell/process/macro execution.**
- [x] **Step 4: Re-read the exact feature diff/source docs for stale 12-tool wording, sensitive audit intent and accidental Approach B exposure; PR/Issue summaries were updated to 14 explicit tools and Approach B absent.**
- [ ] **Step 5: Fresh source/static/build verification.** Another agent is handling CI; this feature pass does not claim a command/build/LOCAL result without observed output.

## Existing onboarding/recovery work retained

The earlier tasks on this carrier remain part of the same architecture: bounded `McpAgentExperience`, local desktop consent + overlay + Esc×2, `McpProjectRecoveryService`, first-run cloudflared toast, four-tab Agent Center, `McpLocalAgentClient`, V25/V26 lifecycle parity, OAuth-first/Named-Tunnel onboarding and canonical documentation. Completion Pack A extends those pieces; it does not replace them.
