# Work claim — Door XLSX error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-door-xlsx-error-redaction-20260812-1114`
- Registered: `2026-08-12T11:14:00+07:00`
- Baseline main SHA: `2d340a93ec16b41bcbb32555162c7e7699ce7075`

## Confirmed defect

`src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs` reflects `ex.Message` from both the top-level `QS3DDOORXLSX` catch and post-export `FinalizeUi(...)`; missing-project guidance is carried through an exception message.

## Reserved scope

- Convert missing-project handling to explicit `BLOCKED` reporting and return.
- Redact raw exception details from export and post-export UI failure paths.
- Preserve save confirmation, read-only project lookup, detached regeneration/build, checked count/area/host summary, XLSX export, and post-commit UI best-effort behavior.
- Add a focused static preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs`
- `scripts/preflight-door-xlsx-error-redaction.py`
- this claim file

## Completion condition

Current `main` keeps the detached Door/Opening export flow and actionable blocked guidance without reflecting runtime exception messages, with focused source regression coverage and exact integration SHAs recorded.