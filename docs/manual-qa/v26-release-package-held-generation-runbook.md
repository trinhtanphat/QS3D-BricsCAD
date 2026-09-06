# V26 release package held-generation identity runbook

## Scope

This runbook covers the C05 release-identity contract implemented by `scripts/assert-v26-release-package-identity.ps1` and `scripts/V26ReleaseIdentityProbe/`. It is REMOTE_SAFE release/package infrastructure. It does not claim a licensed BricsCAD V26 runtime pass or a commercial publication pass.

## Defect model

The validator admits `PACKAGE-METADATA.json`, `QS3D.BricsCAD.V26.dll`, and `QS3D.Core.dll` as ordinary non-reparse files, opens each assembly with a held read `FileStream`, and computes SHA-256 from that stream. The historical assembly-version check then called `AssemblyName.GetAssemblyName($Held.Path)`, which reopened a pathname. An ancestor/path generation switch could therefore make semantic version inspection consume bytes different from the held generation whose SHA-256 was admitted.

The fixed contract never asks an assembly semantic consumer to reopen the candidate pathname. The held assembly stream is reset to offset zero and copied to the standard input of the deterministic .NET 8 metadata probe. The probe uses `PEReader`/`MetadataReader`, rejects malformed or non-assembly images, enforces a 256 MiB input ceiling, reads `AssemblyDefinition.Version`, and does not load or execute candidate code.

## Hosted deterministic checks

`python scripts/preflight-v26-release-package-held-generation.py` is auto-discovered by `scripts/preflight-all.py`. It performs both source-contract and executable regression checks:

1. the package validator must retain held-stream admission/SHA reporting and must not contain the old `GetAssemblyName($Held.Path)` or reflection-only candidate loading;
2. plugin and Core version consumers must both call the held-stream probe while all held streams remain live through the version-equality check;
3. the probe project is built with the installed .NET SDK using list-form process arguments;
4. the built probe parses its own exact DLL bytes delivered through stdin and must report assembly version `1.0.0.0`;
5. malformed stdin must fail closed and must not emit an accepted version marker.

A failing guard is a release-infrastructure failure. Do not disable the guard, update its expected result blindly, or add `continue-on-error`.

## Manual qualification on the V26 release runner

The manual V26 release workflow already executes `actions/setup-dotnet` before package identity validation. On the exact qualified workflow SHA:

1. build/package V26 normally;
2. run `scripts/assert-v26-release-package-identity.ps1` with the packaged metadata, plugin DLL, Core DLL, and exact release tag;
3. require zero exit status and the expected package identity object;
4. confirm the validator's reported plugin/Core SHA-256 values are from the held streams and that managed assembly versions equal `PACKAGE-METADATA.json`;
5. any probe build failure, timeout, malformed PE metadata, ambiguous output marker, truncated stdin copy, or version mismatch must fail the release before signing/publication.

Do not interpret this check as a substitute for the workflow's signed-payload runtime validation, checksum/update-manifest verification, provenance checks, or publication admission.

## Cross-platform and quoting notes

The metadata probe targets `net8.0` and the preflight invokes `dotnet` with list-form arguments, so its hosted regression avoids shell-tokenization differences on Windows/Linux. The production release validator runs on the Windows V26 qualification runner and invokes the resolved `dotnet` application with a quoted project/DLL pathname; candidate bytes travel over redirected binary stdin rather than command-line arguments or temporary pathnames.

## Failure triage

Classify a failure before changing code:

- old pathname reopen present: release identity TOCTOU regression;
- probe build fails: release/toolchain infrastructure defect or missing admitted .NET 8 SDK;
- malformed/non-assembly PE rejected: expected fail-closed behavior;
- correct PE but version differs: package/product identity defect, not a reason to weaken the gate;
- exact-head CI differs across reruns: investigate nondeterminism before merge;
- protected `main` advances: reconcile the carrier, re-check reservations/collisions, and require a new exact-head GREEN run.
