# Work claim — update manifest managed identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-update-manifest-managed-identity`
- Registered: `2026-08-12T00:50:00+07:00`
- Baseline main SHA: `5d09620a6ea4654edabe95d3f683439093bb14bd`
- Priority: owner-requested continue-all review; align manifest producer identity with installer/finalizer consumers.

## Verified defect

`new-v25-update-manifest.ps1` verifies signatures for both managed DLLs but binds `PACKAGE-METADATA` AssemblyVersion/productVersion only to `QS3D.BricsCAD.V25.dll`. A signed staging package whose `QS3D.Core.dll` carries a different managed identity can therefore receive a schema-v2 update manifest even though the hardened installer/finalizer reject the same package.

## Reserved scope

Bind metadata AssemblyVersion and productVersion exactly to both signed managed DLLs before ZIP/staging verification and manifest creation. Reuse/extend the existing update-manifest output regression or add a focused identity regression without touching updater runtime code. Align release docs.

## Expected surfaces

- `scripts/new-v25-update-manifest.ps1`
- `scripts/preflight-update-manifest-output-isolation.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- ACTIVE `UpdateCoordinator.cs` generation-safe publication lane; updater selection/network/launcher behavior; output isolation already completed; package/finalizer/signing semantics; installer/uninstaller; workflow dispatch/publication; `src/**`; `tests/**`; licensed V25 runtime.

## Validation plan

- Generalize managed AssemblyVersion/ProductVersion readers and iterate both `QS3D.BricsCAD.V25.dll` + `QS3D.Core.dll`.
- Exact ordinal productVersion and exact AssemblyVersion equality to metadata for both DLLs before ZIP/staging verification and manifest object creation.
- Regression model rejects Core assembly mismatch and Core productVersion/case mismatch while preserving canonical package.
- Re-fetch exact source/regression after writes; no GitHub Actions dispatch/re-run.

## Coordination

The active updater generation publication claim explicitly excludes manifest generation. No concurrent claim was found for update-manifest Core identity binding.

## Completion condition

The manifest producer cannot emit updater metadata for a signed package whose plugin/Core identities disagree with canonical package metadata, with regression/docs on `main` and this claim marked `COMPLETED`.
