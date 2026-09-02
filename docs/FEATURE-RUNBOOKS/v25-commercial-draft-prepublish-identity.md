# V25 commercial draft pre-publish semantic identity

Lane-Key: `issue-5275`

## Purpose

The final commercial V25 publication decision must be made from the exact downloaded draft asset generations that are about to be published, not only from semantic checks performed earlier in the release job or from equality with mutable local candidate paths.

The final gate therefore re-admits the downloaded `QS3D-BricsCAD-V25.zip`, checksum, update manifest, and provenance together. It holds each ordinary non-reparse file generation read-only, verifies the checksum and update digest from those held streams, parses provenance with bounded strict UTF-8, and reads `PACKAGE-METADATA.json` directly from the same held ZIP generation.

## Required bindings

A publishable downloaded draft must prove all of the following before the final `draft=false` PATCH:

- strict release tag and exact 40-hex workflow source SHA;
- `PACKAGE-METADATA.json` product/target/productVersion/gitCommit binding from the downloaded ZIP;
- provenance product/target/releaseTag/productVersion/sourceCommit/signer binding;
- provenance `packageSha256` equals the held downloaded ZIP SHA-256;
- provenance `updateManifestSha256` equals the held downloaded update-manifest SHA-256;
- checksum text names `QS3D-BricsCAD-V25.zip` and equals the held downloaded ZIP SHA-256;
- existing remote asset-set, local/remote byte parity, Authenticode signature, tag identity, transaction marker, rollback and acknowledgement-reconciliation checks remain enabled.

## Deterministic source gate

Run:

```text
python scripts/preflight-v25-commercial-draft-prepublish-identity.py
```

The guard is auto-discovered by `scripts/preflight-all.py`. Its negative mutations require the final workflow invocation, exact source/tag/signer arguments, held read-only generation handling, ZIP metadata source binding, provenance source binding, and package/update digest bindings.

## Qualification boundary

This is a source/static release-integrity gate. Hosted CI may prove syntax, deterministic source guards, Core smoke/build, and V25 compile-reference/plugin build. It does not dispatch a production commercial release, use signing credentials, run licensed BricsCAD acceptance, or establish `LOCAL_PASS`.
