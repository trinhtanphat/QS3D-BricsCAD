# Work claim — Generated Tie Rebar handle token canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-tie-rebar-handle-canonicality`
- Registered: `2026-08-12T10:23:00+07:00`
- Last Updated: `2026-08-12T10:23:00+07:00`
- Baseline main SHA: `be17a4d1f2121e536a07df74e7239888c95a8f59`
- Priority: P1 — malformed persisted generated Tie Rebar owner handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-TIE-REBAR-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedTieRebarHealthService.Inspect(...)` splits `GeneratedTieRebarHandles` with `StringSplitOptions.None` but immediately trims every token before validating it. A persisted token such as `" A "` therefore passes as valid hex and can complete ownership/live lookup with no canonicality issue. Current sibling generated-solid and generic generated-rebar diagnostics already enforce the writer-owned contract that surrounding token whitespace is fail-visible while downstream lookup continues using the trimmed handle; lowercase canonical hex remains accepted.

## Coordination

The prior Tie Rebar null-health claim is `COMPLETED`, and the empty-handle-token lane is also completed. No current commit/claim search found a Tie Rebar padded-handle canonicality lane. Open PR #747 is Template Profile XML preflight and does not overlap this file.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedTieRebarHandleCanonicalitySmoke.cs`
- this claim file

## Intended contract

- A valid non-empty Tie Rebar hex handle token with surrounding whitespace emits Error `TIE_REBAR_GENERATED_HANDLE_NON_CANONICAL`.
- Continue duplicate, ownership, SourceHandles, liveness, count, diameter, spacing, category and stale checks using the trimmed handle so canonicality does not create false missing/conflict results.
- Lower-case canonical hex remains accepted; no hex-letter casing rule is added.
- Empty/whitespace delimiter tokens continue to emit existing `INVALID_TIE_REBAR_GENERATED_HANDLE` diagnostics.
- Do not modify tie builders, generated ownership policy, CAD runtime code, persistence, quantity semantics, or unrelated diagnostics.

## Validation plan

Add an auto-registered Core smoke proving padded Tie handle fails visible while trimmed live lookup stays valid, lowercase canonical handle remains accepted, and the completed empty-token invalid behavior remains intact. Review exact PR diff before squash merge, then read back source/test and verify merge ancestry on moving `main`.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Padded generated Tie Rebar handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed with exact commit/PR evidence.
