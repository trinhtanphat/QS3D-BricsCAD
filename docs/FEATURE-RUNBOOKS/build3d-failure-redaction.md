# Build3D failure redaction qualification

Issue: #5224  
Lane-Key: `issue-5224`  
Runtime class: `LOCAL_ONLY` for licensed BricsCAD V25 native/UI execution.  
Remote/static source and CI evidence is never `LOCAL_PASS`.

## Source contract

`QS3DBUILD3D` keeps its existing read-only source preflight, single-category fail-closed dispatch, selected/upstream semantic regeneration, late implied-selection handoff, native builder dispatch and semantic rollback-versus-committed-ownership discrimination.

This carrier hardens only failure/UI boundaries:

- pre-commit failures whose generated ownership is unchanged still restore the semantic snapshot and propagate into the stable top-level failure report;
- if generated ownership changed, the command continues to preserve the committed CAD/semantic state rather than rolling project state backward, but reports only stable guidance instead of caught host detail;
- top-level command failure no longer exposes caught exception detail;
- after successful native build + `project.Touch`, Palette refresh, Regen, generated/source selection, status and Editor output fail independently;
- post-commit UI failure emits only a stable committed-state warning; no UI cell can convert a completed rebuild into a false rollback/failure claim.

Deterministic remote acceptance includes strengthened `scripts/preflight-build3d-canonical.py` and focused `scripts/preflight-build3d-failure-redaction.py`, followed by fresh exact-head protected `preflight` + `core`.

## Licensed V25 matrix — B301–B312

Run against one exact authorized plugin artifact; record ProductVersion/plugin SHA-256 and use a disposable project/DWG.

| Cell | Action | Required evidence |
| --- | --- | --- |
| B301 | Start V25, NETLOAD exact plugin, open disposable tracked project | Exact artifact identity and clean startup captured |
| B302 | Run `QS3DBUILD3D` with no valid selection | Stable guidance; no mutation and no raw exception detail |
| B303 | Build valid LINE wall source | Source preflight remains read-only; handoff happens only at dispatch; solid + semantic ownership commit |
| B304 | Build valid open-POLYLINE wall source separately | Correct canonical polyline builder dispatch and ownership |
| B305 | Select mixed/unsupported categories or untracked source | Fail closed before native build; no partial replacement |
| B306 | Force semantic regeneration failure before native dispatch | Semantic snapshot restored; generated ownership unchanged |
| B307 | Force native builder failure before generated ownership changes | Semantic rollback retained; stable top-level failure only |
| B308 | Force controlled post-commit builder/host failure after generated ownership changes | CAD/semantic committed ownership is preserved; stable committed-state guidance; no rollback |
| B309 | Force Palette refresh and Regen failures independently after success | Rebuild remains committed; later UI cells continue best-effort |
| B310 | Force generated/source selection, status and Editor-output failures independently after success | No UI failure escapes or exposes host detail; stable committed-state warning only |
| B311 | Verify generated solid selection after successful build and source fallback when applicable | Existing selection semantics preserved when UI cells succeed |
| B312 | QSAVE, close, fresh-process reopen and inspect generated ownership | Native/semantic ownership persists; cleanup leaves no owned BricsCAD process/residue |

## Verdict

`LOCAL_PASS` requires B301–B312 on one exact artifact identity with sanitized evidence and cleanup. Any licensed/native defect is `RUNTIME_FAIL` or `NO_RESULT`; hosted CI must never be promoted to runtime evidence.
