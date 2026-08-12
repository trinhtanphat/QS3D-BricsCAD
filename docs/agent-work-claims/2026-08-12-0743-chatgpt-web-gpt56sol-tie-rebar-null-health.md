# Work claim — Generated Tie Rebar health null-element fail-visible

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-tie-rebar-null-health`
- Registered: `2026-08-12T07:43:00+07:00`
- Completed: `2026-08-12T07:43:00+07:00`
- Baseline main SHA: `3b5245820b5d346a4b8fdfbfac30ba97cd9d844e`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-TIE-REBAR-NULL-HEALTH`

## Confirmed defect

`GeneratedTieRebarHealthService.Inspect(ProjectState, ...)` and its internal ownership-index traversal silently skipped null semantic elements. A malformed project containing a null semantic element could therefore be normalized away inside the standalone provider. Newer generated-health lanes use a fail-visible contract: direct inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts that bounded failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- `scripts/preflight-tie-rebar-null-health.py`
- this claim file

Tie-rebar builders, rebar notation/fabrication, quantity semantics, ownership policy/index, CAD runtime code, and `ComprehensiveModelHealthService` were not modified.

## Completed implementation

- Claim registration: `fc634c0f32b4aa5cba5ef94d30236f6dd9083405`.
- Source fix: `54aed82ce2fb9f34b675c3926b7917764a35ed8f` (`fix(health): fail visible on null tie rebar entries`).
- Focused regression gate: `f89dc1763648a561f2eade6437c96198babe76ae` (`test(health): pin tie rebar null fail-visible`).
- Both the main diagnostic traversal and `BuildOwnershipIndex(...)` now reject null project elements with `InvalidOperationException`; there is no remaining silent null continue in this service.
- Existing handle/count/diameter/spacing/category/stale/ownership diagnostics remain unchanged.
- Composite health remains unchanged and continues to register this provider through `AddSafely`, whose bounded diagnostic-data filter includes `InvalidOperationException` and emits stable `HEALTH_PROVIDER_FAILED` Errors.

## Validation actually performed

- Re-fetched current `main` source after source/gate commits; source blob is `5bd7efe0caa8100722f8ee30b876400800a7fcb1` with fail-closed checks in both traversals.
- Re-fetched focused gate from `main`; gate blob is `21ff9dcebcdd47fb3339a0355698d9b67f490a72` and pins direct/provider behavior, aggregate compatibility, neighboring tie-rebar diagnostics, and absence of silent null continue.
- No GitHub Actions/build/release workflow was dispatched. No executable Core smoke, full solution build, or BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied on merged source contract: standalone Tie Rebar health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed `COMPLETED`.
