# Level Picker UI error-isolation licensed qualification

Issue: #5346  
Lane-Key: `issue-5346`  
Status: `LOCAL_ONLY / NO_RESULT` until executed in licensed BricsCAD V25 against one exact qualifying source/artifact identity.

Hosted/static checks and V25 compilation are source evidence only. They do not establish native UI `LOCAL_PASS`.

## Exact identity gate

Before execution record exact source SHA, ProductVersion, V25 plugin DLL/PDB hashes, BricsCAD version, disposable DWG hash, runner/probe hashes, clean process pre-state, and the canonical task/run identity. Use only sanitized repository-generated/disposable drawings. Do not publish raw host paths, stack traces, private drawings, handles, or exception payloads.

## Matrix

| Cell | Licensed-host acceptance |
|---|---|
| FLE01 | Open `QS3DLEVELS` on a clean project and confirm ordinary Floor create/update/activate/delete succeeds with normal stable status copy. |
| FLE02 | Invalid/non-finite elevation is refused without semantic mutation; visible error is stable Level Picker product text and contains no CLR/host exception detail. |
| FLE03 | Missing/stale selected Floor is refused before mutation with stable redacted status and unchanged project snapshot. |
| FLE04 | Change project identity after Refresh; save/activate/assignment fails closed, requires Refresh, and preserves exact pre-operation semantic state. |
| FLE05 | Change implied semantic selection between preview and apply; assignment fails closed with no partial Floor/Level mutation. |
| FLE06 | Inject a bounded ProjectFloorService/audit failure after rollback snapshot; prove rollback restores project state and UI publishes only stable operation failure copy. |
| FLE07 | Inject rollback failure in the approved disposable harness; prove no raw aggregate/inner exception text reaches StatusText, Palette status, or command line. |
| FLE08 | Inject post-commit Refresh/Palette failure after a successful Floor mutation; semantic commit remains true while UI reports only the stable `đã commit; đồng bộ UI chưa hoàn tất` warning. |
| FLE09 | Exercise bottom/top/clear vertical Level operations including an unqualified native-integration element; fail closed and redact host/API detail. |
| FLE10 | Reproduce managed Document-wrapper drift; stale picker closes under the existing wrapper-drift contract and reopening binds the live wrapper. |
| FLE11 | Two-DWG isolation: stale/inactive bound drawing cannot mutate the other drawing; neither UI surface leaks raw exception/path/runtime detail. |
| FLE12 | Save, close, fresh-process cold reopen and repeat a valid + refused operation; semantics remain coherent, statuses remain stable/redacted, cleanup leaves zero owned process/private-state residue. |

## Pass boundary

`LOCAL_PASS` requires all applicable FLE01–FLE12 cells on the same exact admitted artifact, plus exact cleanup evidence. A source/static/compile PASS, a subset of cells, or a prior artifact cannot be promoted to runtime acceptance.
