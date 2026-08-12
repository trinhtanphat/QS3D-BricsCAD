# Work claim — Generated Rebar handle token canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-generated-rebar-handle-canonicality`
- Registered: `2026-08-12T10:01:00+07:00`
- Last Updated: `2026-08-12T10:01:00+07:00`
- Baseline main SHA: `ae11ec1b0224a884c4fd7e59e87e33de7b7ea377`
- Priority: P1 — malformed persisted generated-rebar owner handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-GENERATED-REBAR-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedRebarHealthService.InspectSet(...)` trims each `GeneratedRebarHandles` / `GeneratedShapeRebarHandles` token before validation and then uses the trimmed value for all checks. A persisted token such as `" A "` therefore passes as a valid hex handle with no canonicality error. Current Model Health already enforces the sibling generated-solid contract: a padded `GeneratedSolidHandle` is fail-visible while downstream live/ownership lookup continues using the trimmed handle; lower-case hex remains valid. Generated rebar health currently lacks that equivalent persisted-token integrity check.

## Coordination

The older `CORE-GENERATED-REBAR-EMPTY-HANDLE-TOKEN` claim is `COMPLETED` on current `main`; its implementation intentionally changed `InspectSet(...)` to `StringSplitOptions.None` so empty delimiter tokens reach existing invalid-handle diagnostics. This lane is independent and preserves that completed behavior.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedRebarHandleCanonicalitySmoke.cs`
- this claim file

## Intended contract

- For both longitudinal and shape generated rebar handle sets, valid non-empty hex tokens with surrounding whitespace emit an Error canonicality diagnostic.
- Continue all existing duplicate, ownership, SourceHandles, liveness, count and diameter checks using the trimmed handle so the canonicality issue does not create false missing/conflict results.
- Lower-case canonical hex remains accepted; no hex-letter casing policy is added.
- Empty/whitespace delimiter tokens continue to emit the existing `INVALID_<PREFIX>_GENERATED_HANDLE` diagnostics.
- Do not change builders, generated ownership policy, CAD runtime code, persistence, or unrelated diagnostics.

## Validation plan

Add an auto-registered Core smoke covering padded longitudinal and shape handles, trimmed live-handle lookup, lowercase canonical controls, and preservation of existing empty-token invalid diagnostics. Re-read current source before product write and review exact PR diff before squash merge.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Padded generated rebar/shape-rebar handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed with exact commit/PR evidence.
