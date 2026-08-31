# Right Panel CAD failure-surface redaction

Canonical carrier: Issue #5022 / Lane-Key `issue-5022`.

Runtime disposition: source/static checks and protected V25 compilation are REMOTE_SAFE. Licensed BricsCAD V25 panel/native qualification remains LOCAL_ONLY; remote CI or build success is not `LOCAL_PASS`.

## Product contract

Right Panel is a persistent host UI that reads and mutates active-DWG layer/Xref/CAD state. User-visible panel status must not echo caught host/native exception text. Each failure family therefore uses a stable operation-specific message while preserving the underlying safety behavior:

- refresh failure does not fabricate drawing/layer counts;
- drawing/Xref selection failure does not report successful CAD selection;
- layer visibility/lock failure keeps the primary failure status and performs only best-effort panel refresh;
- Xref reload/move/detach failure does not report the mutation as successful;
- command dispatch failure returns `false` and never reports the command as sent;
- Xref mutation success followed by panel refresh failure remains a success-with-redacted-warning distinction.

The redaction change must not weaken active-document dispatch, implied-selection cleanup, SelectedXref main-DWG rejection, Xref current-space scoping, layer refresh ordering, or command normalization.

## Deterministic repository validation

Run:

```text
python scripts/preflight-right-panel-error-redaction.py
python scripts/preflight-right-panel-drawing-selection.py
python scripts/preflight-xref-instance-layer-lock.py
```

Shared CI must also pass all auto-discovered feature guards, deterministic Core smoke tests, locked BricsCAD V25 reference validation, and the V25 plugin build for the exact candidate SHA.

## LOCAL_ONLY licensed V25 matrix

- RP01 normal refresh: load panel with an active DWG and verify drawing/layer counts and search remain usable.
- RP02 no-document transition: close the last DWG and verify stale drawing/layer state is cleared.
- RP03 refresh exception sentinel: induce a catalog/read failure containing a unique sentinel; panel shows the stable refresh failure and never the sentinel.
- RP04 implied-selection exception sentinel: induce clear/select failure; UI shows stable selection failure, never raw host detail, and does not claim success.
- RP05 layer visibility exception sentinel: fail checkbox/bulk show-hide; panel keeps stable failure and best-effort refresh cannot overwrite it with raw text.
- RP06 layer lock exception sentinel: fail bulk lock/unlock; panel keeps stable failure and best-effort layer/drawing refresh cannot leak the sentinel.
- RP07 Xref select/move exception sentinel: fail instance selection/preparation; panel does not claim move readiness and does not expose the sentinel.
- RP08 Xref reload/detach exception sentinel: fail each mutation independently; stable operation-specific failure is shown and success refresh text is absent.
- RP09 post-Xref refresh exception sentinel: complete a mutation but fail subsequent panel refresh; success remains distinguishable with the stable redacted refresh warning.
- RP10 SendStringToExecute exception sentinel: fail `_XATTACH`, `_ZOOM _W`, or `_MOVE` dispatch; `TrySend` returns false behaviorally and panel never reports the command as sent or exposes the sentinel.
- RP11 document switch: switch active DWGs between panel operations; each action targets the current active document and stale implied Xref selection is cleared as designed.
- RP12 cleanup/reopen: close/reopen panel/DWG/process as appropriate and verify no stale failure status is treated as successful CAD state.

Record exact product SHA/artifact identity, BricsCAD build, scenario verdict, unique sentinel used, and cleanup evidence. Only a licensed bounded host run may be reported as `LOCAL_PASS`.
