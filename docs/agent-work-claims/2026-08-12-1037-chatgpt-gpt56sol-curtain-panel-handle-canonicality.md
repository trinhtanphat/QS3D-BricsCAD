# Work claim — Generated Curtain Panel handle token canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-curtain-panel-handle-canonicality`
- Registered: `2026-08-12T10:37:00+07:00`
- Last Updated: `2026-08-12T10:37:00+07:00`
- Baseline main SHA: `3ae1b1c4050a1033083208ac19478c2e3c85b887`
- Priority: P1 — malformed persisted generated Curtain Panel owner handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-CURTAIN-PANEL-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedCurtainPanelHealthService.Inspect(...)` preserves delimiter tokens with `StringSplitOptions.None` but trims each `GeneratedCurtainPanelHandles` token before validating it. A persisted token such as `" A "` therefore passes as a valid hexadecimal handle with no canonicality Error. Sibling generated-output health providers now enforce the writer-owned contract that surrounding token whitespace is fail-visible while duplicate/ownership/source/liveness checks continue with the trimmed handle; lowercase canonical hex remains accepted.

## Coordination

Curtain Panel null-health, empty-handle-token and fingerprint piece-bound lanes are completed. No current commit/claim search found a Curtain Panel padded-handle canonicality lane. Current Foundation/Wall mesh, QSDB XML, semantic-tag, SelectionState and release/preflight claims do not overlap this file.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelHandleCanonicalitySmoke.cs`
- this claim file

## Intended contract

- A valid non-empty Curtain Panel hex handle token with surrounding whitespace emits Error `CURTAIN_PANEL_GENERATED_HANDLE_NON_CANONICAL`.
- Continue duplicate, generated-ownership, SourceHandles, liveness, count/build-state/geometry/mode/category/stale checks using the trimmed handle.
- Lower-case canonical hex remains accepted; no casing rule is added.
- Empty delimiter tokens continue to emit existing `INVALID_CURTAIN_PANEL_GENERATED_HANDLE` diagnostics.
- Do not modify curtain panel builders/fingerprint/planner, generated ownership policy, CAD runtime code, persistence or unrelated diagnostics.

## Validation plan

Add an auto-registered Core smoke covering padded handle + trimmed live lookup, lowercase canonical control and preservation of completed empty-token invalid behavior. Review exact PR diff, squash-merge guarded, read back source/test and verify ancestry.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Padded generated Curtain Panel handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed with exact commit/PR evidence.
