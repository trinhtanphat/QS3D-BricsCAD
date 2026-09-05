# V26 release asset endpoint identity

## Scope

This runbook covers remote-safe V26 publication endpoint identity. It does not claim licensed BricsCAD runtime or release publication success.

## Defect boundary

A qualified V26 draft is admitted by exact repository release URI, release id, tag, target SHA, name/body transaction identity, prerelease state, and later exact asset id/size/hash. Before this package, the publisher still trusted two response URLs as routing authorities before those later checks could protect the transaction:

1. `release.upload_url` was normalized directly and handed to the held-file upload helper without proving it belonged to the admitted repository and release id.
2. `uploadedAsset.url` was followed with authenticated GitHub headers during verification without first proving it was the canonical API endpoint for the admitted repository and exact uploaded asset id.

The endpoint identity contract is fail-closed: response URLs are data to validate, not authority to redirect the transaction.

## Required invariants

Before the first held upload, the publisher constructs the canonical endpoint `https://uploads.github.com/repos/$env:GITHUB_REPOSITORY/releases/$releaseId/assets{?name,label}`, compares `release.upload_url` by ordinal equality, and derives `UploadBase` only from that validated canonical value.

For every uploaded asset, after validating a positive asset id and before any authenticated verification GET, the publisher constructs `https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/assets/$uploadedAssetId`, compares `uploadedAsset.url` by ordinal equality, and downloads through the constructed canonical endpoint rather than directly following the response field.

Existing release id/repository/tag/target/name/body/prerelease admission, held local-file generation, asset name/id/length/SHA-256 validation, final protected-main admission, published transaction reconciliation and rollback semantics remain unchanged.

## Deterministic regression

Run:

```text
python scripts/preflight-v26-release-asset-endpoint-identity.py
```

The preflight must fail if either endpoint comparison is removed/commented, if upload base derivation falls back to raw `release.upload_url`, if the verification GET follows raw `uploadedAsset.url`, or if the checks move after the corresponding network operation.

The repository Shared CI auto-discovers this preflight under `All discovered feature source guards`.

## Acceptance

Remote-safe source completion requires the focused preflight, tracked PowerShell syntax checks, all discovered source guards, and protected PR `preflight` + `core` on the exact candidate SHA. Hosted/static evidence must not be reported as `LOCAL_PASS`.