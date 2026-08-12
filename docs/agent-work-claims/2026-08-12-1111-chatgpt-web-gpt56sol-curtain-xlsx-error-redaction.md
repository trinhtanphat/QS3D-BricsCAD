# Work claim — Curtain XLSX error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-xlsx-error-redaction-20260812-1111`
- Registered: `2026-08-12T11:11:00+07:00`
- Baseline main SHA: `41ec60f899c8aff4f73b9896299050c5579399a5`

## Confirmed defect

`src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs` reflects `ex.Message` from both the top-level `QS3DCURTAINXLSX` catch and post-export `FinalizeUi(...)`. Missing-project guidance is also currently carried through an exception message.

## Reserved scope

- Convert missing-project handling to explicit `BLOCKED` reporting and return.
- Redact raw exception details from export and post-export UI failure paths.
- Preserve save confirmation, read-only project lookup, detached regeneration/build, checked summary math, XLSX export, and post-commit UI best-effort behavior.
- Add a focused static preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs`
- `scripts/preflight-curtain-xlsx-error-redaction.py`
- this claim file

## Completion condition

Current `main` keeps the existing detached export flow and actionable blocked guidance without reflecting runtime exception messages, with focused source regression coverage and exact integration SHAs recorded.