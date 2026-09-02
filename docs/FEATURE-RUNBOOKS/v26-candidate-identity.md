# V26 post-job-boundary candidate identity

Lane-Key: `issue-5313`

## Purpose

The manual V26 release uses separate qualification and publication jobs. Publication must therefore re-admit semantic identity from the exact artifact generations downloaded by the release job, rather than relying only on checks performed before the artifact boundary.

## Candidate provenance

After the final V26 ZIP and checksum are produced, qualification writes `QS3D-BricsCAD-V26.provenance.json`. The sidecar binds:

- product `QS3D`;
- target `BricsCAD V26 x64`;
- exact release tag and package `productVersion`;
- exact 40-hex workflow source commit;
- SHA-256 of the exact finalized V26 ZIP.

The provenance generator reads `PACKAGE-METADATA.json` from the held ZIP generation and refuses a product/target/framework/tag mismatch.

## Release-job admission

Before `publish-v26-release.ps1`, the release job must call `assert-v26-candidate-identity.ps1` with the downloaded ZIP, checksum, provenance, expected workflow SHA/tag, and the update manifest when signing is enabled.

The admission keeps every supplied ordinary non-reparse file generation open read-only together and requires:

- checksum SHA-256 equals the held ZIP SHA-256;
- provenance source commit equals the exact workflow SHA;
- provenance release tag equals the exact requested tag;
- provenance package SHA-256 equals the held ZIP SHA-256;
- the held ZIP contains exactly one bounded strict-UTF8 `PACKAGE-METADATA.json` with QS3D/V26/net8 identity and productVersion matching the tag/provenance;
- a signed update manifest, when present, has QS3D/V26 identity, the same productVersion, and `sha256` equal to the held ZIP SHA-256.

Existing V26 release request, runtime, signing, lightweight-tag, draft transaction, rollback, remote asset verification and publication acknowledgement contracts remain in force.

## Deterministic source gate

Run:

```text
python scripts/preflight-v26-candidate-identity.py
```

The guard is auto-discovered by `scripts/preflight-all.py` and pins provenance production, artifact inclusion, exact release-job invocation ordering, expected source/tag arguments, signed/unsigned update-manifest handling, held-generation semantics, ZIP metadata admission and digest bindings.

## Qualification boundary

This is source/static release-integrity work. It does not dispatch the production V26 workflow, use signing credentials, or establish licensed BricsCAD `LOCAL_PASS`.
