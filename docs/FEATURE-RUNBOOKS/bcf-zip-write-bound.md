# BCF ZIP write bound

## Scope

This runbook covers deterministic Core resource/correctness behavior for `BcfZipPackage.Write`. It requires no licensed BricsCAD runtime qualification.

## Defect

The BCF writer historically created the whole no-compression ZIP in a `MemoryStream`, closed the archive, materialized the final byte array, and only then compared its length with `MaxArchiveBytes` (16 MiB). Valid semantic input could therefore make the writer grow the backing stream beyond its public package ceiling before rejection.

## Required contract

- `MaxArchiveBytes` remains the canonical 16 MiB package ceiling for read and write.
- ZIP creation writes through a bounded seekable stream that rejects any write or resize whose resulting backing-stream length would exceed that ceiling.
- Admission happens before the growth operation reaches the underlying `MemoryStream`.
- `ZipArchive` still uses Create mode and deterministic no-compression entry emission/timestamps.
- Existing `MaxEntryBytes`, entry-count, strict UTF-8, safe-path, BCF GUID/reference, XML and read-side uncompressed limits remain unchanged.
- The final byte-array length check remains a defense-in-depth invariant, not the first resource boundary.

## Regression

`BcfZipWriteBoundSmoke` uses the maximum admitted topic cardinality with individually valid free-text fields to cross the package ceiling and requires the canonical archive-bound failure. A small canonical package is written, read and checked for unchanged topic identity. `scripts/preflight-bcf-zip-write-bound.py` pins bounded-stream construction before `ZipArchive` creation and before `MemoryStream.ToArray()`.

## Validation and merge

Run the focused preflight, deterministic Core smoke suite and Core Release build through Shared CI. Reconcile current protected main non-force before merge and require exact-head protected `preflight` and `core` SUCCESS. Merge only through the protected PR path with expected-head protection, then verify exact protected main contains the candidate.

Runtime classification: `NOT_APPLICABLE`.