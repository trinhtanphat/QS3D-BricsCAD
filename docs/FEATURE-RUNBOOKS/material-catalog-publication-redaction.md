# Material Catalog pending-publication and redaction qualification

Carrier: issue-5104  
Lane-Key: `issue-5104`  
Remote-safe scope: source/guard/build qualification  
Licensed BricsCAD V25 scope: LOCAL_ONLY; no remote LOCAL_PASS claim.

## Source contract

`QS3DMATERIALS` reserves one exact pending Material Catalog owner before BricsCAD host show. Reentrant invocation for the same native database and managed Document wrapper reuses/fail-closes that pending owner rather than constructing a second window. A different-document invocation is blocked while publication is pending. Publication becomes visible in `_published` only after the host returns a loaded window and the exact pending owner is still authoritative. Close/show-failure cleanup clears only matching pending/published owners. User-facing command failures use a stable redacted message and do not expose `Exception.Message`.

## Remote qualification

Run from repository root:

```text
python scripts/preflight-material-catalog-open-project-lifecycle.py
python scripts/preflight-material-catalog-publication-redaction.py
python scripts/run-feature-preflights.py
```

Shared CI must additionally pass current protected `preflight` and `core`, including deterministic Core smoke and locked-reference BricsCAD V25 compile/build.

## LOCAL_ONLY V25 matrix

Use one licensed BricsCAD V25 process and record exact source SHA, BricsCAD build, DWG identity and result for every cell. Fetch/build the prepared candidate first; do not edit source locally.

| Cell | Scenario | Required observation |
| --- | --- | --- |
| MC01 | Open an existing QS3D project DWG; run `QS3DMATERIALS` once | One Material Catalog opens and remains usable. |
| MC02 | Invoke `QS3DMATERIALS` repeatedly while the same window is already loaded | Existing same-DWG window is activated; no duplicate appears. |
| MC03 | Trigger/reinvoke the command while modeless host-show publication is still pending | No second candidate becomes visible; pending owner remains authoritative. |
| MC04 | During pending publication, switch/invoke from another DWG where safely reproducible | Cross-DWG second instance is fail-closed; no ownership steal occurs. |
| MC05 | Close the loaded Material Catalog and reopen it | Exact owner is released and one fresh instance opens. |
| MC06 | Close the window during/around host publication where reproducible | Pending/published state is released only for that candidate; later reopen succeeds. |
| MC07 | Verify project-not-initialized admission | No project is implicitly created/cached; user sees a stable failure surface. |
| MC08 | Force a host-show failure using an approved local harness/fault-injection path | Candidate is best-effort closed, pending owner released, no duplicate/stale owner remains. |
| MC09 | Cause a launcher exception through an approved local harness | Palette/Editor message is stable/redacted and contains no native exception detail/path/stack text. |
| MC10 | Open two project DWGs sequentially, close the first catalog, then open from the second | Native database + managed wrapper affinity follows the active document correctly. |
| MC11 | Apply a material from the catalog after successful publication | Existing semantic selection/apply behavior remains functional. |
| MC12 | Reopen after MC08/MC09 recovery | One clean instance opens; no stale pending/published owner blocks normal use. |

Mark a cell `PASS` only from observed licensed V25 behavior. If a required fault cannot be induced safely, record `NOT_RUN` with the missing harness/input rather than inferring success.
