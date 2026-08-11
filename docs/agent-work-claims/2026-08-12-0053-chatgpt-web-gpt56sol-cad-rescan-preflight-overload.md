# Work claim — CAD rescan preflight overload recognition

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-cad-rescan-preflight-overload`
- Registered: `2026-08-12T00:53:00+07:00`
- Baseline main SHA: `8e1783d50cd2dd6f61bb0adad7fa260c12f0b908`
- Priority: owner-requested release unblock after Cloud V25 Preview Build & Release #25 false-negative preflight failure.

## Verified defect

Run #25 (`31519415965`) built commit `88223fa66f23aa4cc7b3cd83a87b221ae5119909` and failed `scripts/preflight.py` with `CAD rescan must replace stale source-derived metrics/metadata: StartsWith("CAD.")`. At that exact commit, `SemanticCaptureService.cs` already removes stale CAD metadata with `x.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)` and removes matching keys. The preflight only accepts the exact one-argument source substring `StartsWith("CAD.")`, so the stronger ordinal-ignore-case overload is rejected even though the required cleanup is present.

## Reserved scope

Make the CAD rescan preflight recognize the existing safe `StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)` implementation without weakening the requirements that stale `CAD.` metadata is removed and source-derived metrics are replaced/removed.

## Expected surfaces

- `scripts/preflight.py`
- this claim file for close-out

## Excluded scope

- `SemanticCaptureService.cs` behavior changes
- unrelated preflight gates
- version bumps, installer/package/signing/update manifest changes
- BricsCAD runtime behavior

## Validation plan

- Preflight CAD-rescan gate accepts the exact current safe source form `StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)`.
- Gate still requires `ReplaceSourceMetric` and `element.Properties.Remove(key)`.
- Gate must not be relaxed to a generic `CAD.` token that could pass without cleanup semantics.
- Confirm current source still contains the required cleanup after the patch.
- Fresh release workflow is dispatched only after the fix is reachable from current `main`.

## Coordination

Recent active claims inspected around current `main` concern signing EKU, quantity, geometry, recognition, updater and other lanes; no current claim was found reserving this exact `scripts/preflight.py` CAD-rescan false-negative guard.

## Completion condition

The false-negative CAD-rescan preflight is corrected on `main`, current semantic cleanup remains intact, a fresh release run reaches the corrected commit, and this claim is marked `COMPLETED`.