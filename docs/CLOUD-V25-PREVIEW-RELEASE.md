# GitHub-hosted BricsCAD V25 preview release

## Goal

Build and publish an unsigned QS3D BricsCAD V25 prerelease entirely on a GitHub-hosted `windows-latest` runner. A local laptop or self-hosted runner is not required for this preview build path.

The cloud workflow deliberately does **not** claim real BricsCAD NETLOAD/runtime qualification. Runtime qualification remains a separate laptop/VPS/self-hosted step when needed.

## Why no BricsCAD DLLs are committed

`QS3D.BricsCAD.V25.csproj` needs `BrxMgd.dll` and `TD_Mgd.dll` as compile-time references. The cloud workflow obtains them transiently from an authorized official BricsCAD V25 x64 MSI installer and never commits or packages those assemblies.

`scripts/package-v25.ps1` already rejects `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` if they appear in the QS3D release payload.

Do not add fake/reimplemented `BrxMgd.dll` or `TD_Mgd.dll` shims to make CI green. A fake assembly can compile against the wrong binary identity/API surface and does not prove that the plugin will load in BricsCAD.

## One-time GitHub configuration

Repository -> Settings -> Secrets and variables -> Actions.

Create repository secret:

- `BRICSCAD_V25_MSI_URL`: authorized HTTPS URL for the official BricsCAD V25 x64 MSI installer.

Create repository variable:

- `BRICSCAD_V25_MSI_SHA256`: **required** 64-hex SHA-256 of that exact MSI. The cloud workflow fails closed if the digest is missing/malformed and always verifies the downloaded installer before extracting compile references.

When the authorized MSI changes, update the URL/digest pair together after independently obtaining the SHA-256 for that exact installer. Do not leave the digest blank to bypass pinning.

Do not put a BricsCAD license key in the workflow. The cloud preview workflow does not launch BricsCAD and does not need runtime activation.

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
5. download the authorized official V25 MSI;
6. verify the downloaded MSI against the required pinned SHA-256;
7. MSI administrative extraction on GitHub-hosted Windows;
8. resolve `BrxMgd.dll` + `TD_Mgd.dll` only as compile references;
9. build `QS3D.BricsCAD.V25.dll` x64/net48;
10. package QS3D while excluding BricsCAD runtime assemblies;
11. create SHA-256;
12. upload Actions artifact;
13. publish a GitHub prerelease.

## Runtime qualification

Use the existing `QS3D Manual V25 Build & Release` / local V25 qualification path when a real BricsCAD V25 NETLOAD/runtime proof is required. That path remains intentionally separate from the GitHub-hosted cloud preview path.

A cloud preview release must be described as unsigned and runtime-unqualified until the real V25 runtime gate is executed.
