# Work claim — Material XLSX error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-xlsx-error-redaction-20260812-1108`
- Registered: `2026-08-12T11:08:00+07:00`
- Baseline main SHA: `3f0994a29bd445e142e854844a4db27c3095d703`
- Priority: owner-requested continue-all residual export diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs` has two user-visible exception reflection paths: the top-level `QS3DMATERIALXLSX` catch reports `"QS3DMATERIALXLSX lỗi: " + ex.Message`, and `FinalizeUi(...)` writes `"[QS3D] Cảnh báo UI sau export: " + ex.Message`. Runtime exception messages may expose filesystem/provider/environment details.

The command also currently signals a missing QS3D project by throwing `InvalidOperationException(...)` and relying on the raw exception message for user guidance. Redacting the top-level catch without preserving that explicit blocked-state guidance would regress UX, so this lane will make the missing-project case an explicit `Report(...); return;` path while keeping the existing read-only detached-export contract.

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

## Validation plan

- Re-fetch current source after claim registration before editing.
- Convert only the missing-project throw to explicit blocked reporting, replace both raw exception-detail paths with stable generic messages, and keep post-commit UI reporting best effort.
- Add focused Python source preflight pinning save confirmation, read-only/detached flow, blocked path, generic failure paths, and absence of `ex.Message`.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects exception messages from `QS3DMATERIALXLSX` or its post-export finalization, missing-project guidance remains explicit, existing detached export behavior remains source-pinned, focused regression source exists, and this claim is `COMPLETED` with exact integration evidence.