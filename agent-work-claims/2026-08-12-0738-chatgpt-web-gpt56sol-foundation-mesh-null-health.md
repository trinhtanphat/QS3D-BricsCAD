# Work claim — Generated Foundation Mesh health null-element fail-visible

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-foundation-mesh-null-health`
- Registered: `2026-08-12T07:38:00+07:00`
- Completed: `2026-08-12T07:38:00+07:00`
- Baseline main SHA: `7f3d6d910a405a40829b0391ac1f77280c6feff1`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-FOUNDATION-MESH-NULL-HEALTH`

## Confirmed defect

`GeneratedFoundationMeshHealthService.Inspect(ProjectState, ...)` silently skipped null semantic elements. Its ownership-index traversal also skipped the same malformed entries before the diagnostic loop, so malformed project state could be normalized away inside the standalone provider. The repository's newer provider contract is fail-visible: direct generated-health inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts that bounded diagnostic-data failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs`
- `scripts/preflight-foundation-mesh-null-health.py`
- this claim file

Foundation mesh builders, quantity semantics, ownership policy/index, CAD runtime code, and `ComprehensiveModelHealthService` were not modified.

## Completed implementation

- Claim registration: `c91cc2e18c3ad424db50a53e3f9994938ffd869c`.
- Source fix: `5a5c90ee502ecad52ffef6221f56782bf4b1e661` (`fix(health): fail visible on null foundation mesh entries`).
- Focused regression gate: `68c85aa5458cf671901b113aeb818f5478e791ab` (`test(health): pin foundation mesh null fail-visible`).
- Both the main diagnostic traversal and `BuildOwnershipIndex(...)` now reject null project elements with `InvalidOperationException`; there is no remaining `if (element == null) continue;` silent path in this service.
- Existing handle/count/diameter/spacing/cover/faces/mode/category/stale diagnostics remain unchanged.
- Composite health remains unchanged and continues to register this provider through `AddSafely`, whose bounded diagnostic-data filter includes `InvalidOperationException` and emits stable `HEALTH_PROVIDER_FAILED` Errors.

## Validation actually performed

- Re-fetched current `main` source after source/gate commits; source blob is `8e0344d7b24a8e4e492bb3ed7f7fc3a429e452d2` with fail-closed checks in both traversals.
- Re-fetched focused gate from `main`; gate blob is `b7dda4e783522fea7812e0166ee867051cea7a6d` and pins both direct/provider behavior and aggregate compatibility while forbidding silent null continue.
- Initial claim creation received a moving-`main` 409; HEAD was refreshed and the claim was created without force or overwrite.
- No GitHub Actions/build/release workflow was dispatched. No executable Core smoke, full solution build, or BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied on merged source contract: standalone Foundation Mesh health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed `COMPLETED`.
