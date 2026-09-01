# Source Edit failure redaction qualification

Issue: #5220  
Lane-Key: `issue-5220`  
Runtime class: `LOCAL_ONLY` for licensed BricsCAD V25 command/UI execution.  
Remote/static source and CI evidence is never `LOCAL_PASS`.

## Source contract

`QS3DEDITSOURCE` keeps the existing guarded MOVE/ROTATE workflow and reversible native-transform contract. The command must continue to validate authoritative ownership, capture prompt context, revalidate project/source freshness, commit the forward transform, reconcile through `SourceReconcileService`, and reverse the transform if reconcile fails. If reversal also fails, the existing fail-closed UNDO/repair guidance remains the source truth.

This carrier changes only user-visible failure handling and post-success UI isolation:

- top-level command failure uses stable text instead of caught exception detail;
- successful edit + reconcile remains completed before `FinalizeSuccess` runs;
- Palette refresh, Editor Regen, Palette status and Editor output fail independently;
- post-success UI failures emit only a stable warning and never expose host/native exception detail;
- `ReportFailure`/Editor reporting remains best-effort and non-escaping;
- STRETCH/grip/jig remain outside this carrier.

Deterministic remote acceptance includes `scripts/preflight-source-edit.py` and `scripts/preflight-source-edit-failure-redaction.py`, followed by fresh exact-head protected `preflight` + `core`.

## Licensed V25 matrix — SE01–SE12

Execute only against one exact authorized plugin artifact and record ProductVersion/plugin SHA-256 before launch.

| Cell | Action | Required evidence |
| --- | --- | --- |
| SE01 | Start V25, NETLOAD exact plugin, open disposable tracked project | Exact artifact identity and clean startup captured |
| SE02 | Run `QS3DEDITSOURCE` with no selection/cancel selection | No mutation; no raw exception detail |
| SE03 | MOVE one valid authoritative tracked source | Native source transform + reconcile succeeds; generated dependents follow existing invalidation contract |
| SE04 | ROTATE one valid authoritative tracked source under non-default UCS | Rotation honors captured UCS and reconcile succeeds |
| SE05 | Select QS3D-generated output instead of authoritative source | Fail closed before mutation |
| SE06 | Select unknown/untracked or ambiguously owned source | Fail closed before mutation with stable reporting |
| SE07 | Change project/source freshness after prompt and before commit | Freshness rejection occurs before forward native mutation |
| SE08 | Force reconcile failure after forward transform | Forward transform is reversed and stable failure text is reported |
| SE09 | Force reconcile failure plus controlled reverse-transform failure | Fail-closed UNDO/repair guidance remains; no host exception detail is exposed |
| SE10 | Force Palette refresh, Regen, status and Editor output failures independently after successful edit+reconcile | Completed edit/reconcile remains committed; later UI cells continue best-effort; stable warning only |
| SE11 | Verify STRETCH/grip/jig are not silently approximated through MOVE/ROTATE code | No unsupported topology-edit behavior appears |
| SE12 | QSAVE, close, fresh-process reopen, inspect/reconcile affected source | Semantic/native ownership persists; cleanup leaves no owned BricsCAD process/residue |

## Verdict

`LOCAL_PASS` requires SE01–SE12 on one exact artifact identity with sanitized evidence and cleanup. Any licensed/native defect is `RUNTIME_FAIL` or `NO_RESULT`; hosted CI must never be promoted to runtime evidence.
