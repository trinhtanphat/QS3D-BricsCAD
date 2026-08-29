# QS3D MCP Guided Onboarding / Control / Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve completed Approach A and implement owner-approved Approach B as one bounded `desktop_sequence` tool on PR #4632 without creating a generic scripting/shell surface.

**Architecture:** `McpEmbeddedServerV2` remains the only MCP transport and `McpCadAgentRuntime.Mutation(...)` remains the canonical mutation epoch/confirmation boundary. `McpDesktopAutomationRuntime` owns the new sequence parser/executor and composes only an allowlisted subset of existing desktop primitives against one exact target window. Existing local consent, blue overlay, Esc×2, audit and sensitive-read confirmation remain the safety controls.

**Tech Stack:** C# / .NET Framework-compatible V25/V26 plugin code, WPF, Win32 `SendInput`, MCP JSON-RPC `tools/list` + `tools/call`, Python source/preflight contracts.

**Spec:** `docs/superpowers/specs/2026-08-29-mcp-guided-onboarding-control-recovery-design.md`

## Global Constraints

- Approach A is already source-implemented and must remain intact.
- Approach B exposes one canonical tool only: `desktop_sequence`; do not add a `desktop_macro` alias.
- Sequence binds to one exact visible current-session `windowHandle`; no target switching inside a sequence.
- Maximum 12 steps, 30 seconds total runtime, 2000 ms delay after a step, fail-fast, no recursion and no `continueOnError`.
- `stepsJson` is a bounded string containing an array of flat records `{tool, arguments, delayAfterMs}`. Each `arguments` value is a bounded flat JSON-object string.
- Step arguments may not supply `windowHandle`, `confirmMutation` or `confirmSensitiveRead`; the sequence owns those security-sensitive values.
- Allowlisted sequence primitives: focus, mouse move/click/scroll/drag, type, key, clipboard write, wait-for-window and target-window screenshot.
- `desktop_clipboard_read`, CAD/QS3D dispatch, filesystem, shell/process/script/eval and nested sequence are forbidden inside a sequence.
- Sequence screenshot requires sequence-level `confirmSensitiveRead=true` and is forced to the bound target window.
- Sequence itself requires `confirmMutation=true` plus current local desktop consent.
- Esc×2, Pause, consent revocation and mutation epoch changes abort before the next input/delay segment.
- Completed UI steps are not rolled back if a later step fails; partial completion must be explicit in result/audit.
- Audit/status never persists typed text, clipboard text, screenshot pixels, OAuth/bearer tokens or private DWG contents.
- Another authorized agent owns CI remediation/reruns; do not manually rerun CI from this feature pass or overwrite unrelated concurrent guard work.
- Real Windows/BricsCAD/ChatGPT execution remains `PENDING_LOCAL` until actually observed locally.

---

## Approach A — completed source work

### Task 1: Explicit desktop Completion Pack A

**Files:**
- `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs`
- `src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs`
- `src/QS3D.BricsCAD.V25/McpAgentExperience.cs`
- `src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs`
- `scripts/test-mcp-guided-onboarding-control-recovery-source.py`

**Produces:** bounded drag, wait-for-window, screenshot crop, 10-minute consent expiry, Pause/Resume, Action ID/duration/terminal status and Agent Center recovery guidance.

- [x] Contract-first source assertions added before production implementation.
- [x] `desktop_mouse_drag` implemented with exact target and stop revalidation.
- [x] `desktop_wait_for_window` implemented with <=15 second timeout.
- [x] screenshot crop implemented under existing output caps.
- [x] local consent idle expiry + Pause/Resume implemented.
- [x] action timeline metadata and Agent Center states implemented.

---

## Approach B — bounded sequence implementation

### Task 2: RED contract for `desktop_sequence`

**Files:**
- Modify: `scripts/test-mcp-guided-onboarding-control-recovery-source.py`

