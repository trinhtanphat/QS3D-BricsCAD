# Work claim — Material XLSX error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-xlsx-error-redaction-20260812-1108`
- Registered: `2026-08-12T11:08:00+07:00`
- Baseline main SHA: `3f0994a29bd445e142e854844a4db27c3095d703`
- Priority: owner-requested continue-all residual export diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs` previously had two user-visible exception reflection paths: the top-level `QS3DMATERIALXLSX` catch reported `"QS3DMATERIALXLSX lỗi: " + ex.Message`, and `FinalizeUi(...)` wrote `"[QS3D] Cảnh báo UI sau export: " + ex.Message`. Runtime exception messages could expose filesystem/provider/environment details.

The command also signaled a missing QS3D project by throwing `InvalidOperationException(...)` and relying on the raw exception message for user guidance. This was converted to an explicit blocked-report path so privacy hardening does not remove actionable guidance.

## Reserved scope

- Redact raw exception-message reflection from both Material XLSX failure paths.
- Preserve save-confirmation ordering, read-only project lookup, detached snapshot regeneration, authoritative material schedule build, checked element-count aggregation, XLSX export, and post-export UI best-effort semantics.
- Preserve actionable missing-project guidance as an explicit blocked status rather than an exception-derived message.
- Add one focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs`
- `scripts/preflight-material-xlsx-error-redaction.py`
- this claim file

## Excluded scope

- No material schedule semantics, XLSX serializer/row snapshot changes, project persistence, save-dialog ordering, Actions dispatch, release publication, force push, build PASS, or BricsCAD runtime PASS claim.

## Validation completed

- Claim registration: `968d3da30370c522504dda1147648405bad499a6`.
- Source fix: `0fda3dfc5da53cdb7be739dbd6900faef21d7b74`.
- Focused preflight source: `d691ccd599bbdfb0e8c64f9b5037441c15ab4bcc`.
- Readback on current `main` confirmed the missing-project case is explicit `Material XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.` followed by `return`, rather than an exception-derived message.
- Readback confirmed the top-level catch reports only `QS3DMATERIALXLSX lỗi: không thể xuất bảng vật liệu.` and `FinalizeUi(...)` reports only the generic post-export UI warning while retaining the best-effort post-commit contract.
- Readback confirmed save confirmation, read-only project lookup, detached `ProjectStateSnapshot`, detached regeneration, authoritative schedule build, checked element-count aggregation, and `MaterialUsageXlsxExporter.Export(...)` remain intact.
- Readback confirmed `scripts/preflight-material-xlsx-error-redaction.py` pins those contracts and rejects the former missing-project throw, `catch (System.Exception ex)`, `ex.Message`, and raw-detail concatenation.
- Ancestry verification against `main` SHA `d691ccd599bbdfb0e8c64f9b5037441c15ab4bcc` confirmed the source fix is an ancestor and the focused preflight commit is current HEAD.
- Python preflight execution, GitHub Actions, build, and licensed BricsCAD V25/V26 runtime were not executed or claimed PASS through this connector session.

## Completion condition

Completed: current `main` no longer reflects exception messages from `QS3DMATERIALXLSX` or its post-export finalization, missing-project guidance remains explicit, existing detached export behavior remains source-pinned, focused regression source exists, and exact integration evidence is recorded above.