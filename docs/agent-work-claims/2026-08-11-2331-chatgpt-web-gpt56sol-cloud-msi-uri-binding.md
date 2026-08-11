# Work claim — cloud MSI URI object binding

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-cloud-msi-uri-binding`
- Registered: `2026-08-11T23:31:00+07:00`
- Completed: `2026-08-11T23:36:00+07:00`
- Baseline main SHA: `be3e592a87e3f12e07731e4e147cbcd799a5135a`
- Priority: owner-requested whole-repository review; close a verified cloud release policy fail-open where the signed-secret fallback URL used string-prefix matching even though policy allows only the pinned official MSI object's query string to differ.

## Reserved scope

Harden `.github/workflows/release-v25-cloud.yml` fallback MSI URL validation so a configured secret URL must resolve to the exact pinned official HTTPS scheme/host/port/path object, with only query parameters allowed to differ. Reject embedded credentials and fragments. Preserve the exact pinned SHA-256, Bricsys Authenticode signer and MSI ProductName/ProductVersion checks. Extend the existing cloud preview preflight and documentation.

## Completed changes

- `57abcf0e37e4d25b9df077c9256fa40c1ceac5d3` — replaced string-prefix fallback validation with URI-component identity checks: HTTPS scheme, host, effective port and absolute path must match the pinned public MSI object; embedded credentials/fragments are rejected; query is intentionally allowed to differ.
- `a7d76d2fff461cced33ba6110e0abdbc065e0e40` — expanded `scripts/preflight-cloud-v25-preview-release.py` with a URI-object regression model, source-token/order guards and an explicit regression ban on the previous `StartsWith(...)` implementation.
- `d4d19ade5ef4cb46347cb8e37f026b18783bfed7` — documented exact same-object fallback semantics and corrected stale cache action references from v4 to the workflow's current v6.

## Validation evidence

- Inspected exact workflow commit `57abcf0e...`; GitHub diff contains only the fallback URI validation block. Download ordering, pinned digest, cache, Authenticode, MSI identity, extraction and publication logic were untouched.
- Re-fetched the current preflight blob `888626cc4c59c40470a2fe2664cd278ff073a01a`; it requires scheme/host/port/path identity, credentials/fragment rejection, removal of prefix matching, and URI validation before the secret enters the candidate list.
- Executed the URI regression model with Python: exact pinned URL and query-only signed variant pass; path suffix, child path, alternate host, HTTP scheme, alternate port, embedded credentials and fragment variants all fail.
- Existing defense-in-depth remains: exact pinned SHA-256 is verified before MSI extraction, followed by valid Bricsys Authenticode signer and V25.2.10 MSI identity checks.
- No GitHub Actions were dispatched/re-run. No MSI was downloaded, no release was published and no licensed BricsCAD V25 runtime qualification was performed or claimed.

## Coordination / exclusions respected

A first claim creation attempt encountered a normal `409` because concurrent `main` moved; it was abandoned without force and retried after re-sync. No updater/product code, signing key policy, mirror/digest version, cache ordering or active feature lane was changed.

## Result

The optional cloud signed-secret fallback can no longer satisfy policy merely by starting with the pinned public URL; it must identify the exact same official MSI object, with only query parameters allowed to differ. This lane is complete and released.
