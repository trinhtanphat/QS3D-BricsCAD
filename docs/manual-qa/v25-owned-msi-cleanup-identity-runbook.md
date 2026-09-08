# V25 owned MSI cleanup identity qualification

Issue: #6103  
Lane: C05 CI / Installer / Release Safety

## Security invariant

A failed V25 canonical MSI publication must never reopen and delete the canonical pathname merely because the current attempt once created it. The creator generation must remain owned by a live handle with delete-on-close semantics until the bytes on that same handle have been verified against the admitted staging generation. Publication becomes durable only after delete disposition is explicitly cleared on that owned handle.

## Required implementation shape

1. Create the canonical MSI with `CreateNew`, read/write access, exclusive sharing and `FileOptions.DeleteOnClose`.
2. Copy the already-admitted staging stream, flush durably, rewind the same publication handle, compute SHA-256 on that handle, and compare it with both the pinned expected digest and the held staging digest.
3. Keep delete-on-close armed throughout every fallible pre-commit operation.
4. Clear delete disposition through `SetFileInformationByHandle(FileDispositionInfo)` only after same-handle verification succeeds.
5. Clear `$publishedByThisAttempt` only after native disposition clear succeeds, then release the publication handle.
6. On any failure before commit, dispose the still-owned handle and allow delete-on-close to remove that exact generation. Do not call `Get-OrdinaryFileOrNull` or `File.Delete` on the canonical pathname for rollback.
7. After commit, ordinary pinned-MSI read admission, Authenticode checks and MSI product identity checks remain unchanged and fail closed.

## Qualification cases

- Inject a failure after `CreateNew` but before copy completion: closing the creator handle removes only that generation.
- Inject a failure after durable flush but before same-handle digest verification: delete-on-close remains armed and removes the owned generation.
- Inject a digest mismatch: publication is not committed and the owned generation is removed on handle close.
- Inject failure in the native disposition-clear operation: ownership flag remains set; handle close still removes the generation.
- Successful publication: same-handle SHA-256 matches staging and expected digest, native disposition clear succeeds, ownership flag clears, handle closes, and subsequent pinned read admission succeeds.
- Replacement-path adversary: there is no close→reopen→pathname-delete rollback window; cleanup acts only on the already-open creator handle.
- Confirm the focused `preflight-v25-owned-msi-cleanup-identity.py` is auto-discovered by aggregate feature guards and fails if delete-on-close, same-handle hash verification, or explicit commit is removed.

## Non-regression

Do not weaken reparse/path traversal checks, SHA-256 pinning, Authenticode signer validation, MSI product/version checks, extraction fencing, or fallback-source failure behavior to satisfy this qualification.
