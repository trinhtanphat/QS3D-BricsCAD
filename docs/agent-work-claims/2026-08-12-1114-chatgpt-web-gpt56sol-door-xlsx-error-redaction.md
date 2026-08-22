# Work claim — Door XLSX error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-door-xlsx-error-redaction-20260812-1114`
- Registered: `2026-08-12T11:14:00+07:00`
- Baseline main SHA: `2d340a93ec16b41bcbb32555162c7e7699ce7075`

## Confirmed defect

`src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs` previously reflected `ex.Message` from both the top-level `QS3DDOORXLSX` catch and post-export `FinalizeUi(...)`; missing-project guidance was carried through an exception message.

## Reserved scope

- Convert missing-project handling to explicit `BLOCKED` reporting and return.
- Redact raw exception details from export and post-export UI failure paths.
- Preserve save confirmation, read-only project lookup, detached regeneration/build, checked count/area/host summary, XLSX export, and post-commit UI best-effort behavior.
- Add a focused static preflight.

## Validation completed

- Claim registration: `428cad9e13c8ec5486aaae7f7cf3321215778221`.
- Source fix: `be8600e157410827f55e0f382dd86f0a74567973`.
- Focused preflight source: `17403bcbbffe45c4c691b7718fdc246aa0086709`.
- Readback on current `main` confirmed explicit `Door XLSX: BLOCKED` guidance, detached snapshot/regeneration, checked count/area plus host summary, `DoorOpeningXlsxExporter.Export(...)`, generic top-level failure text, and generic post-export UI warning.
- Readback confirmed `scripts/preflight-door-xlsx-error-redaction.py` pins these contracts and rejects the former missing-project throw, `catch (System.Exception ex)`, `ex.Message`, and raw-detail concatenation.
- Ancestry verification against `main` SHA `17403bcbbffe45c4c691b7718fdc246aa0086709` confirmed the source fix is an ancestor and the focused preflight commit is current HEAD.
- Python preflight execution, GitHub Actions, build, and licensed BricsCAD V25/V26 runtime were not executed or claimed PASS.

## Completion condition

Completed: current `main` keeps the detached Door/Opening export flow and actionable blocked guidance without reflecting runtime exception messages, with focused source regression coverage and exact integration SHAs recorded.