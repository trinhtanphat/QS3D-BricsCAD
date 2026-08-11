# Work claim — BricsCAD V26 package/install/update/release lane

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T00:54:00+07:00`
- Baseline main SHA: `8bcd4073e21d373293b90e33f802a4a594a181de`
- Priority: owner requested full latest BricsCAD V26 support after the V26 .NET 8 host lane landed.

## Reserved scope

Add a V26-specific packaging, install/uninstall, Authenticode/signature verification, signed-package finalization, update-manifest/updater and manual release lane for the already-landed `QS3D.BricsCAD.V26` project. Preserve all existing V25 package/update/release behavior unchanged and reuse its hardened security contracts by adaptation rather than weakening/generalizing them.

## Expected surfaces

Prefer new V26-only files wherever possible:

- `scripts/package-v26.ps1`
- `scripts/install-v26-autoload.ps1`
- `scripts/uninstall-v26-autoload.ps1`
- `scripts/sign-v26.ps1`
- `scripts/verify-v26-signatures.ps1`
- `scripts/finalize-v26-signed-package.ps1`
- `scripts/new-v26-update-manifest.ps1`
- `scripts/update-v26.ps1`
- `.github/workflows/release-v26.yml`
- V26-specific deterministic preflight(s)
- V26 package/release documentation and LOCAL_ONLY clean-machine qualification updates

Shared files may be touched only where strictly required to register the new V26 release workflow in an existing deterministic/manual-only policy gate. Do not refactor or alter V25 semantics in this lane.

## Required security/product invariants

- Package identity must bind `product=QS3D`, `target=BricsCAD V26 x64`, V26 assembly/version, Core assembly/version and package hashes.
- Install/uninstall must fail closed on foreign/custom directories and preserve transactional backup/quarantine/rollback semantics.
- DemandLoad registration must target BricsCAD V26 only and never V25.
- Signed package finalization must re-bind package metadata to both signed managed DLLs before hashes/ZIP are regenerated.
- Update manifest/package URLs and updater filenames must be V26-specific; V26 must never consume `QS3D-BricsCAD-V25.update.json` or a V25 ZIP.
- Stable release publication must remain owner-dispatched/manual-only, require explicit `RELEASE` confirmation, signing and exact V26 runtime qualification.
- No proprietary BricsCAD DLL, signing certificate/private key or customer DWG may be committed.

## Excluded / LOCAL_ONLY

- Do not publish an actual GitHub Release in this remote lane.
- Do not claim Authenticode signing/timestamp PASS without the real certificate.
- Do not claim clean-machine install/update/uninstall or licensed BricsCAD V26 runtime PASS without the local V26 environment.
- No changes to unrelated product features, V25 behavior, AutoCAD support or customer/private artifacts.

## Validation plan

- Re-read the current hardened V25 package/install/update/release scripts and copy their current invariants into isolated V26 scripts.
- Add deterministic source regression checks proving host-major/product/package/update-channel isolation between V25 and V26.
- Re-read exact committed V26 files from `main` after publication.
- Record local-only clean-machine/runtime/signing evidence requirements precisely; no fabricated PASS claims.

## Completion condition

V26 has a coherent source-safe package/install/update/manual-release lane on `main`, V25 remains unchanged, deterministic guards prevent cross-major package/update/install mistakes, LOCAL_ONLY qualification is explicit, and this claim is marked `COMPLETED` with exact implementation SHAs and validation actually performed.
