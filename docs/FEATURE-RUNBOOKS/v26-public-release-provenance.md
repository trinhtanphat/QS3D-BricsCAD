# V26 public release provenance

## Purpose

The manual V26 release lane already creates a candidate provenance document before the qualification artifact crosses the job boundary. That provenance binds the exact workflow source SHA, release tag/product version, target identity and package SHA-256 used by post-boundary semantic admission.

The published GitHub release must preserve that same provenance as a public release asset rather than discarding it after admission.

## Contract

For both signed releases and explicitly allowed unsigned prereleases, `scripts/publish-v26-release.ps1` publishes `QS3D-BricsCAD-V26.provenance.json` alongside the V26 ZIP and checksum. Signed releases additionally publish the V26 update manifest.

The provenance asset is not decorative metadata. It must participate in the same draft-transaction safety as every other published V26 asset: exact name/cardinality checks, local-to-remote size comparison, downloaded remote byte hashing, retained uploaded asset ID, final published-release identity reconciliation and rollback on failure.

Existing requirements remain unchanged: release tag must be one exact lightweight tag at `GITHUB_SHA`; V25 assets are rejected; stable signing/runtime policy is preserved; the release stays draft until all asset checks pass; ambiguous publication acknowledgement must reconcile only to the verified transaction.

## Deterministic guard

Run:

```text
python scripts/preflight-v26-public-release-provenance.py
```

The guard fails if the publisher stops including provenance in both the upload source set and the exact expected public asset set, or if the existing remote-byte / asset-ID / final-transaction verification primitives disappear.

This runbook and hosted CI are source/release-transaction evidence only. They do not dispatch a production V26 release, exercise signing credentials, or constitute licensed BricsCAD runtime acceptance.
