# V25 package verifier input-generation stability

Lane-Key: `issue-4439`

## Purpose

Harden the repository-safe V25 package-integrity verifier so the checksum record and ZIP archive are consumed from the exact ordinary, non-reparse file generations that were admitted and fingerprinted. This package does not sign artifacts, execute licensed BricsCAD, or claim `LOCAL_PASS`.

## Defect boundary

The previous verifier resolved the checksum and ZIP by path, read the checksum with path-reopening `Get-Content`, hashed the ZIP with path-reopening `Get-FileHash`, and later reopened the ZIP with `ZipFile.OpenRead`. A same-path replacement or reparse transition could therefore detach the accepted checksum/hash evidence from the bytes actually parsed as the archive.

## Contract

- Reject ZIP/checksum files with any reparse-backed path component.
- Capture file state with streaming SHA-256, byte length, and UTC last-write ticks; resolve and fingerprint twice during admission.
- Recompute the full fingerprint whenever an admitted state is asserted.
- Read the external checksum through a bounded strict-UTF8 stream opened with write/delete sharing denied, and revalidate afterward.
- Open the ZIP through a generation-bound file stream, fingerprint that exact opened handle, rewind it, and construct `ZipArchive` over the same handle.
- Revalidate ZIP/checksum states after archive consumption.
- Preserve safe archive-path normalization, case-collision rejection, required-entry checks, exact `SHA256SUMS.txt` coverage, and per-entry stream SHA-256 verification.

## Deterministic validation

```powershell
python scripts/preflight-v25-package-verifier-input-stability.py
pwsh -NoProfile -File scripts/preflight-v25-package-integrity.ps1
```

The auto-discovered preflight includes mutation locks for second-fingerprint capture, fresh assertion hashing, opened-handle hashing, bounded checksum reading, stable archive-stream consumption, and final state rechecks.

## Runtime boundary

All acceptance in this package is repository-safe source/PowerShell/package-integrity validation. Licensed BricsCAD runtime evidence remains separate and must not be inferred from green hosted CI.
