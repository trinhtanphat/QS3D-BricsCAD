# Grid command failure redaction qualification

Issue: #5198  
Lane-Key: `issue-5198`  
Runtime class: `LOCAL_ONLY` for licensed BricsCAD V25 execution.  
Remote/static source and CI evidence is never `LOCAL_PASS`.

## Source contract

The Grid command surface must preserve existing semantic/native behavior while preventing caught host/native exception details from becoming user-visible text.

- `QS3DGRID` keeps current selection validation, subtype constraints and semantic capture behavior.
- `QS3DGRIDINTERSECTIONHEALTH` remains read-only and must not create/bind a project.
- `QS3DGRIDINTERSECTIONS` and `QS3DGRIDINTERSECTIONSSEL` keep preview/selection before canonical mutation binding, then exact project freshness rejection before native marker refresh.
- `GridIntersectionMarkerService` ownership, replacement, XData, bounded planning and transaction semantics are unchanged.
- Operation failures use stable messages; Palette and Editor reporting are best-effort.
- After semantic capture has succeeded, Palette refresh/status failures must not turn the committed operation into a source failure and must not expose host exception details.

## Deterministic remote checks

Run the repository auto-discovered preflights, including:

- `scripts/preflight-grid-intersection-marker-lifecycle.py`
- `scripts/preflight-grid-command-failure-redaction.py`

Protected acceptance still requires fresh exact-head `preflight` + `core` SUCCESS.

## Licensed V25 matrix — GR01–GR12

Execute only against the exact pushed/merged candidate authorized for LOCAL qualification and record ProductVersion/plugin hashes before launch.

| Cell | Action | Required evidence |
| --- | --- | --- |
| GR01 | Start V25, NETLOAD exact plugin, open disposable DWG/project | Clean startup; exact artifact identity captured |
| GR02 | `QS3DGRID` with no selection | Stable no-selection message; no mutation |
| GR03 | Capture valid LINE Grid | Semantic Grid capture succeeds; no raw host/native exception text |
| GR04 | Capture valid ARC Grid under curved subtype | Semantic capture succeeds with subtype contract preserved |
| GR05 | Capture invalid/zero/nonmatching source | Fail-closed validation; no semantic mutation |
| GR06 | `QS3DGRIDINTERSECTIONHEALTH` without project | Read-only no-project message; no sidecar/project creation |
| GR07 | Health on valid project/markers | Stable health status; project ChangeVersion unchanged by health |
| GR08 | `QS3DGRIDINTERSECTIONS` all refresh | Pair-owned markers materialize; ownership/XData remains canonical |
| GR09 | `QS3DGRIDINTERSECTIONSSEL` selected refresh | Only mapped semantic Grid targets refresh |
| GR10 | Force a stale preview/project change before refresh completion | Command rejects stale state before native replacement |
| GR11 | Exercise a controlled UI-sync failure after successful Grid capture | Semantic capture remains committed; stable UI warning only; no exception detail |
| GR12 | QSAVE, close, fresh-process reopen, rerun health/refresh | Semantic/native ownership survives cold reopen; cleanup leaves no owned process/residue |

## Verdict rules

`LOCAL_PASS` requires all authorized cells to pass on one exact artifact identity with cleanup evidence. Any native/runtime defect is reported as `RUNTIME_FAIL` or `NO_RESULT` with sanitized evidence and must not be inferred from hosted CI. If a new source defect is found, open a bounded source carrier rather than editing production source from the local evidence lane.
