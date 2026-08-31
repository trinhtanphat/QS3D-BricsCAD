# MCP Dual Control Capabilities Implementation Plan

**Goal:** Make Background Control and Foreground Control simultaneous, explicit BricsCAD MCP capabilities while keeping background preferred/default.

**Architecture:** Keep the existing direct CAD/QS3D/background-host and desktop runtimes. Extend the already-registered interaction-policy status with dual capability state. Reuse the released `McpPersistentAgentCenterAugmenter` from merged PR #4990 to inject separate capability cards and synchronize the canonical Resume/Pause/Emergency controls without rewriting the large transport-oriented Agent Center source.

## Constraints

- Background is always the preferred/default route.
- Foreground is explicit-only and requires local consent.
- Enabling foreground never disables background.
- No implicit background-to-foreground fallback.
- No new shell/process/filesystem/eval surface.
- `bricscad_interaction_policy_get/set` remains backward compatible.
- Preserve #4990 foreground-toggle and Windows Credential Manager behavior.

## Task 1 — RED contract

- [x] Add `scripts/preflight-mcp-dual-control-capabilities.py` before production changes.
- [x] Require dual capability JSON fields, local enable/disable helpers, separate Agent Center capability cards/status, and no-silent-fallback tokens.
- [x] Confirm protected CI failure came from the missing dual-control feature after lane/collision guards passed.

## Task 2 — Background policy contract

Files: `src/QS3D.BricsCAD.V25/McpBackgroundHostRuntime.cs`

- [x] Extend `PolicyJson()` with `backgroundControl`, `foregroundControl`, `defaultRoute:"background"`, `fallback:"explicit_only"`, and `implicitForegroundFallback:false` while retaining legacy fields.
- [x] Add `EnableForegroundFromLocalUser()` requiring existing local consent.
- [x] Add idempotent `DisableForegroundFromLocalUser()`.
- [x] Prefix same-process background-host tool descriptions with `BACKGROUND CONTROL:`.
- [x] Preserve same-process HWND checks, confirmation requirements and zero implicit calls into desktop automation.

## Task 3 — Agent Center split

Files: `src/QS3D.BricsCAD.V25/McpPersistentAgentCenterAugmenter.cs`

- [x] Rebase on merged PR #4990 and preserve its credential-persistence + foreground-toggle behavior.
- [x] Inject `Thao tác nền · Background Control` and `Thao tác trực tiếp · Foreground Control` as separate capability cards/status rows in the Agent tab.
- [x] Explain background is `ưu tiên mặc định` and unsupported work `không tự chuyển sang thao tác trực tiếp`.
- [x] Hook canonical Resume so local consent is followed by `McpBackgroundHostRuntime.EnableForegroundFromLocalUser()`.
- [x] Hook Pause/Emergency so foreground policy is revoked with `DisableForegroundFromLocalUser()`.
- [x] Fail closed to `background_only` and foreground OFF if synchronization throws.

## Task 4 — Documentation and GREEN

Files: `docs/FEATURE-RUNBOOKS/mcp-background-host-control.md`, design/plan docs, dedicated preflight.

- [x] Document the simultaneous two-capability model and explicit-only foreground semantics.
- [ ] Run dedicated preflight plus existing MCP/Agent Center preflights.
- [ ] Run fresh protected aggregate/core/V25 CI on the exact PR head.
- [ ] Review final diff for execution/privacy regressions and merge only when green.

## Reservation follow-up

`McpDesktopAutomationRuntime.cs` remains outside this lane while #4799 / PR #4946 owns it. PR #4990 / issue #4989 merged during implementation, so its augmenter path is now reused rather than duplicated. The optional `bricscad_control_capabilities` alias remains unnecessary because the already-registered `bricscad_interaction_policy_get/set` surface carries the complete capability state.
