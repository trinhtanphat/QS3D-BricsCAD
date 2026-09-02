# MCP current save, deterministic layout, and foreground-policy status — V25/V26 qualification

Status: SOURCE_READY / LOCAL_ONLY_RUNTIME / NO_RESULT

Issue: #5283
Lane-Key: `issue-5283`

This lane fixes source-level causes that can block the A00 `LAYOUT + MASTER SAVED` gate without pretending that hosted CI is licensed BricsCAD runtime evidence.

## Source contract

- `cad_save` is a current-document save. It must never call `SaveAs` against the active drawing's own path.
- Current-document save performs exactly one native `Database.Save()` attempt, then reports completion only after `DBMOD` settles to zero. There is no blind save retry.
- `cad_save_as` keeps path-transition semantics and remains the only direct route that calls `SaveAs`.
- Bounded `LAYOUT` / `-LAYOUT` actions `NEW`, `SET`, and `DELETE` use `LayoutManager` directly inside CAD context and return `completed=true` only after the native mutation has completed.
- Unsupported layout prompt grammars remain outside the direct-completion claim; they are not reported as synchronously complete by this route.
- Foreground Control remains fail-closed: process start is `background_only`. Local desktop consent and enabled foreground policy are separate gates and both must be enabled before global input is available.
- V26 links V25 source, so the same source patch is compiled into both adapters; runtime qualification is still required independently on licensed V25 and V26.

## LOCAL_ONLY matrix

Bind every executed row to one exact pushed SHA, adapter/Core ProductVersion and SHA-256, licensed BricsCAD identity, and a disposable/sanitized DWG.

| ID | Scenario | Required result |
|---|---|---|
| S01 | Rooted modified DWG + `cad_save` | Same active path; one current-document Save attempt; no SaveAs-current-path; `DBMOD=0`; `saved=true`, `completed=true`. |
| S02 | Existing rooted DWG + `cad_save_as` to a distinct writable target | Path transitions to the requested target; overwrite contract unchanged; `DBMOD=0`. |
| S03 | Pathless/unsaved drawing + `cad_save` | Fail closed with guidance to use `cad_save_as`; no retry and no false success. |
| S04 | Active BricsCAD command during save | Fail closed until idle; no automatic retry. |
| L01 | `-LAYOUT` `NEW` with a unique A00 layout name | Layout exists before response; response has `completed=true` and `route=LayoutManager-direct`. |
| L02 | `-LAYOUT` `SET` for an existing A00 layout | Requested layout is current before response. |
| L03 | `-LAYOUT` `DELETE` for a non-Model layout | Layout is absent before response; if it was current, current layout safely becomes Model first. |
| L04 | Attempt to delete `Model` | Fail closed; Model remains present. |
| L05 | Unsupported layout prompt grammar such as rename/copy/template | Direct route does not claim synchronous completion. |
| P01 | Cold BricsCAD/process start | `mode=background_only`, `processStartDefault=background_only`, `requiresLocalReenableAfterRestart=true`. |
| P02 | Local consent ON while foreground policy OFF | Status shows consent separately, `policyEnabled=false`, `available=false`; no global input. |
| P03 | Local user explicitly enables Foreground Control | Local consent + policy are both enabled and only then may global input become available. |
| A00 | Create/set required A00 layout(s), perform master save, cold reopen | Required layouts persist; intended master DWG reopens cleanly; only this licensed-runtime evidence may advance the A00 layout/save gate. |

## Evidence rules

Hosted/static CI may establish source guards, locked-reference compilation, and repository policy only. It must not be reported as `LOCAL_PASS`, as proof that `eCantOpenFile` is impossible in every filesystem condition, or as proof that A00 is `COMPLETE`.

For each local row record only sanitized evidence: exact Git SHA, BricsCAD version, adapter/Core ProductVersion and hashes, route/result flags, bounded `DBMOD`/layout observations, and cleanup state. Do not publish private paths, DWG content, secrets, stack traces, or raw host exception detail.
