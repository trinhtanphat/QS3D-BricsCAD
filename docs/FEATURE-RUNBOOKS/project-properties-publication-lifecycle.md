# Project Properties failed-publication lifecycle

Canonical carrier: Issue #5011 / Lane-Key `issue-5011`.

Runtime disposition: repository-safe source/static/V25 compile work is REMOTE_SAFE. Licensed BricsCAD V25 native-window qualification is LOCAL_ONLY and must use one exact pushed SHA or published descendant.

## Source contract

`QS3DPROJECTPROPERTIES` remains the dedicated host-global, read-only BLT3D Project Properties placeholder. It must not route to Project Tools, mutate ProjectState, or absorb Project Information ownership.

The launcher owns two explicit states:

- `_pending`: constructed candidate not yet proven published by BricsCAD;
- `_published`: loaded singleton eligible for activation/reuse.

A pending owner is drained before any replacement construction. A loaded published owner is reused. An unloaded published owner is terminally released before replacement. If close throws or terminal ownership release cannot be proven, replacement fails closed.

Publication order is fixed: construct -> matching `Closed` handlers -> `_pending = window` -> `ShowModelessWindow` -> prove `IsLoaded` -> prove exact pending owner -> clear `_pending` -> assign `_published`. Raw host `ex.Message` is never surfaced to editor or Palette status.

## Deterministic repository validation

Run:

```text
python scripts/preflight-project-properties-publication.py
python scripts/preflight-host-global-utility-window-publication.py
python scripts/preflight-blt3d-project-properties.py
python scripts/preflight-project-information-hosting.py
```

Shared CI must also pass auto-discovered feature guards, Core smoke tests and the locked-reference V25 plugin build for the exact candidate.

## LOCAL_ONLY licensed V25 matrix

- PP01 normal launch: exactly one Project Properties placeholder opens and remains loaded.
- PP02 repeated invocation: existing loaded owner activates; no duplicate is constructed.
- PP03 close/reopen: matching `Closed` releases ownership and exactly one replacement opens.
- PP04 host-show exception: failed publication retains authoritative pending ownership until terminal cleanup.
- PP05 non-loaded host return: retry cannot create a duplicate while the failed candidate is not terminally released.
- PP06 cleanup failure/recovery: close exception/refusal fails closed; after terminal close, one replacement may open.
- PP07 stale callback isolation: delayed callback from an older instance cannot clear a newer pending/published owner.
- PP08 multi-DWG/host-global behavior: the same placeholder singleton remains host-global without silently becoming bound to a stale document.
- PP09 separation/read-only: Project Properties remains the BLT3D placeholder, does not route into Project Tools/Project Information, and produces no project mutation.
- PP10 error redaction/cold cleanup: host exception sentinel is absent from editor/Palette; after final close no owned Project Properties window remains.

Record host build, exact SHA, scenario result and cleanup evidence. Hosted/static/build evidence must not be reported as licensed `LOCAL_PASS`.
