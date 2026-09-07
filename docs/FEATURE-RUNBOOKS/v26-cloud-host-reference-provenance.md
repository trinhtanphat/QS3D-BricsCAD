# V26 cloud held host-reference provenance

## Scope

This source/package contract binds a V26 cloud preview candidate to the exact BricsCAD V26 host-reference generations used to compile the plugin. It does not authorize release dispatch, signing, licensed runtime execution, or a `LOCAL_PASS` claim.

The canonical source carrier is Issue #6052 / Lane-Key `issue-6052`.

## Boundary

`.github/workflows/release-v26-cloud.yml` already captures `V26_HOST_REFERENCE_STATE` before the held-reference V26 build. `scripts/new-v26-candidate-provenance.ps1` consumes that exact environment-bound state when the workflow creates `QS3D-BricsCAD-V26.provenance.json`.

Immediately before provenance emission, the generator reopens the four recorded host files and verifies the recorded length and SHA-256 still match the admitted state. This keeps the package identity tied to the same host-reference generations that were captured before compilation rather than trusting state JSON as an unverified assertion.

The cloud provenance contains exactly these host-reference identities, in canonical order:

- `bricscad.exe`
- `BrxMgd.dll`
- `TD_Mgd.dll`
- `TD_MgdBrep.dll`

Each entry contains only `name`, positive `length`, and lowercase 64-hex `sha256`; machine-local host paths are not exported into the release artifact.

## Cross-job admission

The qualify job uploads the ZIP, checksum, and provenance artifact. The release job downloads that held candidate and calls `scripts/assert-v26-candidate-identity.ps1` before the admitted publisher script is executed.

The candidate validator requires exactly four host-reference records, exactly one record for every canonical name, canonical lowercase SHA-256, and positive lengths. Missing, duplicated, renamed, malformed, or ambiguous host-reference identities fail closed before publication mutation can begin.

The release job cannot compare the downloaded provenance back to runner-local host paths because those paths intentionally do not cross the job boundary. Its authoritative proof is therefore the generator's pre-emission generation re-verification plus the validator's strict provenance schema admission.

## Failure controls

The source contract fails closed for:

- blank/missing host-reference state path;
- non-ordinary/reparse state or host files;
- oversized, non-UTF-8, or malformed state JSON;
- state version other than 1;
- missing, duplicate, or extra host records;
- noncanonical SHA-256 or non-positive lengths;
- a current host file whose length/hash no longer matches captured state;
- malformed or ambiguous host-reference identities after the artifact/job boundary.

`python scripts/preflight-v26-cloud-host-reference-provenance.py` is auto-discovered by `scripts/preflight-all.py` and guards workflow ordering, environment binding, generation re-verification, provenance emission, post-job validation, and mutation sensitivity.

## Validation

Repository-safe validation for this carrier is:

```text
python scripts/preflight-v26-cloud-host-reference-provenance.py
python scripts/preflight-all.py
```

plus the normal exact-head Shared CI and protected PR `preflight` + `core` contexts.

Do not dispatch `release-v26-cloud.yml` merely to validate this source change. Do not report static/hosted validation as licensed BricsCAD runtime evidence.
