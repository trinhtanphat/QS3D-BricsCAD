# Work claim — Generated Rebar handle token canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-generated-rebar-handle-canonicality`
- Registered: `2026-08-12T10:01:00+07:00`
- Completed: `2026-08-12T10:04:00+07:00`
- Last Updated: `2026-08-12T10:04:00+07:00`
- Baseline main SHA: `ae11ec1b0224a884c4fd7e59e87e33de7b7ea377`
- Claim commit: `94f724ea8d63caab76aea00d24ecdfc23f912536`
- Priority: P1 — malformed persisted generated-rebar owner handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-GENERATED-REBAR-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedRebarHealthService.InspectSet(...)` trimmed each `GeneratedRebarHandles` / `GeneratedShapeRebarHandles` token before validation and then used the trimmed value for all checks. Persisted tokens such as `" A "` therefore passed as valid hex handles with no canonicality error, unlike the established generated-solid health contract.

## Completed scope

- Longitudinal and shape generated-rebar handle sets now emit `REBAR_GENERATED_HANDLE_NON_CANONICAL` / `SHAPE_REBAR_GENERATED_HANDLE_NON_CANONICAL` Errors for valid hex tokens with surrounding whitespace.
- Existing duplicate, ownership, SourceHandles, liveness, count and diameter checks continue using the trimmed handle.
- Lower-case canonical hex remains valid.
- The prior empty-token fail-closed contract remains intact: `StringSplitOptions.None` is unchanged and empty/whitespace delimiter tokens still reach existing INVALID diagnostics.

## Implementation evidence

- Source branch commit: `b33b73fd3845f3bb9b3060fc8368077bca2be124`
- Regression branch commit: `ef4a5823628b0e6adf233b85173f5eb56139b770`
- Pull request: `#734`
- Squash merge on `main`: `17717bdb444d385a8954dbe09e16638bddf34e4b`
- Main readback source blob: `d2f4fb4ed14d073a9ce9fdb57a0742c0c54d11b0`
- Main readback smoke blob: `7be1d811657a26bc2b17e61f7a1e137f31011949`

## Validation boundary

Exact PR diff and post-merge source/test readback were verified. GitHub Actions, full build, executable smoke and licensed BricsCAD V25/V26 runtime were not run in this hosted session, so no runtime PASS is claimed.

## Completion

Padded generated rebar/shape-rebar handle tokens are fail-visible without changing downstream trimmed-handle semantics. Claim released as completed.
