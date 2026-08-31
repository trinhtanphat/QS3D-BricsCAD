# MCP dual-control capabilities v2 — design

## Problem

MCP must support both non-disruptive BricsCAD automation and explicit desktop automation. Treating these as one on/off mode creates ambiguity: local desktop consent can be ON while the background-host policy still blocks desktop tools, or a background operation could be tempted to silently fall back to global input.

## Design

Expose two simultaneous capabilities:

- **Background Control**: preferred/default; `cad_*`, `qs3d_*`, bounded command dispatch, and same-process `bricscad_ui_*`; no global mouse/keyboard/focus injection.
- **Foreground Control**: explicit `desktop_*`; available only when the foreground policy and local desktop consent are both enabled.

The interaction policy remains for compatibility, but its status response becomes a capability report as well. `background_only` means foreground is disarmed, not that background is the only capability known to the system. `foreground_fallback` arms foreground but does not change the preferred route.

## Safety invariants

1. `defaultRoute` is `background`.
2. Fallback is `explicit_only`; unsupported background actions fail rather than auto-calling desktop tools.
3. Background-host code remains current-process scoped and has no global-input or shell/process bridge.
4. Foreground permission requires both local consent and policy; either gate disappearing makes global input unavailable.
5. Local Agent Center controls synchronize policy with consent and fail closed.
6. Existing mutation confirmations and sensitive-read confirmations remain independent gates.

## UI integration under concurrent ownership

Issue #5047 owns the canonical `McpAgentControlCenter.cs` during this lane. To avoid a path collision, this implementation uses the already-shipped `McpPersistentAgentCenterAugmenter.cs` to add the two capability summaries and synchronize existing Resume/Pause/Emergency buttons. This is an intentional reservation-safe integration rather than a second competing Agent Center implementation.

## Status schema

`bricscad_interaction_policy_get` preserves legacy fields and adds:

- `backgroundControl.available/preferred/usesGlobalInput`
- `foregroundControl.available/localConsent/policyEnabled/usesGlobalInput`
- `defaultRoute`
- `fallback`
- `implicitForegroundFallback`

`globalInputAllowed` is strict combined availability, not merely policy state.

## Verification

A dedicated source guard proves required contract tokens, same-process message primitives, and absence of known global-input/process-launch tokens in the background runtime. Protected CI remains authoritative for source/build qualification; licensed BricsCAD runtime behavior is separately LOCAL_ONLY.