**Interfaces:**
- Produces source assertions for `desktop_sequence`, `MaxSequenceSteps`, `MaxSequenceMilliseconds`, `MaxSequenceDelayMilliseconds`, `SequenceAllowedTools`, `ParseSequenceSteps`, `RunSequence`, sensitive screenshot confirmation, single-target enforcement and `desktop_macro` absence.

- [x] **Step 1: Replace the old `desktop_sequence` forbid assertion with required Approach B tokens.**
- [x] **Step 2: Require hard limits `12`, `30000`, `2000`, fail-fast/no recursion wording and the bound-window screenshot rule.**
- [x] **Step 3: Keep `desktop_macro` forbidden so only one canonical batch surface exists.**
- [x] **Step 4: Commit the source contract before production sequence code.** At this commit, the contract is intentionally RED because runtime tokens do not yet exist.

### Task 3: Sequence descriptor, parser and allowlist

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs`

**Interfaces:**
- Produces MCP tool `desktop_sequence(windowHandle,stepsJson,maxDurationMs?,confirmMutation,confirmSensitiveRead?)`.
- Produces internal `SequenceStep` records with `Tool`, `Arguments`, `DelayAfterMilliseconds`.
- Produces `ParseSequenceSteps(string)` and `SequenceAllowedTools`.

- [x] **Step 1: Add constants.** `MaxSequenceSteps = 12`, `MaxSequenceMilliseconds = 30000`, `MaxSequenceDelayMilliseconds = 2000`, `MaxSequenceJsonCharacters = 32768`, `MaxSequenceStepArgumentsCharacters = 8192`.
- [x] **Step 2: Add `desktop_sequence` to `Tools` and `MutationTools`, descriptor schema and dispatcher routing.** Descriptor requires exact `windowHandle`, `stepsJson`, `confirmMutation`; optional `maxDurationMs` and `confirmSensitiveRead`.
- [x] **Step 3: Add a strict array scanner for decoded `stepsJson`.** Each element must be one flat JSON object; strings/escapes are honored; nested arrays/objects outside the string-valued `arguments` field are rejected; max 12 elements.
- [x] **Step 4: Parse each step using `McpTopLevelJson`.** Require `tool`; optional `arguments` defaults to `{}`; optional `delayAfterMs` defaults 0 and is 0–2000.
- [x] **Step 5: Validate inner `arguments`.** Require a flat JSON object string <=8192 chars; reject caller-owned `windowHandle`, `confirmMutation`, `confirmSensitiveRead`; reject any nested object/array token outside quoted strings.
- [x] **Step 6: Reject tools outside `SequenceAllowedTools`, including `desktop_sequence` and `desktop_clipboard_read`.**

### Task 4: Single-target sequence executor

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs`

**Interfaces:**
- Produces `RunSequence(string body, Action ensureMutationRunning, Action<string> audit)`.
- Consumes existing primitive methods without opening another transport or dispatcher.

- [x] **Step 1: Resolve/validate the sequence `windowHandle` once before execution and start a wall-clock `Stopwatch`.** Default max duration 15000 ms; caller may set 1000–30000.
- [x] **Step 2: Before every step call `ensureMutationRunning()` and revalidate that the bound handle remains a visible current-session window.**
- [x] **Step 3: Inject the bound target/security values internally.** Target-bound primitives receive exact `windowHandle`; screenshot receives `scope=window` + target + `confirmSensitiveRead=true` only after sequence-level confirmation; inner mutation confirmation is not caller-controlled because the outer sequence is already inside `Mutation(...)`.
- [x] **Step 4: For `desktop_mouse_move`, additionally require its x/y inside the bound window and focus/revalidate the bound target before moving.**
- [x] **Step 5: Execute allowed primitive methods directly in the runtime rather than recursively calling `desktop_sequence`.** Reuse existing click/scroll/drag/type/key/focus/clipboard-write/wait/screenshot methods so their target/input validation stays canonical.
- [x] **Step 6: Implement delay in <=50 ms slices.** Each slice checks mutation epoch and total duration so Esc×2/Pause stops promptly.
- [x] **Step 7: Fail fast.** On the first failure, audit only step index/tool/completed count/duration and throw a sanitized exception; do not log arguments or typed/clipboard/screenshot content.
- [x] **Step 8: Return bounded JSON with target handle, completed step count, duration and per-step result envelopes.** Do not claim rollback; screenshot base64 may appear only in the MCP result, never local audit/status.

