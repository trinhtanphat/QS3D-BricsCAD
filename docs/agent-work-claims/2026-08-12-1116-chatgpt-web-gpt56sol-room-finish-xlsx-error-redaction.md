# Work claim — Room Finish XLSX error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish-xlsx-error-redaction-20260812-1116`
- Registered: `2026-08-12T11:16:00+07:00`
- Baseline main SHA: `72cc2b6e08c861bb4b0dd1e9f77687207666df86`

## Confirmed defect

`src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs` previously reflected `ex.Message` from both the top-level `QS3DFINISHXLSX` catch and post-export `FinalizeUi(...)`; missing-project guidance was carried through an exception message.

## Reserved scope

- Convert missing-project handling to explicit `BLOCKED` reporting and return.
- Redact raw exception details from export and post-export UI failure paths.
- Preserve save confirmation, read-only project lookup, detached regeneration/build, checked count/primary-quantity summary, XLSX export, and post-commit UI best-effort behavior.
- Add a focused static preflight.

## Validation completed

- Claim registration: `4022c487b74ffe4277b0072c4ccc726b86ab8aaa`.
- Source fix: `f8b0408f385a705267427bc32ca6a33d81e4c5f1`.
- Focused preflight source: `51d03fbd6ec244ead5895adeddcbb42a506aa06a`.
- Readback on current `main` confirmed explicit `HT_Phòng XLSX: BLOCKED` guidance, detached snapshot/regeneration, checked count/primary-quantity summary, `RoomFinishXlsxExporter.Export(...)`, generic top-level failure text, and generic post-export UI warning.
- Readback confirmed `scripts/preflight-room-finish-xlsx-error-redaction.py` pins these contracts and rejects the former missing-project throw, `catch (System.Exception ex)`, `ex.Message`, and raw-detail concatenation.
- Ancestry verification against `main` SHA `2d989fb24b465c77a2803dca77b00575f2047eb4` confirmed the source fix and focused preflight are integrated on `main`.
- Python preflight execution, GitHub Actions, build, and licensed BricsCAD V25/V26 runtime were not executed or claimed PASS.

## Completion condition

Completed: current `main` keeps the detached Room Finish export flow and actionable blocked guidance without reflecting runtime exception messages, with focused source regression coverage and exact integration SHAs recorded.