# V25 updater held-archive extraction qualification

Issue: #6025
Lane: C05 CI / Installer / Update Safety
Qualification: REMOTE_SAFE source/static validation only

## Safety contract

The updater must bind the downloaded V25 package ZIP to one open generation from manifest SHA-256 admission through archive classification and extraction. The held stream is opened read-only with write/delete sharing denied, hashed directly, rewound, and supplied to `ZipArchive`; extraction reads the admitted `ZipArchiveEntry` streams rather than reopening the ZIP pathname.

All archive entries are classified before extraction begins. Admission rejects rooted paths, backslashes, colons/ADS syntax, NULs, empty or dot traversal segments, Windows-invalid/trailing-dot-or-space/device-name segments, duplicate or case-insensitive destination aliases, file/ancestor conflicts, excessive entry count, and excessive expanded size. Extraction creates new destination leaves and does not overwrite an existing target. The extraction root itself and every traversed destination parent are checked fail-closed for reparse points immediately before file creation so a junction/symlink cannot turn a lexically in-root destination into a write outside the temporary extraction tree.

The existing updater controls remain required and unchanged: HTTPS and allowed-host checks, bounded manifest/package downloads, official GitHub tag/asset binding, manifest package SHA-256, expected Authenticode signer thumbprint, assembly/product SemVer anti-downgrade and replay checks, per-user update mutex, package-root required payload checks, inner `SHA256SUMS.txt` verification, signed executable payload checks, installed-state revalidation, and temporary-directory cleanup.

## Deterministic evidence

`scripts/preflight-update-v25-held-archive-extraction.py` is auto-discovered by `scripts/preflight-all.py`. It requires the same held stream to participate in digest and `ZipArchive` construction, denies `Expand-Archive` and ZIP-path `Get-FileHash` reopen primitives, requires duplicate/case-insensitive target protection and `CreateNew` extraction, requires root-inclusive reparse-point validation, verifies bounded parameters are wired at the call site, and mutation-probes the essential source markers.

Hosted Shared CI must pass exact-head preflight, the aggregate auto-discovered guard set, tracked PowerShell syntax, V25 package-integrity contract tests, deterministic core smoke tests, and locked-reference V25 compilation before merge.

## Truth boundary

REMOTE_SAFE does not claim a live installer/update execution against an installed BricsCAD instance, Authenticode signing with production keys, release publication, or LOCAL_PASS. It also does not claim a general NTFS handle-relative race-proof extraction primitive beyond the explicit held-ZIP generation and immediate root/parent reparse checks described above. Those runtime/release qualifications remain separate.