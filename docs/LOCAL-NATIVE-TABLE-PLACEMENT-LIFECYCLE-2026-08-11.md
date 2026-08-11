# LOCAL_ONLY native Table placement lifecycle qualification

Updated: 2026-08-11 (UTC+7)

This is a supporting execution runbook for the existing `LOCAL-006 — native documentation objects` item in `docs/LOCAL-AGENT-INBOX.md`. It is **not** a second live queue. `docs/LOCAL-AGENT-INBOX.md` remains authoritative for priority/status, and remote/non-local agents must treat this runtime qualification as `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.

## Source contract under qualification

The six native Table Build commands are:

- `QS3DBQTABLE`;
- `QS3DBBSTABLE`;
- `QS3DELEMENTTABLE`;
- `QS3DMATERIALTABLE`;
- `QS3DFINISHTABLE`;
- `QS3DDOOROPENINGTABLE`.

Their common placement lifecycle must be:

1. prove an existing project/sidecar through `ProjectContextCoordinator.TryGetReadOnly` without binding a mutable canonical project;
2. capture the observed `ProjectId`;
3. request the placement point;
4. on cancel/non-OK, return before canonical project binding and before native/semantic mutation;
5. after accepted placement, bind through `ExistingProjectMutationContext.Require`;
6. verify the rebound canonical `ProjectId` still matches the read-only observation;
7. only then build/update the owned native Table.

Static source enforcement is in `scripts/preflight-native-table-placement-project-lifecycle.py`.

## Exact V25 scenario matrix

Run on a clean checkout of the exact candidate SHA with licensed BricsCAD V25 x64. Use disposable drawings/sidecars and sanitized evidence only.

1. **Cold-cache placement cancel, all six families** — open a drawing with a valid `.qsdb`, clear/forget the live project cache, invoke each Build command independently, then cancel `GetPoint`. The read-only probe may inspect the sidecar, but the cancel path must not bind/cache a mutable canonical project, create or replace Table objects, change generated ownership metadata, advance semantic `ChangeVersion`/audit, or persist a sidecar change.
2. **Absent-sidecar refusal, all six families** — on a drawing without a QS3D project/sidecar, invoke each Build command. It must refuse through the read-only existence guard, create/cache no project, create no sidecar, and create no Table. No accidental creation-capable `GetOrCreate` path is allowed.
3. **Successful cold-cache placement** — with a valid existing `.qsdb`, forget the cache, accept a placement point, and verify canonical binding resolves the same `ProjectId` observed before placement. Verify exactly one owned Table lifecycle is applied, metadata lands on the canonical project, and save/reopen retains ownership/position/content.
4. **Project replacement during placement** — after the read-only project probe but before accepting the placement point, replace/remove the sidecar or otherwise cause canonical project identity to change. Accept placement. The post-placement same-`ProjectId` guard must fail closed before Table/native/semantic mutation and must not leave a replacement project cached for the refused operation.
5. **Refresh/remove continuity** — after a successful Build, forget/reload the cache and exercise the matching Refresh and Remove commands. They must use the canonical existing-project mutation boundary, replace/remove only the complete owned Table set, preserve foreign/user Tables, and persist coherent metadata through save/reopen.
6. **Health remains read-only** — with warm cache, cold cache, absent sidecar and corrupt/mismatched sidecar, run each Table Health command. Health must remain non-creating/non-mutating and must never convert detached inspection state into ownership mutation.
7. **Multi-DWG isolation** — keep a second drawing open with a different project. Exercise successful, cancelled and stale/replaced-project paths in the active drawing and prove the other drawing's cache/project/audit/Table objects are unchanged.

## Evidence required

Record sanitized evidence tied to the exact tested SHA:

- exact QS3D SHA, Windows build, BricsCAD V25 build;
- command/family and scenario result;
- before/after live-cache presence and canonical ProjectId continuity/refusal;
- before/after Table object counts and generated ownership metadata summary;
- before/after semantic `ChangeVersion`, audit count/summary and sidecar persistence state;
- placement-cancel proof of no canonical bind/cache and no Table/semantic mutation;
- absent-sidecar proof of no project/cache/sidecar/Table creation;
- stale/replaced-project proof of refusal before native/semantic mutation;
- successful cold-cache save/reopen ownership and stored-position result;
- foreign/user-object protection and multi-DWG no-cross-mutation result;
- sanitized failure/output notes where useful.

Do not commit private/customer DWGs, proprietary BricsCAD DLLs, raw private paths, raw Handle lists, credentials, or unsanitized runtime captures.

## Status boundary

Source/static review may establish `REMOTE_DONE` for the placement lifecycle contract but cannot establish `LOCAL_PASS`. Until a compatible local agent runs this matrix on an exact current candidate SHA and updates the existing `LOCAL-006` evidence/status, the runtime qualification remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
