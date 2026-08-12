# Work claim — BricsCAD V26 package/install/update/release lane

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T00:54:00+07:00`
- Scope reconciled: `2026-08-12T01:08:00+07:00`
- Completed: `2026-08-12T08:03:00+07:00`
- Baseline main SHA: `8bcd4073e21d373293b90e33f802a4a594a181de`
- Priority: owner requested full latest BricsCAD V26 support after the V26 .NET 8 host lane landed.

## Reserved scope

Add a V26-specific packaging, install/uninstall, Authenticode/signature verification, signed-package finalization, update-manifest/updater and manual release lane for the already-landed `QS3D.BricsCAD.V26` project. Preserve existing V25 package/install/security semantics and reuse its hardened security contracts by adaptation rather than weakening/generalizing them.

## Expected surfaces

Prefer new V26-only files wherever possible:

- `scripts/package-v26.ps1`
- guarded generation of standalone V26 install/uninstall/update payload scripts from current hardened V25 templates
- `scripts/sign-v26.ps1`
- `scripts/verify-v26-signatures.ps1`
- `scripts/finalize-v26-signed-package.ps1`
- `scripts/new-v26-update-manifest.ps1`
- `.github/workflows/release-v26.yml`
- V26-specific deterministic preflight(s)
- V26 package/release documentation and LOCAL_ONLY clean-machine qualification updates
- V26-specific update client/manifest/launcher surfaces required for safe one-click update

Shared files may be touched only where host-major coexistence strictly requires it, with deterministic regression coverage and no weakening of V25 security/transaction semantics.

## Reconciled multi-major release-channel requirement

During implementation a required coexistence defect became explicit: V25 and V26 share the same GitHub Releases/tag stream. A client that simply selects the newest SemVer release can treat a release for the other BricsCAD major as its own channel and surface a false/manual-update state or, if host identity were weakened later, risk cross-major package selection.

Before enabling V26 one-click update, this lane therefore also reserves the minimal shared updater/release-client change needed to make **host-specific manifest asset presence part of release-channel membership**. V25 must continue accepting only releases carrying the V25 manifest asset; V26 must accept only releases carrying the V26 manifest asset. This is a required isolation fix, not a broad updater refactor.

## Required security/product invariants

- Package identity must bind `product=QS3D`, `target=BricsCAD V26 x64`, V26 assembly/version, Core assembly/version and package hashes.
- Install/uninstall must fail closed on foreign/custom directories and preserve transactional backup/quarantine/rollback semantics.
- DemandLoad registration must target BricsCAD V26 only and never V25.
- Signed package finalization must re-bind package metadata to both signed managed DLLs before hashes/ZIP are regenerated.
- Update manifest/package URLs and updater filenames must be V26-specific; V26 must never consume `QS3D-BricsCAD-V25.update.json` or a V25 ZIP.
- V25/V26 release discovery must be filtered by the correct host-major manifest asset before latest-version selection.
- Stable release publication must remain owner-dispatched/manual-only, require explicit `RELEASE` confirmation, signing and exact V26 runtime qualification.
- No proprietary BricsCAD DLL, signing certificate/private key or customer DWG may be committed.

## Excluded / LOCAL_ONLY

- Do not publish an actual GitHub Release in this remote lane.
- Do not claim Authenticode signing/timestamp PASS without the real certificate.
- Do not claim clean-machine install/update/uninstall or licensed BricsCAD V26 runtime PASS without the local V26 environment.
- No changes to unrelated product features, AutoCAD support or customer/private artifacts.

## Validation plan

- Re-read the current hardened V25 package/install/update/release scripts and preserve their current invariants in V26 tooling.
- Add deterministic source regression checks proving host-major/product/package/update-channel isolation between V25 and V26.
- Guard the minimal shared release-client channel filter so V25 ignores non-V25 release assets and V26 ignores non-V26 release assets.
- Re-read exact committed V26 files from `main` after publication.
- Record local-only clean-machine/runtime/signing evidence requirements precisely; no fabricated PASS claims.

## Completion record

Source-side scope is complete on `main`.

Key implementation / hardening commits:

- `6ff749868153a699d6e48b5fd5c727800f3b4830` — dedicated `QS3D.V26.sln` lane for Core + V26 adapter + smoke tests.
- `a7091e19e183f7c99a78468488052e6415750d62` — harden manual V26 release publication with qualified-SHA/tag and uploaded-asset integrity checks before draft publication.
- `64197086f40f71bba087de33a4c0cc713bdc2ce3` — make shared `QS3DRUNTIMECHECK` select BricsCAD major 25/26 from the V26 compile symbol instead of pinning V26 to major 25.
- `adf3ac66a4d454e4d7abc7c2dae4bce0aea5d98c` — make the shared runtime-probe x64 failure text host-neutral.
- `dba4cbf59ee3a45fa5895d5ec47d95194ed608b9` — make shared release-readiness qualification text select V25/V26 correctly.
- `6a8cc0c44b711ad0da235f8aa67d9be1fa70e129` — lock the shared runtime host-major contract into `preflight-bricscad-v26.py`.
- `a5f574196cf56f1af789468e96a36d1990a2a2b2` — document remote release-tag and per-asset size/SHA-256 revalidation before publication.

Validation actually performed in this remote lane:

- re-fetched current `main` after the runtime-host fixes and confirmed `RuntimeDiagnosticsCommands`, `RuntimeProbeCommands`, `ReleaseReadinessCommands`, the V26 preflight and the V26 release runbook retain the intended changes after concurrent commits;
- confirmed the V26 adapter still targets `net8.0-windows`, defines `BRICSCAD_V26`, isolates V26 host references and links the shared V25 command source intentionally;
- confirmed the current V26 runtime gate requires BricsCAD major 26 and the exact `QS3D.BricsCAD.V26.dll` payload;
- confirmed the manual V26 release workflow remains `workflow_dispatch`-only and source-side publication integrity checks remain present;
- no GitHub Actions were dispatched or rerun;
- no local `dotnet`/PowerShell build was executed in this hosted session;
- no licensed BricsCAD V26 runtime, clean-machine install/update/uninstall, real Authenticode certificate/timestamp or actual GitHub Release PASS is claimed. Those remain `LOCAL_ONLY` evidence items in `docs/LOCAL-V26-QUALIFICATION.md`.

## Completion condition

Satisfied source-side: V26 has a coherent package/install/update/manual-release lane on `main`, V25 security/transaction behavior remains preserved, host-major release discovery is isolated, shared runtime checks no longer false-fail V26 as V25, deterministic source guards cover the cross-major contracts, LOCAL_ONLY qualification is explicit, and this claim is released as `COMPLETED`.
