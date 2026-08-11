# Work claim — CAD rescan preflight overload recognition

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-cad-rescan-preflight-overload`
- Registered: `2026-08-12T00:53:00+07:00`
- Baseline main SHA: `8e1783d50cd2dd6f61bb0adad7fa260c12f0b908`
- Priority: owner-requested release unblock after Cloud V25 Preview Build & Release #25 false-negative preflight failure.

## Verified defect

Run #25 (`31519415965`) built commit `88223fa66f23aa4cc7b3cd83a87b221ae5119909` and failed `scripts/preflight.py` with `CAD rescan must replace stale source-derived metrics/metadata: StartsWith("CAD.")`. At that exact commit, `SemanticCaptureService.cs` already removes stale CAD metadata with `x.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)` and removes matching keys. The preflight only accepted the exact one-argument source substring `StartsWith("CAD.")`, so the stronger ordinal-ignore-case overload was rejected even though the required cleanup was present.

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

## Validation completed

- Claim registration commit: `a37bef51f8757dee3d25bf06c30e5eec04d65c9c`.
- Source fix commit: `697249f7e248263b0fb7fed7abed3ef7918c6257`.
- Diff inspection confirms the source fix changes only the CAD-rescan guard in `scripts/preflight.py`.
- The guard still requires both `ReplaceSourceMetric` and `element.Properties.Remove(key)`.
- The prefix gate now requires the exact safe source form `StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)` rather than accepting a generic `CAD.` token.
- Current `SemanticCaptureService.cs` was re-read after registration and still contains the exact ordinal-ignore-case stale-key removal plus source-metric replacement calls.
- No GitHub Actions run was dispatched as part of this source fix. `CI_POLICY.md` explicitly states that pasted logs and `continue all` do not authorize CI/release dispatch; a fresh release remains an owner-controlled follow-up action.

## Coordination

Recent active claims inspected around current `main` concern signing EKU, quantity, geometry, recognition, updater and other lanes; no current claim was found reserving this exact `scripts/preflight.py` CAD-rescan false-negative guard.

## Completion

The false-negative CAD-rescan preflight guard is corrected on `main` without changing semantic capture behavior or weakening cleanup requirements. This source lane is released. Fresh GitHub Release validation/publication remains pending a separate explicit owner instruction under `CI_POLICY.md`.