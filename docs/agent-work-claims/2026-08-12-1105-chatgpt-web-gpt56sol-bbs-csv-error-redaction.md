# Work claim — BBS CSV error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bbs-csv-error-redaction-20260812-1105`
- Registered: `2026-08-12T11:05:00+07:00`
- Baseline main SHA: `c360c8f5867454a6cc432fd1c4e13c19f4d0be55`
- Priority: owner-requested continue-all residual export diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/BbsCsvCommands.cs` previously had two user-visible exception reflection paths: the top-level `QS3DBBSCSV` catch reported `"QS3DBBSCSV lỗi: " + ex.Message`, and `FinalizeUi(...)` caught post-export UI failures and wrote `"[QS3D] Cảnh báo UI sau export: " + ex.Message` to the Editor. Runtime exception messages could expose filesystem/provider/environment details.

## Reserved scope

- Redact raw exception-message reflection from both BBS CSV failure paths.
- Preserve read-only project lookup, detached project snapshot/regeneration, pre-save validation, save dialog, CSV export, checked total-weight summary, and the contract that post-export UI reporting is best effort only.
- Preserve protected Palette/Editor reporting without turning a UI failure after file commit into an export failure.
- Add one focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/BbsCsvCommands.cs`
- `scripts/preflight-bbs-csv-error-redaction.py`
- this claim file

## Excluded scope

- No changes to BBS row semantics, freshness/regeneration ordering, CSV format, atomic exporter behavior, save-dialog ordering, project persistence, Actions dispatch, release publication, force push, build PASS, or BricsCAD runtime PASS claim.

## Validation completed

- Claim registration: `302f9e95793391195ee4b162f089aba01dfffa35`.
- Source fix: `451cb3eda9d851ccb3d45a371617d560c34a0924`.
- Focused preflight source: `32eada53114f72638f087249840aecb69537ed17`.
- Readback on current `main` confirmed the top-level catch now reports `QS3DBBSCSV lỗi: không thể xuất BBS CSV.` without retaining an exception variable or reflecting its message.
- Readback confirmed `FinalizeUi(...)` keeps the post-commit best-effort contract but emits only `Cảnh báo UI sau export: không thể cập nhật giao diện sau khi file đã được xuất.`.
- Readback confirmed detached `ProjectStateSnapshot`, detached regeneration, schedule build, checked total-weight aggregation, save confirmation, and `RebarCsvExporter.Export(...)` remain intact.
- Readback confirmed `scripts/preflight-bbs-csv-error-redaction.py` pins those contracts and rejects `catch (System.Exception ex)`, `ex.Message`, and raw-detail concatenation.
- Ancestry verification against `main` SHA `cdd23aa6b1cc207264d758e97168ca9dc88dcd76` confirmed both source fix and focused preflight commits are ancestors.
- Python preflight execution, GitHub Actions, build, and licensed BricsCAD V25/V26 runtime were not executed or claimed PASS through this connector session.

## Completion condition

Completed: current `main` no longer reflects exception messages from `QS3DBBSCSV` or its post-export UI finalization, the existing detached/validated export flow remains source-pinned, focused regression source exists, and exact integration evidence is recorded above.