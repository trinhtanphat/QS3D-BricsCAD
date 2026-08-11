# Work claim — Quantity Settings create missing category rule UI

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-category-rule-create-ui-20260811-2344`
- Registered: `2026-08-11T23:44:00+07:00`
- Completed: `2026-08-12T00:08:00+07:00`
- Baseline main SHA: `e9bb3ca787dc3554a75cf8a55dbd190810823ab3`
- Priority: P1 — complete the owner-requested rule authoring workflow for imported/partial Quantity Settings where an integer category exists only in intersection rules and therefore has no editable Category Rule row.

## Confirmed gap

`QuantityCalculationMatrixDiagnostics` explicitly reports `IntersectionOnlyCategoryCodes`, and `QuantityCalculationRuleSet.TryGetCategoryRule(...)` returns false when no category row exists. `QS3DSETUP` displayed only existing `CategoryRows`; unlike the previously completed missing A → B action, it offered no way to add a Category Rule for an intersection-only code without editing JSON outside QS3D.

## Reserved scope

- expose intersection-only category codes in the existing `Thông số Cốp pha` workflow;
- add one contextual `Tạo quy tắc loại` action for the selected missing code;
- confirmed creation adds exactly one in-memory `QuantityCategoryRuleRow` with conservative defaults: `ExtractSide=false`, `ExtractBottom=false`, `FaceAngleThresholdDeg=30`; no quantity extraction becomes enabled by creation itself;
- do not persist until the existing `Lưu Cài Đặt` flow;
- after creation, refresh category choices and the intersection browser without synthesizing intersection rules.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-category-rule-create-ui.py`
- this claim file for close-out

## Explicit exclusions

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs` and the clone-cardinality lane;
- `QuantitySettingsStore.cs`, rule engine/runtime arithmetic, matrix diagnostics implementation;
- command-line `QS3DRULECREATE`, intersection-rule creation semantics, project/CAD mutation;
- Build3D, geometry, updater/release, documentation/native artifact claims;
- GitHub Actions and licensed V25 runtime qualification.

## Implementation evidence

- `37d2eefc4accf5eb8671823b856cbaa1549e0934` — implementation commit on `agent/chatgpt-rule-category-ui-20260811-2344`: added the contextual category-rule creation action, conservative default row creation, read-only/duplicate/cancel guards, refresh behavior and focused static preflight.
- `4522f04ba9b845a7bdb64dc936d118a0cdaa3ca2` — PR #546 squash-merged the complete implementation onto `main` as `feat(quantity): create missing category rules in setup UI (#546)`.
- The merge changes only the two Quantity Settings WPF source files plus the focused static preflight; Core quantity arithmetic/storage, Build3D and concurrent ownership lanes were not modified.

## Validation actually performed

- Reviewed the exact implementation source and merge patch after push.
- Verified missing category candidates are derived from selected intersection source/target codes absent from `CategoryRows`, and existing category codes cannot be duplicated.
- Verified future-schema read-only state disables/blocks creation before confirmation.
- Verified confirmed creation appends exactly one in-memory category row with `ExtractSide=false`, `ExtractBottom=false`, `FaceAngleThresholdDeg=30d`, then rebuilds the intersection browser.
- Verified the handler does not call `_store.Save`, Import/Export, project lifecycle APIs, CAD transactions/selections or direct JSON/file writes; persistence remains the existing Save action.
- Added `scripts/preflight-quantity-category-rule-create-ui.py` to pin the XAML/source contract. The preflight source was reviewed remotely; no claim is made that a local runner executed it in this session.
- GitHub Actions were not dispatched for this work, and no licensed BricsCAD V25/WPF runtime PASS is claimed.

## Coordination

Concurrent Core quantity, project-session recovery, Build3D/geometry, release, persistence and other agent lanes remained outside this scope. High `main` churn was handled by rebasing the prepared content onto current `main`, then merging PR #546 without force-pushing or overwriting concurrent work.

## Completion condition

Completed: a user can repair an intersection-only category directly in `QS3DSETUP` without external JSON editing; creation is explicit, non-destructive and unsaved until the existing Save action, duplicate/cancel/future-schema paths remain fail-closed, focused static coverage is on `main`, and the capability is merged with exact SHA evidence above.