# V26 host-reference state bounded read

## Purpose

`assert-v26-host-reference-safety.ps1` treats the captured V26 host-reference state as build/release authority. Its verification read is therefore fail-closed: pathname safety is checked first, but the 64 KiB admission limit is enforced on the exact `FileStream` that will be decoded rather than on earlier `FileInfo` metadata.

## Required source contract

`Read-BoundedStrictUtf8` must:

1. reject redirected/non-ordinary pathname members through the existing safety helpers;
2. open the admitted path exactly once for read access;
3. check `stream.Length` against `MaxBytes` before constructing the decoder;
4. retain strict UTF-8 decoding (`throwOnInvalidBytes=true`);
5. never reopen/retry into a different filesystem generation after the bounded stream is admitted;
6. dispose the exact stream on both oversize rejection and normal/error completion.

A pre-open `FileInfo.Length` may be useful as non-authoritative metadata, but it must not substitute for the exact opened-stream bound.

## Deterministic validation

Run:

```text
python scripts/preflight-v26-host-reference-state-bounded-read.py
python scripts/preflight.py
python scripts/preflight-all.py
```

The focused guard is auto-discovered and pins ordering: open -> exact stream length admission -> strict decoder -> read. It also rejects a return to pathname-only size admission or multiple opens.

## Runtime boundary

This package is source/build-safety work and is `REMOTE_SAFE`. No licensed BricsCAD V26 runtime result is required or inferred. Existing V26 host-generation checks, package/release gates, signing requirements, and LOCAL_ONLY qualification remain separate contracts.
