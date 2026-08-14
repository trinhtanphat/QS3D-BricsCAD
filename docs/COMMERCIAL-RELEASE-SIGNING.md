# QS3D V25 commercial release signing

This runbook is the fail-closed path for publishing commercial QS3D packages for BricsCAD V25. The manual workflow is `.github/workflows/release-v25.yml`; the separate cloud preview workflow remains independent and may not be used as evidence for a commercial signed release.

## Security boundary

Commercial release jobs use the protected GitHub Environment `commercial-release`. Configure required reviewers and limit deployment branches to `main`. The build/sign job has `contents: read`; only the downstream release job has `contents: write`.

Repository/environment configuration:

- Secret `QS3D_SIGNING_CERT_PFX_BASE64`: base64 of the commercial Authenticode PFX. Do not store the PFX in git or on the runner.
- Secret `QS3D_SIGNING_CERT_PASSWORD`: PFX password.
- Variable `QS3D_SIGNING_CERT_THUMBPRINT`: exact 40-hex SHA-1 certificate thumbprint expected for the publisher identity.
- Variable `QS3D_TIMESTAMP_SERVER`: absolute HTTPS RFC3161 timestamp endpoint.
- Existing `BRICSCAD_V25_DIR` and `BRICSCAD_V25_PROFILE` variables remain required for the licensed runtime gate when enabled.

The PFX secret and password are exposed only to the certificate-import step. `scripts/import-v25-signing-certificate.ps1` refuses a pre-existing certificate with the expected thumbprint, imports the PFX as non-exportable into `Cert:\CurrentUser\My`, checks private-key ownership, Code Signing EKU, validity dates and exact thumbprint, and returns only the public thumbprint list. The workflow removes every certificate imported by that step using `Remove-Item -DeleteKey` in an `always()` cleanup step. The temporary PFX file and decoded byte buffer are also deleted/cleared. A runner on which cleanup fails must be quarantined before another commercial release.

## Signing and verification contract

Commercial publication has no unsigned fallback. The workflow must complete this order:

1. Verify manual `workflow_dispatch`, explicit `RELEASE` confirmation, `main`, exact workflow SHA and strict SemVer tag.
2. Build Core and the BricsCAD V25 adapter, then run the existing source/smoke gates.
3. Build the V25 package and require `PACKAGE-METADATA.json.productVersion == release_tag.Substring(1)` and metadata `gitCommit == GITHUB_SHA`.
4. Import the ephemeral certificate.
5. Sign `QS3D.BricsCAD.V25.dll`, `QS3D.Core.dll`, `install-v25-autoload.ps1`, `uninstall-v25-autoload.ps1`, `update-v25.ps1`, and `unblock-v25-netload.ps1`.
6. PE files are signed with SHA-256 and an RFC3161 timestamp: `signtool sign /fd SHA256 /tr <url> /td SHA256`. PowerShell payloads use SHA-256 Authenticode signing with the same trusted timestamp service.
7. Verify exact publisher thumbprint, trusted timestamp and Windows Authenticode trust. PE verification includes `signtool verify /pa /all /v`.
8. Finalize the signed package, remove the private signing key, re-open the final ZIP and verify all signed payloads again without access to the private key.
9. Run the licensed V25 runtime gate on the exact signed plugin when required by release policy.
10. Generate the signed update manifest, outer ZIP SHA-256 and provenance record binding release tag, product version, source commit, signer thumbprint and artifact hashes.
11. Cross the job boundary with a GitHub Actions artifact. The release job re-verifies checksums, provenance, package metadata and signatures.
12. Create a GitHub Release as a **draft**, upload the exact candidate, verify the created tag targets `GITHUB_SHA`, download the draft assets, compare the complete asset set and SHA-256 byte-for-byte, verify the downloaded ZIP signatures again, then and only then switch `draft=false`.

Any failed command aborts publication. A failed draft verification deliberately leaves the release unpublished for operator inspection.

## Certificate rotation

For normal rotation, obtain the new commercial Code Signing certificate and RFC3161-compatible chain, export a password-protected PFX, update `QS3D_SIGNING_CERT_PFX_BASE64`, `QS3D_SIGNING_CERT_PASSWORD`, and `QS3D_SIGNING_CERT_THUMBPRINT` together in `commercial-release`, then run a non-public qualification release. Do not retain both old and new private keys on a self-hosted runner. After successful qualification, revoke/delete superseded secrets and document the new public thumbprint in release evidence.

## Revocation or suspected key compromise

Treat suspected private-key exposure as a release stop:

1. Disable or protect the `commercial-release` Environment so no approval can proceed.
2. Revoke the certificate with the issuer and rotate the PFX/password/thumbprint secrets.
3. Quarantine and rebuild any runner that may have held the key; verify no matching certificate/private key remains in `Cert:\CurrentUser\My`.
4. Audit all tags/releases signed by the affected certificate and follow the issuer/vendor revocation guidance.
5. Resume only after a replacement certificate passes the full signed-candidate workflow.

Never bypass revocation or timestamp verification to make a release green.

## Manual artifact verification

On a clean Windows host with the signer chain trusted, download the release ZIP plus `.sha256` and provenance file. First compare the ZIP SHA-256 with the checksum and provenance. Extract the ZIP, inspect `PACKAGE-METADATA.json` for exact product version/source commit, then run:

```powershell
.\scripts\verify-v25-signatures.ps1 -Path @(
  '.\QS3D.BricsCAD.V25.dll',
  '.\QS3D.Core.dll',
  '.\install-v25-autoload.ps1',
  '.\uninstall-v25-autoload.ps1',
  '.\update-v25.ps1',
  '.\unblock-v25-netload.ps1'
) -ExpectedThumbprint '<QS3D_SIGNING_CERT_THUMBPRINT>'
```

For PE files the verifier also executes `signtool verify /pa /all /v`. A missing timestamp, wrong signer, untrusted signature, mismatched checksum, wrong source SHA or relabelled product version is a hard failure.

## Evidence boundary

Repository hardening is complete when these fail-closed controls are merged and their static/source validations pass. A claim that an actual commercial artifact is signed and customer-ready additionally requires configured production secrets, a secure Windows/BricsCAD V25 runner, successful protected-environment approval, and a real workflow run whose published release/tag/artifact hashes are recorded. Do not substitute preview CI or static tests for that operational evidence.
