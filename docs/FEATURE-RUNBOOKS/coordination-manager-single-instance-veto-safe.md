# Coordination Manager single-instance / veto-safe ownership

## Purpose

Qualify issue #4699: repeatedly invoking `QS3DCOORDINATIONMANAGER` must never orphan a live modeless manager or publish a second document-bound review controller merely because `Window.Close()` returned after a `Closing` veto.

Hosted/source/build evidence does **not** prove licensed BricsCAD modeless behavior. Native/modeless cases below remain `LOCAL_ONLY` until executed against the exact candidate package/SHA in a matching BricsCAD V25 host.

## Source contract

`CoordinationManagerCommands` must retain one atomic published owner containing the manager window plus stable native database identity. It must not retain a managed `Document` wrapper across the modeless lifetime.

1. Same native database + live published manager: activate/reuse it and return even if BricsCAD supplied a different managed `Document` wrapper for that database.
2. Different native database + live published manager: request close, but retain static ownership while the request is in flight.
3. If any `Closing` subscriber vetoes, `Close()` may return while the same published owner remains. In that case fail closed and do not construct/show a second manager.
4. Only the instance-safe terminal `Closed` handler may normally release live static ownership.
5. A genuinely stale non-loaded reference may be repaired defensively before constructing a new candidate.
6. New candidate remains unpublished through construction + `CoordinationManagerReviewUi.Attach` + host show. Static publication occurs only after `ShowModelessWindow` succeeds.
7. Failed candidate initialization/show is closed best-effort without disturbing any independently published owner.
8. Native database identity must be non-zero at publication and subsequent same-document matching must fail closed if the candidate wrapper/database cannot be safely inspected.

## Deterministic repository checks

Run from repository root:

```powershell
python scripts/preflight-coordination-manager-review-attachment-rollback.py
python scripts/preflight-coordination-manager-single-instance-veto-safe.py
```

Both must PASS on the exact candidate SHA. Shared CI remains authoritative for the full changed-path-selected source/build matrix.

## LOCAL_ONLY BricsCAD matrix

| Case | Setup | Action | Required observation |
|---|---|---|---|
| Same-document repeat | Open manager for DWG A | Run `QS3DCOORDINATIONMANAGER` repeatedly | Existing window is activated/reused; exactly one manager/controller remains |
| Managed-wrapper drift | Open manager for DWG A, then exercise a host flow that replaces/re-resolves the managed `Document` wrapper while retaining the same native database | Re-run `QS3DCOORDINATIONMANAGER` | Existing manager is reused by native DB identity; it is not mistaken for a different document |
| Same-document transient debt | Highlight/isolate/focus in manager A | Re-run command | Existing manager remains owner; command does not trigger cleanup solely to replace it |
| External close veto | Add/trigger another supported `Closing` veto on manager A | Switch to DWG B and run command | Manager A stays published/live; manager B is not created |
| QS3D cleanup veto | Arrange supported transient cleanup failure in manager A | Switch to DWG B and run command | Close is vetoed by review cleanup ownership; no second manager is published |
| Terminal close then switch | Close manager A successfully | Activate DWG B and run command | A releases ownership on `Closed`; one manager for B opens normally |
| Attach failure | Use deterministic/source-supported candidate attach-failure fixture | Run command with no live published manager | Failed candidate closes; static published ownership remains null |
| Host show failure | Use supported show-failure fixture if available | Run command | Candidate is not statically published before host show succeeds |
| Document destroy | Destroy DWG owning live manager through supported host flow | Observe | Review destroyed-document abandon semantics run; terminal ownership does not leak into another document |

Capture exact SHA, BricsCAD version, package identity, native database identity where safely observable, active document identity, window count, and sanitized PASS/FAIL observations. Do not report `LOCAL_PASS` from source/static CI alone.
