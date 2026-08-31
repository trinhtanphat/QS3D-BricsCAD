# Template import/export failure-surface redaction

Canonical carrier: Issue #5035 / Lane-Key `issue-5035`.

Runtime disposition: source/static checks and protected V25 compilation are REMOTE_SAFE. Licensed BricsCAD V25 dialog/palette/native qualification remains LOCAL_ONLY; remote CI or build success is not `LOCAL_PASS`.

## Product contract

`QS3DTEMPLATEEXPORT` and `QS3DTEMPLATEIMPORT` may touch file dialogs, project state, regeneration, palette refresh and editor output. User-visible failure surfaces must not echo caught host/native exception text.

The redaction change must preserve these safety semantics:

- export/import never creates a missing QS3D project implicitly;
- import re-checks the active DWG after file selection/confirmation and rejects document drift before mutation;
- import re-checks project identity/change-version and rejects stale project state;
- failed apply/regeneration restores the captured project snapshot before reporting failure;
- rollback refresh remains best-effort and cannot replace the primary failure;
- successful import/export remains distinguishable from a post-success UI refresh/write warning;
- import never auto-saves `.qsdb`.

## Deterministic repository validation

Run:

```text
python scripts/preflight-template-error-redaction.py
```

Shared CI must also pass all auto-discovered feature guards, deterministic Core smoke tests, locked BricsCAD V25 reference validation and the V25 plugin build for the exact candidate SHA.

## LOCAL_ONLY licensed V25 matrix

- TP01 export happy path: export a valid current project and confirm the template file is written and success status/editor message appears.
- TP02 export missing-project boundary: active DWG without QS3D project is rejected without creating one.
- TP03 export UI exception sentinel: induce palette/editor failure containing a unique sentinel after file write; output may show the stable post-export warning but never the sentinel.
- TP04 import happy path: apply a valid template to an existing project, regenerate and confirm the result remains unsaved until explicit project save.
- TP05 import document-drift boundary: switch active DWG while chooser/confirmation is open; mutation is rejected before project write.
- TP06 import project-version drift: mutate the same project while chooser/confirmation is open; stale template apply is rejected.
- TP07 import apply/regeneration failure: induce a unique failure sentinel and verify project snapshot rollback occurs before the stable command failure is surfaced.
- TP08 rollback refresh failure: induce a second unique sentinel during post-rollback palette refresh; neither primary nor secondary raw host detail is exposed and the rolled-back project remains authoritative.
- TP09 import post-success UI warning: complete apply/regeneration but fail palette refresh/editor update; imported data remains applied while the stable UI warning is distinguishable from import failure.
- TP10 top-level command exception sentinel: fail file load/save or another guarded host operation; editor/palette show the stable operation failure and never raw exception text.
- TP11 document switching/reopen: repeat export/import across two DWGs and verify each command remains bound to its captured/current document rules.
- TP12 cleanup: close/reopen affected DWGs/process and confirm no stale warning is interpreted as success state.

Record exact product SHA/artifact identity, BricsCAD build, scenario verdict, unique sentinel(s) used and cleanup evidence. Only a licensed bounded host run may be reported as `LOCAL_PASS`.
