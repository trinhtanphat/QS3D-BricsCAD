# V26 release checksum held-generation contract

Issue: #4667  
Lane-Key: `issue-4667`  
Runtime: `NOT_APPLICABLE` — repository-safe checksum/source integrity only.

## Problem

`write-v26-package-checksum.ps1` validates the V26 ZIP pathname before hashing. A read-only `FileStream` prevents later replacement once it is open, but pathname validation and `File.Open` are separate operations. Without a post-open rebound check, the ZIP path can be replaced in the admission/open gap and the checksum can be computed over a generation that was never the one admitted by the earlier ordinary/non-reparse validation.

## Required generation binding

Before opening the hash stream, snapshot the admitted ZIP's canonical `FullName`, `Length`, and `LastWriteTimeUtc.Ticks`. Open that exact canonical path with read access and `[IO.FileShare]::Read`. While the stream is held and before SHA-256 computation:

1. re-run ordinary/non-reparse validation on the canonical pathname;
2. require the rebound `FullName` to match the admitted canonical path;
3. require admitted length, rebound length, and held stream length to agree;
4. require rebound last-write ticks to equal the admitted snapshot;
5. fail closed before hashing if any binding check differs.

The same held stream must then be passed directly to `SHA256.ComputeHash`; pathname reopen hashing is forbidden.

## Preserved publication safety

This package does not change the existing checksum publication model. The helper must continue to enforce canonical V26 ZIP/checksum filenames, non-reparse directory ancestry, bounded snapshot of any pre-existing checksum, nonce staging/backup names, atomic `File.Replace`/`File.Move`, mutation-window rollback, published-byte verification, safe backup cleanup, and zero staging/backup residue.

## Deterministic validation

Run:

```text
python scripts/preflight-v26-release-checksum-safety.py
```

The guard pins the admission snapshot, post-open rebound, length/write-time checks, rebound-before-hash ordering, held-stream SHA-256 use, existing staged publication/rollback guarantees, and workflow routing through the shared helper. Mutation probes must fail when the rebound check is removed, weakened, or moved after hashing.

Hosted/static validation is authoritative for this bounded source-integrity package. It does not sign or publish a release and does not establish licensed BricsCAD `LOCAL_PASS`.
