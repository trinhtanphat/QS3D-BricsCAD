# V25 release package held assembly identity

## Scope

This contract is REMOTE_SAFE release/package identity infrastructure. It does not establish licensed BricsCAD runtime, signing, timestamping, or commercial publication PASS.

The canonical validator is `scripts/assert-v25-release-package-identity.ps1`. Given the admitted `PACKAGE-METADATA.json`, it resolves the packaged `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` from that same package directory.

## Required identity boundary

The validator must keep read-held generation locks on metadata, plugin, and Core through all cross-identity checks. The two managed assembly versions are obtained from bytes read from those exact held streams with reflection-only loading; candidate code is not executed and the DLL pathnames are not reopened for semantic inspection.

Acceptance requires:

- ordinary non-reparse metadata/plugin/Core files and ancestors;
- strict bounded UTF-8 metadata;
- exact product `QS3D` and target `BricsCAD V25 x64`;
- exact 40-hex source commit equality;
- existing exact release-tag/productVersion equality when a tag is supplied;
- canonical parseable metadata managed `version`;
- plugin managed assembly version == Core managed assembly version == metadata `version`;
- pathname/generation binding remains stable before and after semantic inspection.

Malformed assemblies, missing packaged DLLs, oversize assembly inputs, generation drift, or any cross-version mismatch fail closed.

## Deterministic regression

Run:

```text
python scripts/preflight-v25-release-package-held-assembly-identity.py
```

The guard also mutation-tests regression to pathname-based assembly semantic reopening, removal of Core-version equality, and loss of held Core cleanup.

The repository aggregate auto-discovers this guard. Protected merge still requires fresh exact-candidate `preflight` and `core` under current repository policy.

## Runtime boundary

Do not claim `LOCAL_PASS` from this validation. No BricsCAD process is required. Production signing, timestamping, release dispatch, and licensed host qualification remain separately controlled.
