# V26 release package held-generation identity

## Scope

This source-only release-integrity contract binds V26 package identity validation to the exact admitted file generations for `PACKAGE-METADATA.json`, `QS3D.BricsCAD.V26.dll`, and `QS3D.Core.dll`.

It does not dispatch a release, sign a package, execute licensed BricsCAD, or change production publication authorization.

## Defect boundary

A pathname state captured before a consumer and rechecked afterward does not prove the consumer read that generation. A same-path file can otherwise be replaced after admission, consumed by a pathname-based API, and restored before the post-read check.

`Reflection.AssemblyName.GetAssemblyName(path)` is pathname-based. Package metadata historically also reopened its pathname after state capture. Therefore path/length/write-time/SHA-256 before/after checks alone left a swap-and-restore TOCTOU window.

## Required contract

`scripts/assert-v26-release-package-identity.ps1` must:

1. reject containers and any file or ancestor directory that is a reparse point;
2. open metadata, plugin, and Core as read-only `FileStream` handles with `FileShare.Read` before semantic consumption;
3. bind path, length, UTC write time, and streaming SHA-256 evidence while each handle is held;
4. consume bounded strict UTF-8 package metadata directly from the held metadata stream;
5. keep plugin/Core generation locks active before, during, and after `AssemblyName.GetAssemblyName(path)` so write/delete/replace cannot race pathname consumption;
6. preserve existing product/target/framework/tag/version equality checks;
7. dispose every held handle in `finally`, including on malformed metadata or assembly identity failure.

`FileShare.Read` is intentional: concurrent readers are allowed, while writers/delete/replace operations are denied for the lifetime of the identity check.

## Deterministic regression

Run:

```text
python scripts/preflight-v26-release-package-held-generations.py
```

The auto-discovered guard validates lock/consume/dispose ordering, rejects transient state/reopen helpers, and mutation-tests the critical generation-binding markers.

This preflight is source evidence only. It is not signing evidence, release publication evidence, or licensed V26 runtime qualification.
