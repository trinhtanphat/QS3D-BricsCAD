# V25 held package ProductVersion identity qualification

Status: `REMOTE_SAFE` source/package identity validation. This runbook does not establish licensed BricsCAD runtime, code-signing, timestamping, or commercial publication PASS.

## Purpose

The V25 package identity admission must distinguish package generations that share the same managed `AssemblyVersion` but have different SemVer `ProductVersion` / `AssemblyInformationalVersion` identities. Metadata `productVersion`, the held V25 plugin, and the held Core assembly must therefore describe one exact semantic product generation.

## Contract

`scripts/assert-v25-release-package-identity.ps1` must:

1. read-hold `PACKAGE-METADATA.json`, `QS3D.BricsCAD.V25.dll`, and `QS3D.Core.dll` while semantic checks execute;
2. obtain managed identity only from the exact held assembly bytes through reflection-only metadata inspection, never by reopening the admitted package pathname;
3. require exactly one `AssemblyInformationalVersionAttribute` with one canonical non-empty string argument on each managed assembly;
4. require plugin/Core `AssemblyVersion` to equal metadata `version`;
5. require plugin/Core informational `ProductVersion` to equal metadata `productVersion` using ordinal equality;
6. preserve strict source SHA, optional strict release-tag, UTF-8/size, reparse, generation-lock, and final pathname-binding checks;
7. expose the admitted plugin/Core ProductVersion values as bounded diagnostic identity evidence.

The informational-version path is intentionally metadata-only: reflection-only loading plus `CustomAttributesData` must not execute candidate assembly code.

## Deterministic regression guard

Run:

```powershell
python scripts/preflight-v25-release-package-held-product-version.py
```

The guard is auto-discovered by aggregate feature preflight. It requires both ProductVersion equality limbs, held-byte reflection-only inspection and exact evidence fields; it rejects pathname semantic reopens/executable loads and mutation-tests removal of the informational-version attribute, plugin equality, Core equality, and held-byte inspection.

## Release boundary

This carrier does not modify `.github/workflows/release-v25.yml`, signing, runtime, upload, rollback, or publication logic. Existing later signed-package finalization remains independent defense-in-depth; this package identity gate now fails earlier when unsigned/admitted package generations are semantically mixed.

## Merge acceptance

Before merge: focused guard PASS, tracked PowerShell syntax PASS, aggregate source guards PASS, exact-head protected `preflight` and `core` terminal GREEN, latest protected-main reconciliation/collision check, expected-head merge, and exact protected-main verification. Hosted/static results must not be described as licensed runtime or commercial release PASS.
