# V25 compile-reference acquisition stability

## Goal

The V25 cloud/shared-CI reference-acquisition path must never trust or publish an installer merely because a pathname happened to name matching bytes at one instant. Cache admission, downloaded-candidate admission, canonical cache publication, and later trust consumption must all remain bound to exact ordinary-file generations.

## Contract

`scripts/acquire-v25-compile-references.ps1` preserves the pinned URL/digest, Bricsys signer, ProductVersion/ProductName, bounded extraction and reparse-safe reference discovery requirements, with two explicit generation boundaries.

### Pre-admission and cache publication

1. Existing cached MSI bytes are re-admitted through `Open-PinnedMsiReadLock`; SHA-256 is streamed through the held `FileShare.Read` handle and canonical path, length and UTC last-write ticks are rebound before the cache hit is accepted.
2. No acquisition path may use pathname `Get-FileHash`.
3. Every remote candidate downloads to a unique sibling `.qs3d-v25-msi-<guid>.tmp` staging file under the already validated cache directory. Remote bytes are never downloaded directly to the canonical MSI pathname.
4. The staged ordinary file must pass the same held-generation digest admission before publication.
5. Immediately before publication, the canonical destination ancestry/final entry is rechecked for reparse/non-ordinary state. An existing ordinary stale destination may be removed, but a reparse-backed or directory destination fails closed.
6. Publication uses same-filesystem `File.Move` from the admitted staging generation to the canonical path. A racing destination creation causes the move to fail rather than overwrite unknown bytes.
7. The newly published canonical generation is immediately held-verified again. Staging cleanup runs in `finally`, so failed downloads or rejected candidates do not leave partial staging artifacts.

### Final trusted consumption

1. Re-admit the canonical MSI as an ordinary non-reparse file.
2. Open a read handle with `FileShare.Read`, intentionally denying write/delete/replace while later pathname-based Windows trust consumers inspect the file.
3. Stream the pinned SHA-256 through that held handle and bind canonical path, length and UTC last-write ticks to the stream.
4. Reassert that state before/after Authenticode, after Windows Installer metadata reads, immediately before `msiexec /a`, and after extraction.
5. Dispose the held handle only after extraction has completed or failed.
6. Discover extracted `BrxMgd.dll` with an explicit stack walk that rejects every reparse-backed entry before descent.

The final lock remains necessary even after atomic publication: cache restore or external workspace mutation can occur between acquisition phases. Every trust-consuming phase therefore rebinds the exact generation it relies on.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-compile-reference-acquisition-stability.py
python scripts/preflight-v25-protected-main-release.py
```

The focused guard mutation-tests held sharing/hash semantics, staging download identity, staging-before-publication ordering, atomic canonical publication, post-publication held verification, downstream trust-lock lifetime and reparse-safe extracted-tree discovery. It explicitly rejects both pathname `Get-FileHash` and direct `Invoke-WebRequest ... -OutFile $msi` regressions.

## Runtime boundary

This package is repository-safe Windows build/release infrastructure. Shared CI may exercise it while acquiring trusted V25 compile references; that does not constitute licensed BricsCAD runtime execution or `LOCAL_PASS`.
