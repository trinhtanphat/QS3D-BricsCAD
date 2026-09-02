# MCP current save, deterministic layout, and foreground-policy status — V25/V26 qualification

Status: SOURCE_READY / LOCAL_ONLY_RUNTIME / NO_RESULT

Original issue: #5283  
Save-confirmation follow-up: #5296

This runbook defines the QS3D MCP save/layout/foreground-policy contract. Drawing-project deliverables are outside this source lane and must be qualified separately.

## Source contract

- `cad_save` is a current-document save. It must never call `SaveAs` against the active drawing's own path.
- Current-document save performs exactly one native `Database.Save()` attempt. There is no blind save retry.
- BricsCAD `DBMOD` is treated as a bitmask for completion. Persistent drawing state is mask `1 | 4 | 32` (object database, database variables, fields); those bits must clear before MCP reports save completion.
- Residual window/view state bits `8 | 16` may remain after the native save and do not by themselves make the save fail. The observed post-save value is returned/logged as bounded `dbmodAfterSave` diagnostics.
- If any persistent-content bit remains dirty through the bounded wait, save confirmation fails closed and reports the bounded numeric `DBMOD` state. It does not retry the native save.
- `cad_save_as` keeps path-transition semantics and remains the only direct route that calls `SaveAs`; it uses the same persistent-content completion check after the path transition is verified.
- Synchronous `QSAVE` uses the same `Save()` path as `cad_save`, so it inherits the same one-attempt and persistent-DBMOD confirmation contract.
- Bounded `LAYOUT` / `-LAYOUT` actions `NEW`, `SET`, and `DELETE` use `LayoutManager` directly inside CAD context and return `completed=true` only after the native mutation has completed.
- Unsupported layout prompt grammars remain outside the direct-completion claim; they are not reported as synchronously complete by this route.
- Foreground Control remains fail-closed: process start is `background_only`. Local desktop consent and enabled foreground policy are separate gates and both must be enabled before global input is available.
- V26 links V25 source, so the same source patch is compiled into both adapters; runtime qualification is still required independently on licensed V25 and V26.

## LOCAL_ONLY matrix

Bind every executed row to one exact pushed SHA, adapter/Core ProductVersion and SHA-256, licensed BricsCAD identity, and a disposable/sanitized DWG.

| ID | Scenario | Required result |
|---|---|---|
| S01 | Rooted modified DWG + `cad_save` | Same active path; exactly one current-document Save attempt; no SaveAs-current-path; `(DBMOD & 37) == 0`; `saved=true`, `completed=true`; return `dbmodAfterSave`. |
| S02 | Save succeeds while only DBMOD window/view bits 8 and/or 16 remain | MCP accepts completion, returns the residual `dbmodAfterSave`, and performs no retry. |
| S03 | DBMOD object/database-variable/field bit 1, 4, or 32 remains through the bounded wait | Fail closed; include bounded DBMOD diagnostic; perform no second native save. |
| S04 | Existing rooted DWG + `cad_save_as` to a distinct writable target | Path transitions to the requested target; overwrite contract unchanged; persistent DBMOD mask clears; return `dbmodAfterSave`. |
| S05 | Pathless/unsaved drawing + `cad_save` | Fail closed with guidance to use `cad_save_as`; no retry and no false success. |
| S06 | Active BricsCAD command during save | Fail closed until idle; no automatic retry. |
| S07 | Synchronous command-sequence `QSAVE` | Same completion behavior as `cad_save`, including acceptance of residual 8/16 and rejection of persistent dirty bits. |
| S08 | Save, close, then reopen the disposable DWG | Saved persistent drawing content is present after reopen; no `eCantOpenFile` or cross-session collision under the coordinated writer workflow. |
| L01 | `-LAYOUT` `NEW` with a unique layout name | Layout exists before response; response has `completed=true` and `route=LayoutManager-direct`. |
| L02 | `-LAYOUT` `SET` for an existing layout | Requested layout is current before response. |
| L03 | `-LAYOUT` `DELETE` for a non-Model layout | Layout is absent before response; if it was current, current layout safely becomes Model first. |
| L04 | Attempt to delete `Model` | Fail closed; Model remains present. |
| L05 | Unsupported layout prompt grammar such as rename/copy/template | Direct route does not claim synchronous completion. |
| P01 | Cold BricsCAD/process start | `mode=background_only`, `processStartDefault=background_only`, `requiresLocalReenableAfterRestart=true`. |
| P02 | Local consent ON while foreground policy OFF | Status shows consent separately, `policyEnabled=false`, `available=false`; no global input. |
| P03 | Local user explicitly enables Foreground Control | Local consent + policy are both enabled and only then may global input become available. |

## Evidence rules

Hosted/static CI may establish source guards, locked-reference compilation, and repository policy only. It must not be reported as `LOCAL_PASS` or as proof that `eCantOpenFile` is impossible in every filesystem/runtime condition.

For each local row record only sanitized evidence: exact Git SHA, BricsCAD version, adapter/Core ProductVersion and hashes, route/result flags, bounded `DBMOD`/layout observations, and cleanup state. Do not publish private paths, DWG content, secrets, stack traces, or raw host exception detail.
