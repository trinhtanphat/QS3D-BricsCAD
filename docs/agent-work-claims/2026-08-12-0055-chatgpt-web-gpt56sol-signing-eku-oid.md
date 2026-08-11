# Work claim — signing EKU OID validation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-signing-eku-oid`
- Registered: `2026-08-12T00:55:00+07:00`
- Corrected against current main: `2026-08-12T00:57:00+07:00`
- Completed: `2026-08-12T01:01:00+07:00`
- Baseline main SHA: `cc830c7f7d87c73738df48f0b4dedbed2e72edbf`
- Priority: owner-requested continue-all review; remove locale/rendering dependence from the production code-signing certificate EKU gate.

## Verified defect

Current `scripts/sign-v25.ps1` required an Enhanced Key Usage extension, located it by OID, then called `Format($false)` and accepted/rejected based on rendered text containing either the code-signing OID or the English words `Code Signing`. X509 extension display text is presentation data and can vary by platform/locale.

## Completed changes

- `aaaad8f1a31594a4a47af1ee6166cec346d6ffec` — replaced formatted-text EKU authorization with `X509EnhancedKeyUsageExtension` + `EnhancedKeyUsages` enumeration and exact ordinal OID `1.3.6.1.5.5.7.3.3` comparison. Missing EKU continues to fail closed.
- `4a8bc658511a68cec11f68382ef1ed01c6386942` — added auto-discovered `scripts/preflight-signing-eku.py`; it bans `Format(...)`/English friendly-name authorization, models missing/Code Signing/client/server/mixed EKUs and pins EKU validation before `Set-AuthenticodeSignature`.
- `2580861b909e71e1c3fe48559d027fe40cfb70a1` — documented the structured Code Signing EKU OID requirement.

## Validation evidence

- Inspected exact implementation diff for `aaaad8f1...`; only the EKU parsing/authorization block changed. CurrentUser certificate lookup, accessible private key, validity period, SHA-256 signing, HTTPS timestamp input, post-sign signature status and exact signer checks are unchanged.
- Re-fetched current signer blob `ba5cb46d8313dd50d89f3df8175f4c73f2046814`; there is no `Format(...)` or English `Code Signing` matching in authorization.
- Re-fetched current preflight blob `ca053e3ee50e497e2ce7c053e5b550068769abbb`; it pins structured EKU validation before signing.
- Deterministic OID model: missing EKU FAIL; Code Signing OID PASS; Client Authentication only FAIL; Server Authentication only FAIL; mixed EKUs including Code Signing PASS.
- PowerShell/certificate-store runtime and real Authenticode signing were not available/executed in this connector environment, so no production-signing qualification is claimed. No GitHub Actions were dispatched/re-run.

## Coordination / exclusions respected

No certificate/key custody, store-location policy, timestamp trust semantics, signature verification helper, package/finalizer/manifest/updater/installer workflow, `src/**`, `tests/**`, active product lane or licensed V25 runtime behavior was changed. No force-push was used.

## Result

Production certificate EKU authorization is now based on the certificate's structured OID data rather than localized display text while preserving the repository's explicit-EKU fail-closed policy. This lane is complete.
