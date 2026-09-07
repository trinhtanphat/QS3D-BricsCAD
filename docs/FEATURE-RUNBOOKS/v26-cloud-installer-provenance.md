# V26 cloud installer provenance qualification

## Scope

This REMOTE_SAFE contract binds a V26 cloud candidate to the exact admitted BricsCAD V26.2.07 installer digest already carried by `BRICSCAD_V26_PINNED_MSI_SHA256`. It is dependency/package provenance only. Hosted/static evidence from this runbook is never licensed BricsCAD `LOCAL_PASS`.

## Source contract

`scripts/new-v26-candidate-provenance.ps1` consumes `BRICSCAD_V26_PINNED_MSI_SHA256` through its `InstallerSha256` parameter. Emission fails closed unless the value is exactly canonical lowercase 64-hex. The emitted provenance contains one `installerSha256` value alongside the existing source commit, package SHA-256, product version and four held host-reference identities.

`scripts/assert-v26-candidate-identity.ps1` independently consumes the expected installer digest through `ExpectedInstallerSha256`, requires the expected value and the downloaded provenance value to both be canonical lowercase 64-hex, and requires exact ordinal equality before the admitted publisher script is parsed or executed. Existing ZIP/checksum/source/version/update-manifest/held-host-reference generation checks remain authoritative.

The release workflow already carries the exact digest produced by `installer-cache` into downstream V26 jobs as `BRICSCAD_V26_PINNED_MSI_SHA256`; this carrier does not introduce a second installer authority or publish an MSI as a QS3D artifact.

## Deterministic validation

Run from repository root:

```powershell
python scripts/preflight-v26-cloud-installer-provenance.py
python scripts/preflight-v26-cloud-host-reference-provenance.py
```

The focused guard must fail when the generator environment binding, lowercase canonicality check, provenance field, validator environment binding, validator provenance field, validator canonicality check, or exact digest equality is removed. It also requires installer admission to occur before admitted publisher parsing/execution.

Shared CI remains the repository-level authority for auto-discovered feature guards, tracked PowerShell syntax, reservation/collision policy and applicable Core/build validation.

## Negative controls

Candidate provenance emission/admission must reject:

- absent or blank installer digest;
- uppercase, mixed-case, short, long or non-hex installer digest;
- missing or malformed `installerSha256` provenance;
- a canonical installer digest that differs from the admitted expected digest;
- any attempt to execute the admitted publisher before installer provenance admission succeeds.

Errors report the violated identity contract and do not print installer bytes, credentials, tokens or machine-local host-reference paths.

## Acceptance boundary

A source candidate is ready for merge only after fresh exact-head Shared CI succeeds, latest protected `main` is reconciled without reservation collision, protected `preflight` and `core` are successful on the current candidate, and the expected head is merged through the protected PR path. No release dispatch, signing claim or licensed runtime PASS is part of this carrier.
