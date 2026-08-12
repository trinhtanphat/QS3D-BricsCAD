# Work claim — safe generated ownership health redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-safe-generated-ownership-redaction`
- Registered: `2026-08-12T07:32:00+07:00`
- Baseline main SHA: `38d117137dd563556bb858ade2e659afe8a91d4b`
- Priority: P1 — safe ownership diagnostics must preserve malformed-project visibility without reflecting raw canonical validation detail.
- Task Key: `CORE-SAFE-GENERATED-OWNERSHIP-REDACTION`

## Confirmed defect

`SafeGeneratedHandleOwnershipHealthService.Inspect(...)` catches `InvalidOperationException` from `GeneratedHandleOwnershipIndex.Build(project)` and appends `ex.Message` verbatim to `GENERATED_HANDLE_OWNERSHIP_INVALID_PROJECT`. The canonical index includes persisted semantic identities in validation messages (for example duplicate element IDs), so the safe wrapper reflects raw project detail. Because the wrapper catches the exception internally, aggregate `ComprehensiveModelHealthService` provider redaction cannot sanitize it.

This is a follow-up to the already completed malformed-project visibility lane. The raw index must remain fail-closed and the safe wrapper must remain fail-visible; only the reflected exception detail is in scope.

## Reserved scope

- `src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

`GeneratedHandleOwnershipIndex`, ownership policy, native generated builders/runtime, and the prior null-element contract are excluded.

## Intended contract

- Preserve `GENERATED_HANDLE_OWNERSHIP_INVALID_PROJECT` with `HealthSeverity.Error` when canonical index validation throws `InvalidOperationException`.
- Replace raw `Exception.Message` reflection with stable text.
- Preserve valid-project ownership conflict detection and same-logical-slot de-duplication.
- Preserve read-only inspection; do not weaken raw index validation.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Malformed-project ownership errors remain fail-visible without raw canonical validation detail, focused regression coverage pins the contract, and this claim is closed after merged-main readback.
