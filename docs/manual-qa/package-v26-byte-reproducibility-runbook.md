# V26 byte-reproducible package qualification

Lane: C05 CI / Release / Deterministic Build Validation

This runbook qualifies `scripts/package-v26.ps1` for deterministic release identity. It does not authorize release publication by itself.

## Contract

For one exact admitted Git source commit and the same admitted V26 build inputs, repeated packaging must produce the same package metadata, command list, checksum manifest, ZIP entry order, ZIP entry timestamps, ZIP payload bytes, and final ZIP SHA-256.

The packager must:

- bind provenance to one exact 40-hex Git `HEAD` and derive package time from that commit timestamp rather than wall clock time;
- reject source commit timestamps outside the representable ZIP timestamp range rather than substituting a runtime-dependent value;
- use ordinal, culture-independent ordering for `COMMANDS.txt`, `SHA256SUMS.txt`, and ZIP entries;
- normalize ZIP entry names to forward-slash package-relative paths and reject empty, rooted, traversal, drive/ADS-colon, backslash, duplicate, reparse-backed, or escaped entries;
- use explicit `ZipArchive` entries with `NoCompression` and the source-bound timestamp so runtime deflate and filesystem timestamp behavior cannot change release bytes;
- copy each ZIP payload from its held admitted generation and re-check pathname/file identity around the copy;
- write through a private temporary file and only move a fully closed/flushed archive into the final package path;
- preserve existing V26 managed identity, Authenticode, forbidden-runtime-assembly, sample provenance, and checksum gates.

## Automated guard

Run from repository root:

```text
python scripts/preflight-package-v26-byte-reproducibility.py
```

The guard is auto-discovered by `scripts/preflight-all.py`. Its mutation probes must fail when any required source-binding, ordinal ordering, canonical entry-name, deterministic compression/timestamp, or deterministic-writer call marker is removed.

The normal shared CI must also pass the tracked PowerShell syntax check, Reservation-v2, generic source guard, all auto-discovered feature guards, and required build/smoke jobs on the exact candidate head.

## Windows qualification

On the supported Windows packaging host with the required V26 build output present:

1. Record the exact candidate commit with `git rev-parse HEAD`.
2. Run `scripts/package-v26.ps1` twice without changing the source commit or admitted build inputs. Remove only generated `dist/` output between runs; do not rebuild from a different source generation.
3. Record SHA-256 of `dist/QS3D-BricsCAD-V26.zip` from both runs. The hashes must be identical.
4. Open both ZIPs and compare the ordered entry-name list, each entry timestamp, each uncompressed payload SHA-256, `PACKAGE-METADATA.json`, `COMMANDS.txt`, and `SHA256SUMS.txt`. They must match byte-for-byte.
5. Confirm `PACKAGE-METADATA.json.gitCommit` equals the exact candidate commit and `generatedUtc` equals that commit's `%cI` timestamp normalized to UTC.
6. Confirm `SHA256SUMS.txt` covers every staged package file except itself and is sorted ordinal by canonical relative path.
7. Confirm no BricsCAD proprietary runtime assembly (`BrxMgd.dll`, `TD_Mgd.dll`, `TD_MgdBrep.dll`) is present.
8. Confirm generated V26 installer/updater scripts contain no V25 token and existing signature/managed-identity checks remain active.

Any mismatch is a release-admission failure. Do not publish, do not update expected hashes, and do not weaken a guard to accept nondeterministic output.

## Reconciliation and release admission

Before merge, refresh protected `main`, reconcile without force-push, ensure the direct `main...candidate` diff is limited to the reserved C05 paths, and obtain fresh required CI GREEN on the exact reconciled head. A green run from an ancestor is not reusable.

After merge, verify the signed merge commit is the protected-main head and that the merge tree contains the exact qualified deterministic-packaging implementation. Normal release signing/checksum/publication workflows remain authoritative; this carrier must not publish a release itself.
