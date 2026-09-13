# C03 Start Center status-toggle generation affinity

## Scope

This runbook covers the live embedded BricsCAD V25 Start Center palette status toggles that mutate document-scoped system variables (`ORTHOMODE` and `OSMODE`). Application-global UI controls such as `COLORTHEME` and `LINEARCONTRAST` are intentionally outside this carrier.

## Defect contract

A modeless click can enter while one managed `Document`/native database generation is active, then BricsCAD can pump host messages before the native write completes. A later MDI activation or same-wrapper native database replacement must not allow the stale click to mutate the newer drawing.

The implementation therefore captures the active managed `Document` plus `Database.UnmanagedObject` identity, validates that generation before the native read, computes the next value without CAD side effects, validates the same generation again immediately before `Application.SetSystemVariable`, and suppresses the stale mutation if affinity changed.

No compensating rollback is attempted after a successful native `SetSystemVariable`: once the native commit succeeds, a display refresh failure must not be reported as a failed mutation.

## Remote-safe verification

Run the focused source guard:

```text
python scripts/preflight-v25-startcenter-status-toggle-generation.py
```

Then require the repository aggregate feature guards, deterministic smoke tests, admitted/trusted BricsCAD V25 reference validation, and the locked-reference V25 plugin compile on the exact candidate SHA.

## Local-only verification

Licensed BricsCAD validation remains `LOCAL_ONLY` and must not be inferred from remote CI. Exercise at minimum:

1. Open DWG A with ORTHOMODE/OSMODE in known states and show the embedded Start Center palette.
2. Invoke the toggle while forcing an MDI switch to DWG B during the interaction boundary; verify DWG B is not mutated by DWG A's stale click.
3. Repeat with a same managed `Document` wrapper whose native database generation is replaced/reloaded; verify the stale click is suppressed.
4. Verify normal same-generation clicks still toggle ORTHOMODE and the OSMODE suppression bit while preserving configured snap bits.
5. Verify post-commit palette refresh cannot convert a successful native write into a false operation failure.

Do not record `LOCAL_PASS` without a real licensed BricsCAD run.
