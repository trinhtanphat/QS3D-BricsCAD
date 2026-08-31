# V26 release identity input stability

Lane-Key: `issue-4404`

## Scope

This package hardens the repository-safe V26 release package-identity verifier. It does not publish a release, sign artifacts, launch BricsCAD, or claim licensed runtime evidence.

## Defect boundary

The release helper historically admitted `PACKAGE-METADATA.json`, `QS3D.BricsCAD.V26.dll`, and `QS3D.Core.dll` as ordinary non-reparse files, then reopened those paths later for bounded metadata parsing and managed assembly identity reads. That left the admission decision detached from the file generation actually consumed.

## Required contract

For all three inputs the helper must:

1. resolve an ordinary non-reparse leaf and reject any reparse ancestor;
2. capture length, UTC last-write ticks, and a streaming SHA-256 fingerprint;
3. re-resolve after fingerprint capture so replacement during capture fails closed;
4. consume the metadata or assembly identity only after the initial stable state is bound;
5. recapture and compare the full state immediately after consumption;
6. reject disappearance, reparse transition, length drift, timestamp drift, or SHA-256 drift;
7. preserve the existing 64 KiB strict-UTF8 metadata bound, exact product/target/framework/productVersion checks, release-tag equality, and plugin/Core assembly-version parity.

`Get-FileHash` and whole-file materialization are intentionally not used for assembly fingerprints; hashing is streaming through a read-only `FileStream`.

## Deterministic guard

Run:

```text
python scripts/preflight-v26-release-identity-input-safety.py
```

The guard mutation-locks ordinary/reparse admission, strict UTF-8 and metadata bounds, streaming SHA-256 state capture, metadata/plugin/Core before/after stability checks, ordering, and the exact workflow routing through the shared helper.

## Acceptance boundary

Hosted source/CI success is sufficient for this repository-safe release validation change. No `LOCAL_PASS`, BricsCAD host, private DWG, signing credential, or release publication is part of this lane. Merge still requires current protected `preflight` + `core` and exact-main verification under repository policy.
