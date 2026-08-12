# Work claim — Room Finish XLSX error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-xlsx-error-redaction-20260812-1116`
- Registered: `2026-08-12T11:16:00+07:00`
- Baseline main SHA: `72cc2b6e08c861bb4b0dd1e9f77687207666df86`

## Confirmed defect

`src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs` reflects `ex.Message` from both the top-level `QS3DFINISHXLSX` catch and post-export `FinalizeUi(...)`; missing-project guidance is carried through an exception message.

## Reserved scope

- Convert missing-project handling to explicit `BLOCKED` reporting and return.
- Redact raw exception details from export and post-export UI failure paths.
- Preserve save confirmation, read-only project lookup, detached regeneration/build, checked count/primary-quantity summary, XLSX export, and post-commit UI best-effort behavior.
- Add a focused static preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs`
- `scripts/preflight-room-finish-xlsx-error-redaction.py`
- this claim file

## Completion condition

Current `main` keeps the detached Room Finish export flow and actionable blocked guidance without reflecting runtime exception messages, with focused source regression coverage and exact integration SHAs recorded.