# V26 compile-reference MSI publication safety

## Scope

This runbook covers remote-safe acquisition of the pinned BricsCAD V26 installer used to derive compile references. It does not constitute licensed BricsCAD runtime evidence.

## Required contract

1. Download each candidate to a unique sibling staging path under an ordinary/non-reparse cache directory.
2. Admit the staged MSI under a held read stream and validate its SHA-256, Authenticode signer, ProductName and pinned ProductVersion before publication.
3. Keep that admitted staging stream alive throughout publication. Canonical cache bytes must be copied from the held stream itself, never by reopening or moving the staging pathname after admission is released.
4. The canonical destination is fresh-only during publication. `FileMode.CreateNew` must reject a destination that exists or races into existence; do not destructively replace an unadmitted destination generation.
5. Flush the canonical output durably, then immediately re-admit it under the normal V26 installer validator using the staged digest as the expected digest.
6. Require published digest, product identity and signer identity to match the held staged admission before the canonical admission can be returned for extraction.
7. On publication failure, remove only an ordinary canonical leaf created by this attempt; reparse/container state fails closed rather than being recursively or blindly removed.
8. Preserve existing source URL policy, reparse checks, exact pinned-product validation, fresh extraction directory semantics and held canonical MSI state during `msiexec` consumption.

## Deterministic guard

Run from repository root:

```text
python scripts/preflight-v26-compile-reference-msi-publication.py
```

The guard mutation-tests fresh-only creation, held-source copying, durable flush, post-publication admission and digest parity.

## Evidence boundary

Hosted/static validation is REMOTE_SAFE build and release-readiness evidence only. It must not be reported as licensed BricsCAD `LOCAL_PASS`.
