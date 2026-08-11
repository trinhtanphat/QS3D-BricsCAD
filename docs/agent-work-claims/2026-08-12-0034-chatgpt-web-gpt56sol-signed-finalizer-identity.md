# Work claim — signed finalizer package identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-signed-finalizer-identity`
- Registered: `2026-08-12T00:34:00+07:00`
- Baseline main SHA: `f5565546158d21391ded509d264fc86e3db3c486`
- Priority: owner-requested continue-all review; close a release-integrity gap where the signed package finalizer validates executable signatures and only the plugin AssemblyVersion, but does not re-bind PACKAGE-METADATA product/target/productVersion and Core identity before regenerating hashes/ZIP.

## Reserved scope

Harden `scripts/finalize-v25-signed-package.ps1` so the exact package being finalized must retain canonical QS3D / BricsCAD V25 metadata and exact metadata AssemblyVersion/productVersion identity across both signed managed DLLs before metadata mutation, hash regeneration or ZIP publication. Add an auto-discovered static/model regression and align release documentation.

## Expected surfaces

- `scripts/finalize-v25-signed-package.ps1`
- `scripts/preflight-signed-finalizer-identity.py` (new)
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- signing certificate/timestamp policy, package creation, updater/coordinator, installer/uninstaller, release workflow dispatch/publication, `src/**`, `tests/**`, active product lanes and licensed BricsCAD runtime.

## Validation plan

- Re-fetch exact finalizer blob and inspect implementation diff.
- Require metadata product=`QS3D`, target=`BricsCAD V25 x64`, version + productVersion, and bind both `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` AssemblyVersion/ProductVersion before `ShouldProcess`, metadata rewrite, hash manifest rewrite or ZIP compression.
- Preserve existing Authenticode signer checks for all executable payloads.
- Regression model must reject product/target/version/productVersion substitution and Core/plugin mismatch.
- Execute deterministic Python model/source regression where practical; no signing/release operation is available in this connector environment.
- No GitHub Actions dispatch/re-run.

## Coordination

Recent active product claims are in Grid/browser/generated ownership/updater and unrelated source lanes. No current claim was found for the signed finalizer helper. Historical installer/updater identity work remains preserved and is not edited here.

## Completion condition

The signed finalizer fails closed on metadata/DLL identity substitution before mutating package metadata or publishing a ZIP, regression/docs are on `main`, and this claim is marked `COMPLETED`.
