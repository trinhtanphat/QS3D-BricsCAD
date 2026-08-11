# Agent Work Claim — auto-update product SemVer binding hardening

- Claim ID: `AUTO-UPDATE-PRODUCT-SEMVER-BINDING-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T21:17:20+07:00`
- Baseline main SHA: `5189d11a7658e2a2ff8566c7bb8a48db7c7629cd`
- Parent lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811` (`RELEASED`)

## Verified defect

The current plugin performs strict GitHub SemVer selection before one-click scheduling, but `update-v25.ps1` ultimately authorizes the package by `AssemblyVersion`. Prereleases such as `0.1.0-preview.2` and `0.1.0-preview.3` can intentionally share `AssemblyVersion 0.1.0.0`. The current `-AllowSameVersion` compatibility handoff therefore permits a newer prerelease, but the downloaded package is not independently required to have a product SemVer newer than the installed product SemVer.

That leaves a replay/downgrade gap within one AssemblyVersion family: a same-publisher package with an older `productVersion` can satisfy the assembly-version layer unless the updater also binds and compares product SemVer.

## Reserved scope

Harden the package/update scripts so product SemVer is an independent mandatory authorization boundary.

Expected surfaces:

- `scripts/new-v25-update-manifest.ps1`
- `scripts/update-v25.ps1`
- `scripts/preflight-update-product-version-binding.py` (new)
- this claim file

## Explicit non-overlap

The concurrently active `updater Authenticode verification` claim owns `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs` and `scripts/preflight-auto-update.py`. This lane MUST NOT edit either file.

Also excluded: `src/QS3D.Core/**`, Commands/Ribbon/Start Center/Quantity/Workspace/Room/Family/rebar/curtain surfaces, release workflow dispatch/publication, signing keys/certificates, and unrelated installer semantics.

## Planned hardening

1. Promote newly generated update manifests to schema 2 and include canonical `productVersion` from `PACKAGE-METADATA.json`.
2. Validate manifest-generation `productVersion` as strict QS3D SemVer and keep its package metadata / signed assembly binding checks.
3. Teach `update-v25.ps1` to parse and compare strict SemVer for installed and target product versions.
4. Require manifest `productVersion` to equal downloaded `PACKAGE-METADATA.productVersion` exactly (normalized optional leading `v` only where explicitly supported).
5. Reject target product SemVer lower than or equal to the installed product SemVer during normal update. `-AllowSameVersion` remains only an AssemblyVersion compatibility flag and must not authorize product-version replay/downgrade.
6. Preserve independent AssemblyVersion, SHA-256, Authenticode, archive-safety, host allowlist and atomic installer checks.
7. Add a separate auto-discovered preflight to lock schema 2/productVersion/newer-SemVer behavior without touching the Authenticode lane's preflight file.

## Validation / release conditions

- Re-read latest `main` before each write and preserve concurrent changes.
- Re-fetch committed script/preflight source after writes.
- Verify implementation commits remain ancestors of current `main` (`behind_by: 0`).
- Do not dispatch GitHub Actions.
- Native signed update execution remains `LOCAL-009 / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
- Set this claim `RELEASED` only after source and regression gate are committed on `main`.
