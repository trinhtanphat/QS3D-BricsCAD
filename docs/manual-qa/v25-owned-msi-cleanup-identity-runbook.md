# V25 owned MSI cleanup identity qualification

Issue: #6103  
Lane: C05 CI / Installer / Release Safety

## Security invariant

A failed V25 canonical MSI publication must never reopen and delete the canonical pathname merely because the current attempt once created it. The creator generation must remain owned by a live handle with a **cancelable explicit delete disposition** until the bytes on that same handle have been verified against the admitted staging generation. Publication becomes durable only after that explicit disposition is cleared on the owned handle.

`FileOptions.DeleteOnClose` / `FILE_FLAG_DELETE_ON_CLOSE` is intentionally forbidden here. Microsoft documents that `FILE_DISPOSITION_INFO.DeleteFile = FALSE` has no effect on a handle opened with `FILE_FLAG_DELETE_ON_CLOSE`, so a create-time delete-on-close flag cannot be used as a commit/disarm transaction.

## Required implementation shape

1. Create the canonical MSI fresh-only through `CreateFileW(..., CREATE_NEW, ...)` with read, write **and DELETE** access, but without `FILE_FLAG_DELETE_ON_CLOSE`.
2. Immediately arm deletion on that exact creator handle with `SetFileInformationByHandle(FileDispositionInfo, DeleteFile=true)` before copying fallible payload bytes.
3. Copy the already-admitted staging stream, flush durably, rewind the same publication handle, compute SHA-256 on that handle, and compare it with both the pinned expected digest and held staging digest.
4. Keep the explicit delete disposition armed throughout every fallible pre-commit operation.
5. Clear the explicit disposition with `SetFileInformationByHandle(FileDispositionInfo, DeleteFile=false)` only after same-handle verification succeeds.
6. Clear `$publishedByThisAttempt` only after disposition clear succeeds, then release the publication handle.
7. On any failure before commit, dispose the still-owned handle and allow the armed disposition to remove that exact generation. Do not call `Get-OrdinaryFileOrNull` or `File.Delete` on the canonical pathname for rollback.
8. After commit, ordinary pinned-MSI read admission, Authenticode checks and MSI product identity checks remain unchanged and fail closed.

## Qualification cases

- Inject a failure after `CREATE_NEW` and explicit delete arm but before copy completion: closing the creator handle removes only that generation.
- Inject failure while arming deletion: abort before copying bytes; no fallback may assume cleanup ownership unless the native state is proven.
- Inject a failure after durable flush but before same-handle digest verification: explicit deletion remains armed and removes the owned generation.
- Inject a digest mismatch: publication is not committed and the owned generation is removed on handle close.
- Inject failure in the disposition-clear operation: ownership flag remains set; handle close still removes the generation.
- Successful publication: same-handle SHA-256 matches staging and expected digest, native disposition clear succeeds, ownership flag clears, handle closes, and subsequent pinned read admission succeeds.
- Replacement-path adversary: there is no close→reopen→pathname-delete rollback window; cleanup acts only on the already-open creator handle.
- Confirm `FileOptions.DeleteOnClose` is rejected by the focused guard so static CI cannot regress to an un-disarmable create-time delete flag.
- Confirm `preflight-v25-owned-msi-cleanup-identity.py` is auto-discovered and fails if native DELETE access, explicit arm, same-handle hash verification, or explicit commit is removed.

## Non-regression

Do not weaken reparse/path traversal checks, SHA-256 pinning, Authenticode signer validation, MSI product/version checks, extraction fencing, or fallback-source failure behavior to satisfy this qualification.