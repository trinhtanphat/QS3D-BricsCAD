# V25 commercial release asset held-generation verification

Lane-Key: `issue-4673`

## Purpose

The manual V25 commercial release workflow must verify exactly the file generation that was admitted. A prior pathname check followed by `Get-FileHash` or `Expand-Archive` on the pathname is insufficient because the pathname can be replaced between those operations.

## Source contract

`scripts/verify-v25-held-file.ps1` is the repository-safe primitive for V25 commercial asset verification. It canonicalizes the path, rejects reparse-point ancestry and a reparse leaf, records length and last-write ticks, opens the file with read access and `FileShare.Read`, immediately rebinds the pathname, and fails closed unless canonical identity, rebound length/write-time, and held stream length still match the admitted generation.

`Hash` computes SHA-256 directly from the held stream. `Copy` copies directly from the held stream to a new destination and verifies copied length. The workflow uses the stable held copy as the ZIP extraction source so digest verification and payload/signature inspection cannot silently observe different source generations.

## Validation contract

`preflight-v25-release-asset-held-generations.py` is auto-discovered and rejects pathname-only hashing, weak sharing, missing stream-length binding, or consumption before the open/rebind boundary. `preflight-release-asset-integrity.py` continues to pin draft-first publication, exact remote tag binding, exact downloaded asset set, digest comparison, checksum verification, Authenticode verification, and publication ordering while requiring the held-generation helper.

Run the normal exact-head Shared CI for the canonical branch. Protected current-candidate `preflight` and `core` must both succeed before merge. No signing credential, GitHub release publication, licensed BricsCAD runtime, or `LOCAL_PASS` is required or claimed by this source-safe package.
