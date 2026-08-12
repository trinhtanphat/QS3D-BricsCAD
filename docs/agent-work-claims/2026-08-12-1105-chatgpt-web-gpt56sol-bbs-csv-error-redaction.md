# Work claim — BBS CSV error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bbs-csv-error-redaction-20260812-1105`
- Registered: `2026-08-12T11:05:00+07:00`
- Baseline main SHA: `c360c8f5867454a6cc432fd1c4e13c19f4d0be55`
- Priority: owner-requested continue-all residual export diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/BbsCsvCommands.cs` has two user-visible exception reflection paths: the top-level `QS3DBBSCSV` catch reports `"QS3DBBSCSV lỗi: " + ex.Message`, and `FinalizeUi(...)` catches post-export UI failures and writes `"[QS3D] Cảnh báo UI sau export: " + ex.Message` to the Editor. Runtime exception messages may expose filesystem/provider/environment details.

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

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace both raw exception-detail paths with stable generic text while preserving best-effort post-export UI behavior.
- Add focused Python source preflight covering read-only/detached export contracts, generic top-level failure, generic post-export UI warning, and absence of `ex.Message`.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects exception messages from `QS3DBBSCSV` or its post-export UI finalization, the existing detached/validated export flow remains source-pinned, focused regression source exists, and this claim is `COMPLETED` with exact integration evidence.