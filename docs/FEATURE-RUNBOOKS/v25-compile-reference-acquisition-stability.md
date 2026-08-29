# V25 compile-reference acquisition stability

## Goal

The V25 cloud reference-acquisition path must consume the exact MSI generation that was admitted by the pinned SHA-256 and Bricsys trust checks. A matching pathname is not sufficient identity.

## Contract

`scripts/acquire-v25-compile-references.ps1` keeps the existing cache/download, pinned digest, signer, ProductVersion/ProductName and bounded extraction requirements, then adds a stable-generation boundary before trusted consumption:

1. Re-admit the MSI as an ordinary non-reparse file.
2. Open a read handle with `FileShare.Read`, which intentionally denies write/delete/replace while later path-based readers consume the file.
3. Stream SHA-256 through that held handle and require the pinned digest again.
4. Re-resolve the pathname and bind canonical path, length and UTC last-write ticks to the held stream.
5. Reassert that state before/after Authenticode, after Windows Installer metadata reads, immediately before `msiexec /a`, and after extraction.
6. Dispose the held handle only after extraction has completed or failed.
7. Discover extracted `BrxMgd.dll` with an explicit stack walk that rejects every reparse-backed entry before descent.

The preliminary `Test-PinnedMsi` remains useful for cache/download selection. It is not trusted as final generation identity; `Open-PinnedMsiReadLock` performs the final pinned re-hash while the non-replaceable handle is already held.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-compile-reference-acquisition-stability.py
python scripts/preflight-v25-protected-main-release.py
```

The first guard includes mutation locks for the held-file sharing mode, streaming digest, post-signature/post-metadata/post-extraction state assertions, and reparse traversal rejection. The protected-main guard ensures Shared CI cannot regress to an acquisition boundary that validates a path and later consumes an unbound generation.

## Runtime boundary

This package is repository-safe build/release infrastructure. It does not claim a licensed BricsCAD runtime PASS or `LOCAL_PASS`.
