# V26 host reference generation binding

Lane-Key: `issue-4445`

## Scope

This lane hardens repository-safe V26 build/release reference admission. It does not claim licensed BricsCAD runtime qualification, signing, or `LOCAL_PASS`.

## Proven defect

`assert-v26-host-reference-safety.ps1` currently rejects missing/reparse-backed `bricscad.exe`, `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` and validates the V26 host major. Both V26 workflows then consume the same paths later. A same-path replacement after admission can therefore make the build/runtime consume a different generation from the one that was admitted.

## Required contract

The shared helper must capture stable generation evidence for all four required host files using streaming SHA-256, length, and UTC last-write ticks with a second ordinary/non-reparse resolve/hash. The evidence must be serialized to a bounded machine-readable state file. Verification mode must re-read that state strictly, re-resolve each required path, reject reparse transitions/missing or unexpected entries, and compare length/timestamp/hash before the consuming build/runtime boundary.

Both `.github/workflows/bricscad-v26.yml` and `.github/workflows/release-v26.yml` must create an isolated state path, capture it in the existing V26 host-safety step, and run verification immediately before the V26 plugin build. Workflows that invoke licensed runtime must also keep generation verification ordered before the runtime invocation. Existing V26-only host-major, .NET 8, release, signing, runtime and package semantics remain unchanged.

## Deterministic guard

`scripts/preflight-v26-host-reference-path-safety.py` is auto-discovered by aggregate preflight. It must reject mutations that remove stable-state capture, stable-state revalidation, workflow state creation, or pre-build verification ordering while preserving the existing absolute/reparse/ordinary-file checks.

## Validation

Run the focused guard first, then `scripts/preflight-all.py`, tracked PowerShell syntax validation through Shared CI, and the normal exact-head `preflight + core` path. Reconcile latest `main` non-force before protected PR evidence. Merge only the same canonical PR with expected-head protection and verify exact protected `main` afterwards.
