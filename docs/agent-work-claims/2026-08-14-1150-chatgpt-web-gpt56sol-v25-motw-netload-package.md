# Work claim — V25 Mark-of-the-Web manual NETLOAD recovery

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T11:50:00+07:00`
- Baseline main SHA: `674aa692e92255a112dc1ea906614d54183af33a`
- Priority: owner-reported BricsCAD V25 NETLOAD failure with `.NET Framework` "Operation is not supported" when loading the release DLL from an extracted Desktop package.

## Reserved scope

Harden the V25 release-package/manual-NETLOAD path against Windows Mark-of-the-Web (`Zone.Identifier`) failures without weakening BricsCAD or PowerShell security. Add a package-local one-click recovery helper, package it deterministically, document the safe install/manual fallback, and add a focused static regression guard for this distribution contract.

## Expected surfaces

- `scripts/package-v25.ps1`
- `scripts/install-v25-autoload.ps1` — reuse its existing fail-closed SHA256/package-identity verification for an explicit package-only unblock mode before any payload Mark-of-the-Web removal
- `scripts/INSTALL-QS3D.cmd` only if needed to keep launcher/signature behavior consistent
- `scripts/UNBLOCK-QS3D.cmd` (new)
- `scripts/preflight-update-install-ux.py` — extend the existing installer/MOTW source guard rather than adding a duplicate preflight
- `README.md`
- generated V25 `README.txt` text embedded in `scripts/package-v25.ps1`
- this claim file for close-out

## Excluded scope

- plugin startup/lifecycle code after an assembly has successfully loaded
- V25/V26 product-version and release-tag synchronization automation
- V26 package behavior
- updater release-channel behavior
- BricsCAD security/trusted-path relaxation
- GitHub Actions dispatch/release publication
- licensed BricsCAD runtime qualification

## Validation plan

- preserve `RemoteSigned`; do not introduce `ExecutionPolicy Bypass`
- make package-only unblock reuse `Assert-PackageIntegrity` and `Assert-PackageIdentity` before recursively removing Mark-of-the-Web
- verify `package-v25.ps1` includes the helper launcher before `SHA256SUMS.txt` generation
- extend/run the focused deterministic `preflight-update-install-ux.py` guard where execution is available; otherwise perform exact-source readback and report the execution limitation without fabricating a PASS
- re-fetch `main` and recheck same-path claims immediately before implementation push

## Coordination

The earlier V25 preview package/run-140 claim currently uses a non-reserving `SOURCE_FIXED / AUTOMATION_HARDENED / PENDING_FRESH_CI` status and explicitly excludes updater/product UX; this lane does not change release-tag/version synchronization. Recent active BREP/cloud-build work targets workflow compile-reference acquisition, not the release-package manual-NETLOAD MOTW helper. Stop rather than stack changes if a new `ACTIVE`/`BLOCKED` same-path claim appears.

## Completion condition

The secure manual-NETLOAD MOTW recovery path, packaging inclusion, documentation and regression guard are pushed to current `main`; the claim is marked `COMPLETED` with exact implementation SHA/readback evidence and any remaining LOCAL_ONLY V25 validation is stated explicitly.
