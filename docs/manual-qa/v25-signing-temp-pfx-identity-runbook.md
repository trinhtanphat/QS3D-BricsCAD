# V25 signing PFX in-memory import qualification

Issue: #6143  
Lane: C05 CI / Release / Installer / Quality Gates

## Purpose

Verify that the V25 commercial-signing certificate importer never materializes decoded PFX bytes at a filesystem pathname and preserves the existing fail-closed certificate admission/rollback contract.

## Automated contract

The protected preflight must pass both:

- `scripts/preflight-v25-signing-temp-pfx-identity.py`
- `scripts/preflight-v25-signing-pfx-input-safety.py`

Together they require bounded base64/decoded-size admission, `SecureString` password use, direct managed X509 import from the decoded byte array, CurrentUser/My store placement, non-exportable persisted-key flags, exactly-one expected private-key Code Signing certificate admission, validity checks, newly-imported thumbprint ownership, bounded rollback, object disposal, and decoded-byte zeroing.

They fail closed if pathname primitives return, including `WriteAllBytes`, `Import-PfxCertificate`, temporary PFX path creation/reopen/cleanup, or exportable-key flags.

## Manual Windows check

Use only a disposable test signing certificate and a disposable runner/profile. Do not use production credentials.

1. Confirm the expected thumbprint is absent from `Cert:\CurrentUser\My`.
2. Invoke `scripts/import-v25-signing-certificate.ps1` with bounded base64 PFX input, a `SecureString` password, and the expected thumbprint.
3. Verify exactly one expected private-key Code Signing certificate appears in `CurrentUser\My` and the script reports that thumbprint as newly imported.
4. Verify no temporary `.pfx` file is created by the importer under the runner temp directory or system temp directory.
5. Run a negative case with the wrong expected thumbprint. Verify the operation fails and leaves no certificate introduced by that attempt.
6. Run a malformed/oversized input case. Verify rejection occurs before persistent certificate-store mutation.
7. Remove the disposable certificate and private key after the check.

## Evidence boundary

Hosted CI proves source/guard/build behavior only. A licensed/native Windows signing run must be reported separately if performed; do not relabel hosted CI as licensed BricsCAD runtime evidence.
