# V26 cloud installer provenance qualification

## Scope

This REMOTE_SAFE contract binds a V26 cloud candidate to the exact admitted BricsCAD V26.2.07 installer digest carried by `BRICSCAD_V26_PINNED_MSI_SHA256`. It is dependency/package provenance only. Hosted/static evidence from this runbook is never licensed BricsCAD `LOCAL_PASS`.

## Source contract

`scripts/new-v26-candidate-provenance.ps1` consumes `BRICSCAD_V26_PINNED_MSI_SHA256` through `InstallerSha256`. Candidate emission fails closed unless that value is canonical lowercase 64-hex. The emitted provenance contains one `installerSha256` value alongside the existing source commit, package SHA-256, product version and four held host-reference identities.

`scripts/assert-v26-candidate-identity.ps1` independently consumes the admitted expected installer digest through `ExpectedInstallerSha256`. The workflow input/variable boundary may supply any valid 64-hex casing, so admission first validates 64-hex and normalizes only that expected value to lowercase. The downloaded provenance value itself must already be canonical lowercase 64-hex and must equal the normalized expected digest by ordinal comparison.

Before `ConvertFrom-Json`, admission scans JSON property tokens using Windows-PowerShell-compatible logic, decodes JSON escapes in property names, and requires exactly one semantic `installerSha256` property. This prevents duplicate-key last-value behavior from becoming a fail-open identity path while remaining forward-compatible with unrelated additional provenance fields. Installer admission completes before the held publisher script is parsed or executed. Existing ZIP/checksum/source/version/update-manifest/held-host-reference generation checks remain authoritative.

The cloud workflow retains one installer authority: `installer-cache` computes the canonical lowercase digest used to qualify/build the candidate, while release-time admission validates the held candidate against the already-admitted expected installer identity. This carrier does not publish the MSI as a QS3D artifact.

## Deterministic validation

Run from repository root:

```powershell
python scripts/preflight-v26-cloud-installer-provenance.py
python scripts/preflight-v26-cloud-host-reference-provenance.py
```

The focused auto-discovered guard fails if the generator environment binding, emission canonicality, provenance field, validator environment binding, expected-digest syntax validation/normalization, unique-property admission, provenance canonicality or exact digest equality is removed. It also requires installer admission to precede admitted publisher parsing/execution.

Shared CI remains the repository-level authority for auto-discovered feature guards, tracked PowerShell syntax, reservation/collision policy and applicable Core/build validation.

## Negative controls

Candidate provenance emission/admission must reject:

- absent, blank, short, long or non-hex installer identity;
- non-lowercase `installerSha256` in emitted/downloaded provenance;
- missing, duplicate, escaped-duplicate or malformed `installerSha256` provenance;
- a canonical provenance digest that differs from the normalized admitted expected digest;
- any attempt to execute the admitted publisher before installer provenance admission succeeds.

An uppercase or mixed-case expected workflow input is not a distinct digest and is normalized only after proving it is exactly 64 hexadecimal characters. Unknown unrelated provenance fields remain schema-compatible. Errors report the violated identity contract and do not print installer bytes, credentials, tokens, digest contents or machine-local host-reference paths.

## Acceptance boundary

A source candidate is ready for merge only after fresh exact-head Shared CI succeeds, latest protected `main` is reconciled without reservation collision, protected `preflight` and `core` are successful on the current candidate, and the expected head is merged through the protected PR path. No release dispatch, signing claim or licensed runtime PASS is part of this carrier.
