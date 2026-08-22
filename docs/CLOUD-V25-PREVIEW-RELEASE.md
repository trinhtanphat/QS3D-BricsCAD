# GitHub-hosted BricsCAD V25 preview release

## Goal

Build and publish an unsigned QS3D BricsCAD V25 prerelease entirely on a GitHub-hosted `windows-latest` runner. A local laptop or self-hosted runner is not required for this preview build path.

The cloud workflow deliberately does **not** claim real BricsCAD NETLOAD/runtime qualification. Runtime qualification remains a separate laptop/VPS/self-hosted step when needed.

## Why no BricsCAD DLLs are committed

`QS3D.BricsCAD.V25.csproj` needs `BrxMgd.dll` and `TD_Mgd.dll` as compile-time references. The cloud workflow obtains them transiently from the pinned BricsCAD V25.2.10 x64 MSI installer and never commits or packages those assemblies.

`scripts/package-v25.ps1` already rejects `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` if they appear in the QS3D release payload.

Do not add fake/reimplemented `BrxMgd.dll` or `TD_Mgd.dll` shims to make CI green. A fake assembly can compile against the wrong binary identity/API surface and does not prove that the plugin will load in BricsCAD.

## GitHub configuration

The workflow contains the pinned HTTPS public object URL for the official BricsCAD V25.2.10 x64 en_US MSI. No laptop and no mandatory repository variable are required for the normal pinned HTTPS cloud preview path.

The workflow also contains the approved pinned HTTP mirror for the same V25.2.10 x64 en_US installer. Because transport integrity is not provided by plain HTTP, that mirror is **not** an unconditional fallback: it is considered only when the exact MSI digest is configured through `BRICSCAD_V25_MSI_SHA256`. The HTTP mirror is skipped when no SHA-256 pin is configured.

If the public object requires a signed query URL in GitHub's network environment, optionally create repository secret:

- `BRICSCAD_V25_MSI_URL`: signed HTTPS fallback for the **same pinned public MSI object**. The workflow rejects a fallback that does not start with the pinned official object URL.

Integrity repository variable:

- `BRICSCAD_V25_MSI_SHA256`: optional for the normal pinned HTTPS path, but required before the workflow will consider the pinned HTTP mirror. It must be the trusted 64-hex SHA-256 of the exact V25.2.10 x64 en_US MSI. Whenever configured, a mismatch blocks extraction regardless of which candidate supplied the file.

The SHA pin remains optional for the pinned HTTPS public/signed-object path, where normal TLS transport is combined with the mandatory installer checks below. It is a hard prerequisite for the plain-HTTP mirror. Before any administrative extraction, the workflow always calculates and logs the downloaded MSI SHA-256, requires a mandatory Authenticode signature, verifies that the MSI ProductName is BricsCAD, and requires the MSI ProductVersion must identify V25.2.10.

When the pinned MSI version changes, update the pinned object/version checks together. Before enabling an HTTP mirror for a new installer, independently establish and configure its trusted SHA-256 first.

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
5. consider the pinned HTTP mirror only when `BRICSCAD_V25_MSI_SHA256` is configured, otherwise use the pinned official HTTPS object or same-object signed HTTPS fallback;
6. calculate and log the downloaded MSI SHA-256, enforcing the configured pin whenever present;
7. verify the mandatory Authenticode signature;
8. verify MSI ProductName + V25.2.10 ProductVersion;
9. MSI administrative extraction on GitHub-hosted Windows;
10. resolve `BrxMgd.dll` + `TD_Mgd.dll` only as compile references;
11. build `QS3D.BricsCAD.V25.dll` x64/net48;
12. package QS3D while excluding BricsCAD runtime assemblies;
13. create package SHA-256;
14. upload Actions artifact;
15. publish a GitHub prerelease.

## Runtime qualification

Use the existing `QS3D Manual V25 Build & Release` / local V25 qualification path when a real BricsCAD V25 NETLOAD/runtime proof is required. That path remains intentionally separate from the GitHub-hosted cloud preview path.

A cloud preview release must be described as unsigned and runtime-unqualified until the real V25 runtime gate is executed.
