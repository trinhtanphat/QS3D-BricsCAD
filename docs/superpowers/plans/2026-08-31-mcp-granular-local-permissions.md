# MCP Granular Local Permissions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add explicit, locally controlled checkbox permissions for background BricsCAD control, screen reads, mouse, keyboard, and clipboard while preserving restart-safe MCP credential persistence.

**Architecture:** Keep the existing API/background-first versus foreground-fallback split. Add one focused process-memory permission authority, enforce it in the desktop/background runtimes, and render its authoritative state as WPF checkboxes in Agent Center. Existing consent, confirmation, emergency-stop, target-validation, and updater boundaries remain independent gates.

**Tech Stack:** C#/.NET BricsCAD V25/V26 plugin, WPF, Python source preflights, GitHub protected PR CI.

**Spec:** `docs/superpowers/specs/2026-08-31-mcp-granular-local-permissions-design.md`

## Global Constraints

- Product remains a BricsCAD V25 + V26 Windows x64 hosted plugin.
- `background_only` remains the safe process-start interaction mode.
- Foreground granular permissions default OFF and are process-memory-only.
- MCP may never remotely grant local foreground permissions.
- Existing `confirmMutation`, `confirmSensitiveRead`, local consent, Esc×2 emergency stop, and bounded target/input contracts remain in force.
- Runtime API key remains persisted in Windows Credential Manager with exact read-back verification; bearer persistence has no ephemeral fallback.
- Direct task writes to `main` are forbidden; merge through the protected task PR only.

---

### Task 1: Failing permission-contract preflight

**Files:**
- Create: `scripts/preflight-mcp-granular-local-permissions.py`

**Interfaces:**
- Consumes: existing V25 MCP source files and canonical runbook.
- Produces: deterministic source contract that fails until permission runtime/UI/enforcement/docs are implemented.

- [ ] **Step 1: Write the failing preflight**

Require the future source tokens for `McpLocalControlPermissions`, default permission state, exact tool mappings, CheckBox tags/labels, per-step sequence checks, and persistent-credential wording. Forbid the legacy single `DesktopForegroundToggleTag` UI contract and stale runbook claims that the Runtime API key is RAM-only/not persisted.

- [ ] **Step 2: Run test to verify RED**

Run: `python scripts/preflight-mcp-granular-local-permissions.py`
Expected: FAIL because `McpLocalControlPermissions.cs` and checkbox contracts do not exist yet.

- [ ] **Step 3: Commit the failing regression**

Commit message: `test(mcp): guard granular local permissions` with `Lane-Key: issue-5054`.

### Task 2: Local permission authority and runtime enforcement

**Files:**
- Create: `src/QS3D.BricsCAD.V25/McpLocalControlPermissions.cs`
- Modify: `src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs`
- Modify: `src/QS3D.BricsCAD.V25/McpBackgroundHostRuntime.cs`

**Interfaces:**
- Produces: `McpLocalControlPermissions.BackgroundHostControl`, `.ScreenRead`, `.MouseInput`, `.KeyboardInput`, `.ClipboardAccess`; local-only setter methods; `RequireForTool(string toolName)` and sequence-step enforcement helper.
- Consumes: existing `McpDesktopControlSession` and interaction-policy gates without replacing them.

- [ ] **Step 1: Implement minimal permission state**

Use process-memory booleans with background `true` and all foreground permissions `false`. Expose local grant/revoke methods only inside plugin source; do not add an MCP setter tool.

- [ ] **Step 2: Enforce exact tool mappings**

Call `RequireForTool` before applicable desktop/background dispatch. `desktop_sequence` must validate each parsed step's tool before executing that step so a permitted sequence cannot smuggle a denied mouse/keyboard/screenshot action.

- [ ] **Step 3: Run focused preflight**

Run: `python scripts/preflight-mcp-granular-local-permissions.py`
Expected: still FAIL only on missing Agent Center/docs contracts.

- [ ] **Step 4: Commit runtime enforcement**

Commit message: `feat(mcp): enforce granular local control permissions` with `Lane-Key: issue-5054`.

### Task 3: Agent Center checkbox UX

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpPersistentAgentCenterAugmenter.cs`

**Interfaces:**
- Consumes: `McpLocalControlPermissions` local setters/state and existing desktop consent/interaction-policy helpers.
- Produces: stable WPF `CheckBox` controls for background, screen, mouse, keyboard, clipboard plus explanatory status text.

- [ ] **Step 1: Replace the coarse button**

Remove the legacy `DesktopForegroundToggleTag` cloned button path. Insert a compact permission panel once, identified by a stable container tag.

- [ ] **Step 2: Add five checkbox controls**

Use stable tags and the exact Vietnamese labels from the spec. Refresh `IsChecked` from authoritative runtime state every augmenter tick.

- [ ] **Step 3: Wire fail-closed local changes**

Background checkbox directly grants/revokes same-process background authority. Foreground checkbox enable paths resume local desktop consent and set `foreground_fallback` before marking the requested granular permission granted; on failure, revoke the attempted permission and restore a safe state. Disabling the last foreground permission returns to `background_only` and revokes foreground consent without stopping API/background automation.

- [ ] **Step 4: Run focused preflight**

Run: `python scripts/preflight-mcp-granular-local-permissions.py`
Expected: only docs/persistence wording may still fail.

- [ ] **Step 5: Commit UI**

Commit message: `feat(mcp): add Agent Center permission checkboxes` with `Lane-Key: issue-5054`.

### Task 4: Canonical credential documentation and regression closure

**Files:**
- Modify: `docs/MCP-CANONICAL-RUNBOOK.md`
- Modify as needed: `scripts/preflight-mcp-granular-local-permissions.py`

**Interfaces:**
- Consumes: merged `McpPersistentUserSettings`/bearer persistence contract.
- Produces: canonical docs consistent with source truth.

- [ ] **Step 1: Update stale Runtime API-key wording**

Document Windows Credential Manager persistence, exact read-back verification before process publication, startup restore, no plaintext QS3D config/log storage, and updater credential isolation.

- [ ] **Step 2: Document granular permissions**

Describe background default and the five local checkbox permissions, including process-memory foreground fail-closed behavior and tool mapping boundaries.

- [ ] **Step 3: Run focused MCP preflights**

Run:
`python scripts/preflight-mcp-granular-local-permissions.py`
`python scripts/preflight-mcp-background-host-control.py`
`python scripts/preflight-mcp-credential-persistence.py`
Expected: all PASS.

- [ ] **Step 4: Commit docs/regression closure**

Commit message: `docs(mcp): document persisted keys and granular permissions` with `Lane-Key: issue-5054`.

### Task 5: Protected PR validation and merge

**Files:**
- No new source scope unless CI reveals a current-lane defect.

**Interfaces:**
- Produces: protected, current, collision-clean PR merged to `main`.

- [ ] **Step 1: Open/update canonical PR**

PR body includes `Closes #5054`, `Lane-Key: issue-5054`, `Reservation-Protocol: v2`, scope, focused validation, and no false LOCAL_PASS claim.

- [ ] **Step 2: Diagnose current-lane CI failures**

Use exact workflow/job logs, fix only same-task failures on the same branch, rerun fresh checks.

- [ ] **Step 3: Verify protected gates**

Require current candidate `preflight` and `core` SUCCESS, strict freshness, mergeability, and collision cleanliness.

- [ ] **Step 4: Merge same task PR**

Merge through the protected PR path, verify resulting current `main` SHA, close Issue #5054, and release reservation state.
