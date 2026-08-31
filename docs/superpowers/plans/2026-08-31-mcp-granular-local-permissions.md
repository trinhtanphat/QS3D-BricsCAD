# MCP Local-Control Permission UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the coarse foreground desktop action button with a clear two-checkbox authority panel while preserving restart-safe MCP credential persistence.

**Architecture:** Reuse the existing real authority split instead of adding a parallel permission subsystem: background BricsCAD/API stays the safe/default path, and foreground desktop remains an explicit locally enabled fallback. The UI is an augmenter-only change backed by existing `McpDesktopControlSession` and `bricscad_interaction_policy_*` enforcement.

**Tech Stack:** C#/.NET BricsCAD V25/V26 shared plugin source, WPF, Python source preflights, GitHub protected PR CI.

**Spec:** `docs/superpowers/specs/2026-08-31-mcp-granular-local-permissions-design.md`

## Global Constraints

- Product remains BricsCAD V25 + V26 Windows x64 hosted plugin.
- `background_only` remains process-start default.
- Remote MCP cannot grant local desktop consent.
- Existing mutation/sensitive-read confirmations, Esc×2 Emergency Stop and bounded target/input contracts remain unchanged.
- Runtime API key remains Windows Credential Manager persisted with exact read-back verification; bearer persistence has no ephemeral fallback.
- Direct task writes to `main` are forbidden; merge through the protected task PR.

---

### Task 1: Failing permission-UI regression

**Files:**
- Create: `scripts/preflight-mcp-granular-local-permissions.py`

- [x] **Step 1: Write RED guard**

Require stable checkbox tags/labels, two-mode policy/consent wiring, fail-closed foreground OFF, credential read-back verification and updater credential isolation.

- [x] **Step 2: Verify RED from source truth**

The guard initially failed because the Agent Center augmenter had no `PermissionPanelTag` or WPF `CheckBox` permission surface. Local clone execution was unavailable because the execution sandbox has no GitHub DNS; exact-head CI remains the executable validation path.

### Task 2: Agent Center checkbox UX

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpPersistentAgentCenterAugmenter.cs`

- [x] **Step 1: Replace cloned foreground action button**

Insert one tagged permission `StackPanel` after the existing Resume desktop action.

- [x] **Step 2: Add the two real authority checkboxes**

Read-only checked background indicator: `MCP chạy nền BricsCAD/API (không chiếm chuột/phím): BẬT`.

Interactive local foreground checkbox: `Cho phép chuột / bàn phím / màn hình user`.

- [x] **Step 3: Keep authoritative policy/consent wiring**

Foreground ON calls local Resume then `foreground_fallback`. Foreground OFF restores `background_only` then `DisableForegroundAccessFromLocalUser`, leaving background CAD/API alive. Failure restores safe state and reports locally without rethrowing through WPF.

### Task 3: Focused documentation and regression closure

**Files:**
- Create: `docs/FEATURE-RUNBOOKS/mcp-local-control-permission-ui.md`
- Update: `scripts/preflight-mcp-granular-local-permissions.py`

- [x] **Step 1: Document the two-mode UI and current Runtime API-key persistence**

Record Windows Credential Manager write → exact read-back verification → process publication → V25/V26 startup restore, with no plaintext QS3D config/log storage.

- [ ] **Step 2: Run protected validation**

Required focused guards:

```text
python scripts/preflight-mcp-granular-local-permissions.py
python scripts/preflight-mcp-background-host-control.py
python scripts/preflight-mcp-credential-persistence.py
python scripts/preflight-mcp-agent-center-uiux.py
```

Protected PR `preflight` and `core` SUCCESS on the exact candidate remain authoritative.

### Task 4: PR validation and merge

- [ ] **Step 1: Open canonical PR** with `Closes #5054`, `Lane-Key: issue-5054`, `Reservation-Protocol: v2`.
- [ ] **Step 2: Diagnose/fix only current-lane CI failures on the same branch.**
- [ ] **Step 3: Require fresh protected `preflight` + `core`, mergeability, freshness and collision cleanliness.**
- [ ] **Step 4: Merge the same task PR, verify current `main`, close Issue #5054 and release reservation.**
