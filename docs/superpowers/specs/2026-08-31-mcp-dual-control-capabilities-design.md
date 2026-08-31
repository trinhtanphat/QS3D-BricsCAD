# MCP Dual Foreground / Background Control Design

Issue: #5018  
Lane-Key: issue-5018  
Canonical carrier: `agent/chatgpt-gpt56sol-20260831-dual-control/issue-5018-dual-control-capabilities`

## Goal

QS3D MCP exposes two simultaneous BricsCAD control capabilities:

1. **Background Control** — preferred/default. Direct `cad_*`, `qs3d_*`, BricsCAD application-context commands and bounded same-process `bricscad_ui_*` operations. It never moves the global cursor, injects global keyboard input, or steals foreground focus.
2. **Foreground Control** — explicit opt-in. Existing `desktop_*` tools may focus windows, move/click the cursor and inject keyboard input, but only after the user enables local desktop consent.

Enabling Foreground Control never disables Background Control. A background failure never silently falls back to global Windows input.

## Existing state and defect

The repository already has `McpBackgroundHostRuntime`, `McpDesktopAutomationRuntime`, local desktop consent, and the compatibility policy `background_only` / `foreground_fallback`. Background UI operations are bounded to the current BricsCAD process.

The UX defect was that local desktop consent and the compatibility interaction policy could diverge. A user could visibly enable desktop control while `desktop_*` remained blocked by `background_only`.

During implementation, PR #4990 merged restart-safe credential persistence plus a foreground toggle into `main`. #5018 therefore integrates with that released surface instead of rewriting the large transport-oriented Agent Center file: `McpPersistentAgentCenterAugmenter` now injects the dual-capability cards/status and hooks the canonical Resume/Pause/Emergency buttons while preserving the #4990 toggle and secret-persistence behavior.

## Capability state

`bricscad_interaction_policy_get` stays backward compatible and also reports explicit simultaneous capability state:

- `backgroundControl.available=true`
- `backgroundControl.preferred=true`
- `backgroundControl.usesGlobalInput=false`
- `foregroundControl.available` only when both local consent and the compatibility foreground policy are enabled
- `foregroundControl.localConsent`
- `foregroundControl.policyEnabled`
- `foregroundControl.usesGlobalInput=true`
- `defaultRoute="background"`
- `fallback="explicit_only"`
- `implicitForegroundFallback=false`

Existing fields `mode`, `globalInputAllowed`, `defaultMode`, and `processScoped` remain intact.

## Local synchronization

`McpBackgroundHostRuntime` adds local-only helpers:

- `EnableForegroundFromLocalUser()` requires current local desktop consent and switches the compatibility policy to foreground-available.
- `DisableForegroundFromLocalUser()` switches the compatibility policy to `background_only` and is idempotent.

The Agent Center augmenter attaches after the canonical button handlers. Resume first grants local consent and then calls the enable helper. Pause and Emergency Stop revoke foreground policy after the canonical consent/emergency action. Synchronization failure restores `background_only`, revokes foreground access and emits a bounded local error.

Remote callers still cannot enable foreground availability without local desktop consent because `bricscad_interaction_policy_set mode=foreground_fallback` retains its existing consent requirement.

## Agent Center UX

The Agent tab presents two independent capability cards/status rows without replacing the existing transport UI.

### Thao tác nền · Background Control

Always shown as `AVAILABLE · ưu tiên mặc định` while the BricsCAD MCP host is available. Copy explains direct CAD/QS3D/API/command/same-process UI and states that unsupported background UI fails rather than stealing the user's desktop or silently switching to foreground control.

### Thao tác trực tiếp · Foreground Control

Shows current local-consent + policy availability. Existing Resume/Pause/Emergency controls and the #4990 foreground toggle remain local-user controls. Background Control remains available simultaneously.

## Reservation coordination

This lane intentionally does not modify `McpDesktopAutomationRuntime.cs` while reservation #4799 / PR #4946 owns it. PR #4990 / issue #4989 merged during this implementation, releasing `McpPersistentAgentCenterAugmenter.cs`; #5018 rebases onto that merge and extends the released augmenter rather than duplicating it.

A dedicated `preflight-mcp-dual-control-capabilities.py` validates this feature without weakening `preflight-mcp-background-host-control.py` from #4990.

## Safety invariants

- No arbitrary shell, PowerShell, cmd, process launch, eval/script or arbitrary filesystem reader.
- Foreground enablement still requires a local user action.
- Mutating tools retain `confirmMutation=true` and mutation-epoch validation.
- Sensitive reads retain existing confirmation rules.
- Background HWND operations remain same-process and revalidate ownership.
- Background operations never call `desktop_*` implicitly.
- Foreground availability never changes the default route away from background.
- Failures in UI synchronization restore `background_only` and fail closed.

## Verification

The dedicated preflight proves dual capability JSON, local Resume/Pause/Emergency synchronization, separate Agent Center capability cards/status, explicit-only fallback semantics, `BACKGROUND CONTROL:` descriptors, compatibility with #4990 and absence of desktop/global-input/process-launch calls from `McpBackgroundHostRuntime`.

Fresh aggregate/core/V25 CI is required on the exact candidate. Real Windows/BricsCAD interactive behavior remains `LOCAL_ONLY` until exercised on the licensed host.
