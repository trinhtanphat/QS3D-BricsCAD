# V25 held draft signature admission

## Purpose

Bind Authenticode/trusted-timestamp verification of the remotely downloaded commercial V25 draft to the exact ZIP generation already admitted for checksum, provenance, tag, source and package metadata identity.

## Defect boundary

Before this hardening, `assert-v25-commercial-draft-identity.ps1` held the downloaded ZIP/checksum/update/provenance generations only through semantic admission. The release workflow then copied/reopened the ZIP pathname, expanded that later generation and verified signatures there. A coherent pathname replacement after semantic admission could therefore make draft generation A satisfy semantic identity while signatures were checked on generation B.

## Required contract

- `assert-v25-commercial-draft-identity.ps1` opens the downloaded ZIP with read-only sharing and keeps that stream alive through signature admission.
- Signature verification extracts only the six required signed payload entries directly from the held ZIP stream: `QS3D.BricsCAD.V25.dll`, `QS3D.Core.dll`, `install-v25-autoload.ps1`, `uninstall-v25-autoload.ps1`, `update-v25.ps1`, and `unblock-v25-netload.ps1`.
- Each required entry must appear exactly once; missing or duplicate/ambiguous entries fail closed.
- Extraction is bounded per entry and in total, uses a private ordinary non-reparse workspace under `RUNNER_TEMP`, uses create-new/no-share writes, validates written length, and always attempts cleanup.
- The existing `verify-v25-signatures.ps1` remains the Authenticode/trusted-timestamp authority and receives the expected normalized signer thumbprint.
- Held ZIP/checksum/update/provenance streams are disposed only after semantic and signature admission completes.
- The final draft-download-to-publish window must not reopen/copy/expand the ZIP pathname and run a second signature check after held admission.
- Existing draft asset-ID/size/hash verification, tag/source/provenance/update identity, rollback and ambiguous-acknowledgement behavior remain unchanged.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-held-draft-signature.py
python scripts/preflight-v25-commercial-draft-prepublish-identity.py
python scripts/preflight-all.py
```

The focused guard also mutation-tests removal of the held signature call, weakening duplicate-entry rejection, removal of the extraction cap, and reintroduction of a post-admission pathname reopen/signature path.

Protected PR acceptance additionally requires fresh exact-candidate `preflight` and `core` success after latest-main reconciliation.

## Runtime/release boundary

This is repository-safe release transaction hardening. Do not dispatch the production V25 commercial release, use signing credentials, or claim licensed BricsCAD `LOCAL_PASS` as part of source validation. Actual production signing/runtime/publication remains governed by the manual commercial release workflow and its existing authorization/environment requirements.
