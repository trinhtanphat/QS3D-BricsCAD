# GitHub-hosted BricsCAD V25 preview release

## Goal

Build and publish an unsigned QS3D BricsCAD V25 prerelease entirely on a GitHub-hosted `windows-latest` runner. A local laptop or self-hosted runner is not required for this preview build path.

The cloud workflow deliberately does **not** claim real BricsCAD NETLOAD/runtime qualification. Runtime qualification remains a separate laptop/VPS/self-hosted step when needed.

## Why no BricsCAD DLLs are committed

`QS3D.BricsCAD.V25.csproj` needs `BrxMgd.dll` and `TD_Mgd.dll` as compile-time references. The cloud workflow obtains them transiently from the pinned BricsCAD V25.2.10 x64 MSI installer and never commits or packages those assemblies.

`scripts/package-v25.ps1` already rejects `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` if they appear in the QS3D release payload.

Do not add fake/reimplemented `BrxMgd.dll` or `TD_Mgd.dll` shims to make CI green. A fake assembly can compile against the wrong binary identity/API surface and does not prove that the plugin will load in BricsCAD.

## GitHub configuration

The workflow pins the exact BricsCAD V25.2.10 x64 en_US MSI digest established by the verified cloud run:

`F44DF674C0E165D96BF579E243B20A8301E3F395F929779F47BF39A7D9DACDE1`

The workflow uses pinned official HTTPS sources for that exact installer object. After an Actions cache miss, the pinned official HTTPS primary is attempted first, then the pinned official HTTPS secondary, then an optional same-object signed HTTPS fallback. Plain HTTP installer sources are rejected; every candidate must also match the pinned SHA-256 before it can proceed to signature or MSI identity validation.

If the public object requires a signed query URL in GitHub's network environment, optionally create repository secret:

- `BRICSCAD_V25_MSI_URL`: signed HTTPS fallback for the **same pinned public MSI object**. Its scheme, host, effective port and path must exactly match the pinned official URL; only its query string may differ. Embedded credentials and URL fragments are rejected.

Optional repository variable:

- `BRICSCAD_V25_MSI_SHA256`: compatibility/configuration assertion. When configured it must be a 64-hex SHA-256 and must exactly equal the digest pinned in the workflow; it cannot override the pinned digest.

Do not put a BricsCAD license key in the workflow. The cloud preview workflow does not launch BricsCAD and does not need runtime activation.

## Installer cache and integrity

The workflow restores `.cache/bricscad/BricsCAD-V25.2.10-x64.msi` through an immutable pinned revision of `actions/cache` corresponding to v6.1.0, with a cache key that includes the exact pinned SHA-256. A cache hit is re-verified before use; the cache is never trusted merely because GitHub returned it.

On a cache miss, the workflow downloads the installer only from the approved HTTPS candidates, requires the exact pinned SHA-256, then verifies a mandatory valid Bricsys Authenticode signer. It also verifies that MSI ProductName identifies BricsCAD and that MSI ProductVersion must identify V25.2.10. Only after those checks may administrative extraction begin.

After a successful verified acquisition, the immutable pinned `actions/cache` save action stores the exact MSI for future workflow runs. The extracted BricsCAD runtime directory is **not** cached and is not uploaded as a QS3D artifact; `BrxMgd.dll` and `TD_Mgd.dll` remain transient compile references only.

The download has a finite download timeout and the MSI administrative extraction has a finite administrative extraction timeout with verbose MSI log tail output on failure. This prevents a hosted runner from waiting indefinitely at the installer step.

When the pinned MSI version changes, update the HTTPS URL, ProductVersion check, pinned SHA-256, cache key, and documentation together. Establish the new installer digest independently and do not reintroduce plain-HTTP installer transport.

## Run

Actions -> `QS3D Cloud V25 Preview Build & Release` -> Run workflow.

For the current preview:

- branch: `main`
- `release_tag`: `v0.1.0-preview.2`
- `confirm_release`: `RELEASE`

The workflow performs:

1. checkout exact `main` SHA;
2. manual-only/preflight gates;
3. Core Release build;
4. deterministic Core smoke tests;
5. restore the exact V25.2.10 MSI from Actions cache when available;
6. on cache miss, try the pinned official HTTPS primary, pinned official HTTPS secondary, then optional same-object signed HTTPS fallback;
7. require the exact pinned MSI SHA-256 and re-check it even on cache hit;
8. verify the mandatory valid Bricsys Authenticode signer;
9. verify MSI ProductName + V25.2.10 ProductVersion;
10. perform bounded MSI administrative extraction with verbose failure logging;
11. save the verified MSI to Actions cache after a cache miss;
12. resolve `BrxMgd.dll` + `TD_Mgd.dll` only as compile references;
13. build `QS3D.BricsCAD.V25.dll` x64/net48;
14. package QS3D while excluding BricsCAD runtime assemblies;
15. create package SHA-256;
16. upload Actions artifact;
17. publish a GitHub prerelease.

## Runtime qualification

Use the existing `QS3D Manual V25 Build & Release` / local V25 qualification path when a real BricsCAD V25 NETLOAD/runtime proof is required. That path remains intentionally separate from the GitHub-hosted cloud preview path.

A cloud preview release must be described as unsigned and runtime-unqualified until the real V25 runtime gate is executed.
