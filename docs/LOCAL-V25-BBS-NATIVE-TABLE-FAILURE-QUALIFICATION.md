# LOCAL V25 BBS Native Table Failure Qualification

Status: `LOCAL_ONLY / SOURCE_READY / NO_RESULT` until executed in licensed BricsCAD V25 on the exact candidate artifact.

Hosted source guards, Core smoke tests and locked-reference compilation are not licensed runtime evidence. Record the exact source/package SHA, BricsCAD V25 build, active DWG and sanitized evidence for every cell.

Qualification matrix:

- BT01 Build success: `QS3DBBSTABLE` in ModelSpace with supported UCS creates/updates the owned native BBS Table and reports the durable handle/regeneration result.
- BT02 Build prompt freshness: mutate/switch the QS3D project while the insertion-point prompt is outstanding; the command must fail closed without building from stale project identity/version.
- BT03 Build host failure redaction: inject a native/project/build exception containing a distinctive secret/path token; neither Palette nor Editor output may expose that token, stack text or raw exception detail.
- BT04 Refresh success: `QS3DBBSTABLEREFRESH` uses stored WCS position, regenerates semantic state first, updates the owned Table and reports success.
- BT05 Refresh host failure redaction: inject failure before native refresh commit; report only the stable operation-specific message and leave no false success claim.
- BT06 Remove success/failure: successful `QS3DBBSTABLEREMOVE` clears owned artifact/metadata; injected remove failure is redacted and must not claim success.
- BT07 Health read-only: `QS3DBBSTABLEHEALTH` reports current BBS/native issues without creating a project, regenerating semantic state, building or removing CAD artifacts.
- BT08 Health failure redaction: inject an Inspect/provider exception with distinctive native detail; user surfaces must receive only stable redacted command failure text.
- BT09 Post-commit Regen failure: after a successful Build/Refresh/Remove durable mutation, force `Editor.Regen` failure; committed CAD/project state must remain, no exception escapes, and a stable post-commit UI warning is attempted.
- BT10 Palette isolation: independently fail `RefreshProject` and `SetStatus`; Editor success/warning reporting must still be attempted and raw exception detail must remain hidden.
- BT11 Editor isolation: fail `WriteMessage` after durable mutation; Palette status/warning attempts must remain non-escaping and committed native state must not be rolled back or repeated.
- BT12 Cold lifecycle: save/close/reopen the DWG, rerun Health/Refresh/Remove/Build, and confirm owned BBS Table metadata, stored position, project freshness and redacted failure behavior remain consistent without duplicate ownership.

Runtime PASS requires all applicable BT01-BT12 cells on the exact artifact. Until then retain `LOCAL_ONLY / SOURCE_READY / NO_RESULT`.
