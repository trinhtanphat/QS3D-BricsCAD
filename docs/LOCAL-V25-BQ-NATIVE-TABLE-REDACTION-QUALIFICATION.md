# LOCAL V25 BQ Native Table Redaction Qualification

Status: `LOCAL_ONLY / SOURCE_READY / NO_RESULT` until executed in licensed BricsCAD V25 on the exact candidate.

Use the exact pushed SHA and an exclusive host. Do not infer `LOCAL_PASS` from remote CI or compile success.

Qualification matrix:

- BT01 build success: existing project, supported UCS, pick point, native Table commits and success message remains intact.
- BT02 build host failure: inject a native/host exception before durable completion; user surface must show only the stable `QS3DBQTABLE` failure text and no exception detail.
- BT03 build freshness: mutate project identity/change version while point acquisition is outstanding; command must fail closed without creating an unintended Table.
- BT04 refresh success: stored WCS position is reused and semantic regeneration precedes native Table build.
- BT05 refresh failure: injected builder/host failure is redacted and does not escape to BricsCAD.
- BT06 remove success/failure: owned artifact removal remains scoped to the project; injected failure is redacted.
- BT07 health success: read-only inspection reports stable health issues and does not create project state.
- BT08 health failure: injected inspection failure is redacted and non-escaping.
- BT09 post-commit Regen failure: after confirmed durable Table mutation, fail `Editor.Regen`; warning must state the Table already committed and expose no raw exception text.
- BT10 palette refresh/status failure: after durable commit, inject palette failure; command must not roll back committed CAD/project state or escape the exception.
- BT11 editor warning failure: inject `WriteMessage` failure on the warning path; no exception may escape.
- BT12 cold reopen: save/reopen exact drawing and verify owned Table metadata/position/health remain consistent; record sanitized evidence and host cleanup.

For every cell capture exact package/source SHA, BricsCAD V25 version, outcome, sanitized command transcript, and cleanup evidence. Runtime PASS requires all applicable cells to pass on the exact artifact.