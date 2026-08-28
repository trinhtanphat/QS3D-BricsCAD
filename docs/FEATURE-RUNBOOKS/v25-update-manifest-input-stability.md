# V25 update manifest input-generation stability

Lane-Key: `issue-4413`

## Scope

This repository-safe package hardens `scripts/new-v25-update-manifest.ps1`. It does not publish a release, use signing credentials, launch BricsCAD, or claim licensed runtime evidence.

## Defect boundary

The manifest generator admits the signed package directory, `PACKAGE-METADATA.json`, signed payload files, and the package ZIP as ordinary non-reparse paths. It later reopens those same paths for bounded metadata parsing, Authenticode and managed identity checks, staging/ZIP parity hashing, and manifest materialization. Without generation binding, same-path replacement between admission and consumption can detach the trust decision from the bytes actually consumed.

## Required contract

For metadata, package ZIP, and every signed staging payload:

1. resolve an ordinary non-reparse file and reject any reparse ancestor;
2. capture length, UTC last-write ticks, and a streaming SHA-256 fingerprint;
3. re-resolve and re-fingerprint before publishing the captured state;
4. consume metadata, signature, managed identity, or ZIP parity only after the stable state is captured;
5. immediately recapture and compare the full state after each trust/identity consumption boundary;
6. fail closed on disappearance, reparse transition, path replacement, length/timestamp drift, or SHA-256 content drift;
7. preserve existing strict SemVer, signer thumbprint, metadata product/target/version checks, package ZIP/staging exact parity, bounded temporary verification workspace, and atomic output publication.

## Deterministic guard

Run:

```text
python scripts/preflight-v25-update-manifest-input-stability.py
```

The guard must fail on current-main behavior until the stable-state contract is implemented. It mutation-locks the second fingerprint, metadata post-read recheck, ZIP post-verification recheck, ordering, and removal of unsafe path-reopening hash shortcuts.

## Acceptance boundary

Hosted source/CI success is sufficient for this repository-safe hardening package. No `LOCAL_PASS`, licensed BricsCAD host, private DWG, signing credential, or release publication is required or claimed. Merge still requires current protected `preflight` + `core`, latest-main reconciliation, expected-head protection, and exact-main verification.
