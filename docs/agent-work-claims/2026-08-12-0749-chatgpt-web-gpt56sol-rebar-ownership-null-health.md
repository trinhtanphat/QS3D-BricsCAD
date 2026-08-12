# Work claim — Generated Rebar Ownership health null-element fail-visible

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-rebar-ownership-null-health`
- Registered: `2026-08-12T07:49:00+07:00`
- Completed: `2026-08-12T07:49:00+07:00`
- Baseline main SHA: `7014868bd5ee1da9fda48f3c9ae90b35bc6fce47`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-REBAR-OWNERSHIP-NULL-HEALTH`

## Confirmed defect

`GeneratedRebarOwnershipHealthService.Inspect(ProjectState)` silently skipped null semantic elements. A malformed project containing a null semantic element could therefore produce a false-clean result from this standalone provider. Newer health-provider lanes use a fail-visible contract: direct inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts the bounded failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs`
- `scripts/preflight-rebar-ownership-null-health.py`
- this claim file

Generated rebar builders, ownership policy/index, rebar notation/fabrication, CAD runtime ownership, and `ComprehensiveModelHealthService` were not modified.

## Completed implementation

- Claim registration: `6b90f2764c0c30c90c77ed224c609b02b896ecd1`.
- Source fix: `3a766aeb9192ae12d42fc4f9bd2d27b05baaae37` (`fix(health): fail visible on null rebar ownership entries`).
- Focused regression gate: `9677d1220b676f5db44b9a03d0a843ec4f257829` (`test(health): pin rebar ownership null fail-visible`).
- Direct `GeneratedRebarOwnershipHealthService.Inspect(...)` now rejects null project elements with `InvalidOperationException` instead of silently skipping them.
- Existing `REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT` detection and owner-key enumeration remain unchanged.
- Composite health remains unchanged and continues to register this provider through `AddSafely`, whose bounded diagnostic-data filter includes `InvalidOperationException` and emits stable `HEALTH_PROVIDER_FAILED` Errors.

## Validation actually performed

- Re-fetched current `main` source after source/gate commits; source blob is `46483862fd92f318b83200e903d378e79fcc5311` with direct null fail-closed behavior.
- Re-fetched focused gate from `main`; gate blob is `39943fc4879508cf6929da3d771ee298adeeb36b` and pins direct/provider behavior, aggregate compatibility, conflict diagnostics, and absence of silent null continue.
- No GitHub Actions/build/release workflow was dispatched. No executable Core smoke, full solution build, or BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied on merged source contract: standalone Rebar Ownership health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed `COMPLETED`.
