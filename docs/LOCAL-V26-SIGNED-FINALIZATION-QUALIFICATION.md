# V26 signed package finalization qualification

Status: `PENDING_LOCAL / LOCAL_ONLY / DO_NOT_RETRY_REMOTE`.

This runbook qualifies the existing V26 signing, signature-verification, signed-package finalization and update-manifest pipeline without weakening any production release guard. Hosted CI may validate this runner and its source contract, but only a Windows machine holding the approved signing certificate/private key and timestamp access can produce `LOCAL_PASS`.

## Prerequisites

Use a clean checkout of the exact intended merged-main descendant containing Issue #3865. Install the normal V26 build prerequisites from `LOCAL-V26-QUALIFICATION.md`. The approved code-signing certificate must exist in the current-user or local-machine certificate store with its private key. Keep the thumbprint, certificate/private key and any internal release information out of Git and published logs. The timestamp and package URLs must be HTTPS.

Build/package inputs must be source-controlled and clean before the runner starts. The runner refuses a dirty checkout or a SHA mismatch.

## Command

```powershell
.\scripts\test-v26-signed-finalization.ps1 `
  -ExpectedGitSha '<exact checked-out 40-hex Git SHA>' `
  -ExpectedSignerThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT `
  -TimestampServer $env:QS3D_TIMESTAMP_SERVER `
  -PackageUri 'https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/<approved-tag>/QS3D-BricsCAD-V26.zip' `
  -ArtifactDirectory '<outside-repository evidence directory>'
```

Do not substitute a self-signed certificate for production qualification. Do not publish a release merely to make this row pass; use an owner-approved signed release candidate and release process.

## Acceptance

The runner must prove all of the following on one exact SHA: clean checkout identity; expected signing certificate/private-key availability and validity; package creation; Authenticode signing of both managed DLLs plus installer/uninstaller/updater scripts; signature verification against the expected thumbprint; signed finalization; finalized ZIP SHA-256; V26 update manifest generation using HTTPS; manifest binding to the finalized ZIP digest, requested package URI and expected signer; and post-finalization re-verification of every signed payload.

The only success marker is `QS3D_V26_SIGNED_FINALIZATION_LOCAL_PASS`. The JSON evidence contains the exact Git SHA, finalized package SHA-256, payload signature count, booleans for HTTPS/finalization/manifest binding, and only a SHA-256 digest of the signer thumbprint rather than the raw thumbprint. Do not attach certificate/private-key material or unsanitized machine paths.

Any failure remains `LOCAL_FAIL`/`NO_RESULT` until diagnosed. Hosted/static success is never `LOCAL_PASS`.
