# Grid repeat authoring — licensed BricsCAD V25 qualification

Carrier: #5308

Runtime status: **LOCAL_ONLY / NO_RESULT** until the exact candidate is exercised in a licensed BricsCAD V25 host. Hosted source guards and V25 compilation are not runtime PASS evidence.

## Preconditions

Load the exact candidate plugin in a clean licensed V25 session. Record plugin/source SHA, DWG path, BricsCAD version, and command transcript/screenshots. Use two independent DWGs for isolation cases. Do not reuse stale plugin binaries.

## Matrix

| ID | Scenario | Required observation |
|---|---|---|
| GRP01 | `QS3DGRIDRECTREPEAT` before any successful rectangular authoring | Fail closed with missing-template status; no native/semantic mutation. |
| GRP02 | Create rectangular Grid, then repeat | Repeat asks only new key/origin/U direction and reuses committed U/V counts + spacing. |
| GRP03 | Cancel rectangular authoring before builder commit | Cancelled request does not become repeat template. |
| GRP04 | Cause rectangular builder failure, then repeat | Last previously successful template remains authoritative; failed request does not poison state. |
| GRP05 | `QS3DGRIDRADIALREPEAT` before successful radial authoring | Fail closed; no native/semantic mutation. |
| GRP06 | Create radial Grid, then repeat | Repeat asks only new key/center/first-ray direction and reuses committed ray/ring parameters. |
| GRP07 | Cancel/fail radial authoring | Cancelled/failed request does not replace the last successful template. |
| GRP08 | Two open DWGs | Template created in DWG-A is unavailable in DWG-B until B has its own successful authoring; switching back restores A's independent in-memory template. |
| GRP09 | Replacement semantic key | Repeating with an existing system key follows canonical builder replacement/fail-closed ownership rules; no duplicate semantic owner. |
| GRP10 | Undo/Redo after repeated materialization | Native/semantic behavior matches the canonical builder contract; repeat state itself is session authoring memory, not persisted project truth. |
| GRP11 | Close/reopen DWG / restart host | No claim that repeat memory persists. Reopened document begins without session template; no stale closed-document state leaks into another DWG. |
| GRP12 | UI/status failure injection if available | Native/semantic commit remains valid when post-commit palette/status synchronization fails; no raw exception text is disclosed. |

## Acceptance

A local agent may report `LOCAL_PASS` only with exact-SHA evidence for all applicable GRP01–GRP12 cases. Any mismatch remains `LOCAL_FAIL` with the exact command sequence and evidence attached to #5308. Remote CI must retain `LOCAL_ONLY / NO_RESULT` wording.
