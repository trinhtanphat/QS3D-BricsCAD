# Work claim — V25 Mark-of-the-Web manual NETLOAD recovery

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T11:50:00+07:00`
- Baseline main SHA: `674aa692e92255a112dc1ea906614d54183af33a`
- Priority: owner-reported BricsCAD V25 NETLOAD failure with `.NET Framework` "Operation is not supported" when loading the release DLL from an extracted Desktop package.

## Reserved scope

Harden the V25 release-package/manual-NETLOAD path against Windows Mark-of-the-Web (`Zone.Identifier`) failures without weakening BricsCAD or PowerShell security. Add a package-local one-click recovery helper, package it deterministically, keep signed-release executable coverage complete, document the safe install/manual fallback, and add a focused static regression guard for this distribution contract.

## Expected surfaces

- `scripts/package-v25.ps1`
- `scripts/INSTALL-QS3D.cmd` only for comparison; no behavior change expected
- `scripts/UNBLOCK-QS3D.cmd` (new)
- `scripts/unblock-v25-netload.ps1` (new fail-closed package-integrity helper)
- `scripts/finalize-v25-signed-package.ps1` — include the new executable helper in signed payload coverage
- `.github/workflows/release-v25.yml` — sign/verify the new executable helper when signed V25 packaging is requested; do not dispatch the workflow
- `scripts/preflight-update-install-ux.py` — extend the existing installer/MOTW source guard and lock signed-helper coverage
- `README.md`
- generated V25 `README.txt` text embedded in `scripts/package-v25.ps1`
- this claim file for close-out

## Excluded scope

- `scripts/install-v25-autoload.ps1` behavior; its existing verified-install path already unblocks copied payloads and remains unchanged
- plugin startup/lifecycle code after an assembly has successfully loaded
- V25/V26 product-version and release-tag synchronization automation
- V26 package behavior
- updater release-channel behavior
- BricsCAD security/trusted-path relaxation
- GitHub Actions dispatch/release publication
- licensed BricsCAD runtime qualification

## Validation plan

- preserve `RemoteSigned`; do not introduce `ExecutionPolicy Bypass`
- make `unblock-v25-netload.ps1` verify the full `SHA256SUMS.txt` coverage and required V25 package identity files before recursively removing Mark-of-the-Web
- verify `UNBLOCK-QS3D.cmd` uses one `RemoteSigned` PowerShell process and invokes the package verifier; invalid or incomplete packages must fail before payload unblocking
- verify `package-v25.ps1` includes both new recovery files before `SHA256SUMS.txt` generation
- require signed V25 releases to Authenticode-sign and verify `unblock-v25-netload.ps1`, and have signed-package finalization record it in `signedExecutablePayload`
- extend/run the focused deterministic `preflight-update-install-ux.py` guard where execution is available; otherwise perform exact-source readback and report the execution limitation without fabricating a PASS
- re-fetch `main` and recheck same-path claims immediately before implementation push

## Coordination

The earlier V25 preview package/run-140 claim currently uses a non-reserving `SOURCE_FIXED / AUTOMATION_HARDENED / PENDING_FRESH_CI` status and explicitly excludes updater/product UX; this lane does not change release-tag/version synchronization. The completed BREP/cloud-build lane targets workflow compile-reference acquisition, not release-package manual-NETLOAD MOTW recovery. Stop rather than stack changes if a new `ACTIVE`/`BLOCKED` same-path claim appears.

## Completion condition

The secure manual-NETLOAD MOTW recovery path, signed-release coverage, packaging inclusion, documentation and regression guard are pushed to current `main`; the claim is marked `COMPLETED` with exact implementation SHA/readback evidence and any remaining LOCAL_ONLY V25 validation is stated explicitly.