### Task 5: Sensitive-read and partial-execution safety

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs`
- Modify: `scripts/test-mcp-guided-onboarding-control-recovery-source.py`

**Interfaces:**
- Sequence screenshot opt-in is sequence-level `confirmSensitiveRead=true` and target-window-only.

- [x] **Step 1: Pre-scan all steps before executing any mutation.** If a screenshot step exists and sequence-level `confirmSensitiveRead` is not true, reject before step 1.
- [x] **Step 2: Force screenshot `scope=window` and the bound handle; reject step-provided scope/handle/confirmation fields.**
- [x] **Step 3: Keep clipboard read forbidden in `SequenceAllowedTools`.** Clipboard write remains allowed but audit records only character count.
- [x] **Step 4: Add source assertions for no `desktop_macro`, no recursive sequence allowlisting, hard caps and sensitive-read preflight-before-execution behavior.**

### Task 6: Documentation / Agent Center discoverability

**Files:**
- Modify: `docs/MCP-CANONICAL-RUNBOOK.md`
- Modify: `docs/MCP-GUIDED-ONBOARDING-RECOVERY.md`
- Modify: `docs/CHATGPT-MCP-INTEGRATION.md` only if it contains a pinned stale desktop-tool count.
- Modify: PR #4632 body / Issue #4629 metadata.

- [x] **Step 1: Document Approach A as completed source and Approach B as selected/current.**
- [x] **Step 2: Document the exact `desktop_sequence` shape, limits, allowed steps and single-target rule with one short example.**
- [x] **Step 3: Document non-atomic partial execution, Esc×2 cancellation and screenshot sensitive-read opt-in.**
- [x] **Step 4: State clearly that sequence is not shell/process/script/plugin dispatch and does not allow clipboard read.**
- [x] **Step 5: Update PR summary from 14 to 15 desktop tools after production code lands.**

### Task 7: Source review / verification boundary

**Files:**
- Review exact changed files above; coordinate rather than overwrite concurrent CI-guard owner.

- [x] **Step 1: Re-read current PR head before each mutation and preserve concurrent fixes.**
- [x] **Step 2: Static-review sequence parser for unterminated strings, nested-object bypass, step-count bypass, duplicate security-sensitive properties and oversized payloads.** `McpTopLevelJson` rejects duplicate top-level properties and canonicalizes flat `arguments`; sequence scanners independently bound nesting/count/size.
- [x] **Step 3: Static-review cancellation paths so every delay/input path reaches mutation-epoch checks.** `EnsureSequenceStepRunning` is the injected per-step callback and delegates to consent + epoch + target + duration revalidation.
- [x] **Step 4: Do not manually rerun CI in this feature pass.** New commits may naturally trigger checks owned by the other agent.
- [x] **Step 5: Keep Windows/BricsCAD/ChatGPT sequence behavior `PENDING_LOCAL`; do not claim `LOCAL_PASS` without actual licensed-host evidence.**

## Reconciliation note

- Concurrent source contract reconciliation landed in `c81e385c8b22a2a6aa2e582297308e1b04313ab4` (`EnsureSequenceStepRunning` + exact wait fail-fast wording).
- Consent-generation fail-closed protection for `desktop_sequence` landed in `f44bd49409d1c6b7baeff8489b329b46881f04da`.
- This checklist records source/static completion only. Protected CI remains owned by the concurrent CI worker, and real Windows/BricsCAD/ChatGPT execution remains `LOCAL_ONLY / PENDING_LOCAL`.
