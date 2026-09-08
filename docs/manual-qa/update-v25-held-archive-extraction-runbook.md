# V25 updater held-archive extraction qualification

Issue: #6025
Lane: C05 CI / Installer / Update Safety
Qualification: REMOTE_SAFE source/static validation only

## Safety contract

The updater must bind the downloaded V25 package ZIP to one open generation from manifest SHA-256 admission through archive classification and extraction. The held stream is opened read-only with write/delete sharing denied, hashed directly, rewound, and supplied to `ZipArchive`; extraction reads the admitted `ZipArchiveEntry` streams rather than reopening the ZIP pathname.

All archive entries are classified before extraction begins. Admission rejects rooted paths, backslashes, colons/ADS syntax, NULs, empty or dot traversal segments, Windows-invalid/trailing-dot-or-space/device-name segments, duplicate or case-insensitive destination aliases, file/ancestor conflicts, excessive entry count, and excessive expanded size. Extraction creates new destination leaves and does not overwrite an existing target.

Directory mutation is fail-closed and component-by-component. Before creating the extraction root, the updater validates the nearest existing ancestor chain for reparse points; after root creation it validates the root again. For every archive directory or file parent, `Ensure-SafeExtractionDirectory` validates the current existing parent before creating one child component and revalidates the resulting child immediately afterward. File parents are validated again immediately before the `CreateNew` leaf open. Directory archive records therefore cannot bypass reparse validation with a direct `CreateDirectory(record.Target)`/`continue`, and file records cannot create a parent hierarchy before validating it.

The existing updater controls remain required and unchanged: HTTPS and allowed-host checks, bounded manifest/package downloads, official GitHub tag/asset binding, manifest package SHA-256, expected Authenticode signer thumbprint, assembly/product SemVer anti-downgrade and replay checks, per-user update mutex, package-root required payload checks, inner `SHA256SUMS.txt` verification, signed executable payload checks, installed-state revalidation, and temporary-directory cleanup.

## Deterministic evidence

`scripts/preflight-update-v25-held-archive-extraction.py` is auto-discovered by `scripts/preflight-all.py`. It requires the same held stream to participate in digest and `ZipArchive` construction, denies `Expand-Archive` and ZIP-path `Get-FileHash` reopen primitives, requires duplicate/case-insensitive target protection and `CreateNew` extraction, rejects the historical mutation-before-validation directory/file-parent patterns, requires the explicit root-inclusive path-chain validator and safe directory helper, verifies validate/create/revalidate ordering for the extraction root and each child directory, requires repeated file-parent validation before leaf creation, verifies bounded parameters are wired at the call site, and mutation-probes the essential source markers.

Hosted Shared CI must pass exact-head preflight, the aggregate auto-discovered guard set, tracked PowerShell syntax, V25 package-integrity contract tests, deterministic core smoke tests, and locked-reference V25 compilation before merge.

## Truth boundary

REMOTE_SAFE does not claim a live installer/update execution against an installed BricsCAD instance, Authenticode signing with production keys, release publication, or LOCAL_PASS. It also does not claim a general NTFS handle-relative race-proof extraction primitive. The source hardening narrows pathname TOCTOU exposure by validating before and after each directory mutation and again immediately before file-leaf creation, while the held-ZIP handle separately proves package-generation continuity. Stronger kernel handle-relative directory traversal would require a different native implementation and separate qualification.
