# Work claim — Curtain XLSX error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-xlsx-error-redaction-20260812-1111`
- Registered: `2026-08-12T11:11:00+07:00`
- Baseline main SHA: `41ec60f899c8aff4f73b9896299050c5579399a5`

## Confirmed defect

`src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs` previously reflected `ex.Message` from both the top-level `QS3DCURTAINXLSX` catch and post-export `FinalizeUi(...)`; missing-project guidance was also carried through an exception message.

## Reserved scope

- Convert missing-project handling to explicit `BLOCKED` reporting and return.
- Redact raw exception details from export and post-export UI failure paths.
- Preserve save confirmation, read-only project lookup, detached regeneration/build, checked summary math, XLSX export, and post-commit UI best-effort behavior.
- Add a focused static preflight.

## Validation completed

- Claim registration: `04d1993666494823d9d8a8e3facdc3ed5a1f924c`.
- Source fix: `e87cc1cbca352a6c4877245ae74c057a3af0d289`.
- Focused preflight source: `514d861f88142339c28df5ed476a41c74a5d3b4f`.
- Readback on current `main` confirmed explicit `Curtain XLSX: BLOCKED` guidance, detached snapshot/regeneration, checked panel/glass/frame aggregation, `CurtainWallXlsxExporter.Export(...)`, generic top-level failure text, and generic post-export UI warning.
- Readback confirmed `scripts/preflight-curtain-xlsx-error-redaction.py` pins these contracts and rejects the former missing-project throw, `catch (System.Exception ex)`, `ex.Message`, and raw-detail concatenation.
- Ancestry verification against `main` SHA `4c883ccb9c3c326e63f33247adaa63b956810550` confirmed the source fix and focused preflight are integrated on `main`.
- Python preflight execution, GitHub Actions, build, and licensed BricsCAD V25/V26 runtime were not executed or claimed PASS.

## Completion condition

Completed: current `main` keeps the existing detached export flow and actionable blocked guidance without reflecting runtime exception messages, with focused source regression coverage and exact integration SHAs recorded.