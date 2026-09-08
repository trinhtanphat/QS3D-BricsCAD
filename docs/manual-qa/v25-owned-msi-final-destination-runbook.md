# V25 owned MSI final-destination qualification

Issue: #6137
Lane: C05 installer / release safety

## Threat model

The canonical MSI pathname can pass a pre-publication reparse/path check and then resolve differently when `CreateFileW` executes. Publication must therefore trust the fresh creator handle, not the pathname check alone. A parent rename/reparse change after the first proof is also relevant: the handle-owned generation must still resolve to the intended canonical destination immediately before publication commit.

## Required invariant

1. Preserve fresh-only `CREATE_NEW`, read/write/DELETE access, explicit `FileDispositionInfo` delete arming and same-handle SHA-256 verification.
2. Immediately after the creator handle is opened and delete disposition is armed, query `GetFinalPathNameByHandleW` on that exact handle.
3. Normalize Windows extended DOS (`\\?\C:\...`) and extended UNC (`\\?\UNC\server\share\...`) forms without weakening canonical absolute-path checks.
4. Fail closed unless the handle-resolved final path equals canonical `$msi` before any staging payload bytes are copied.
5. After copy/flush/hash verification, query the same creator handle again and fail closed on destination drift before clearing delete disposition.
6. On any failure before commit, disposing the still-delete-armed owned handle remains the rollback primitive. Do not reopen/delete the pathname.
7. Only after the pre-commit final-path re-proof succeeds may `DeleteFile=false` commit the exact owned generation.

## Deterministic source qualification

Run:

```text
python scripts/preflight-v25-owned-msi-final-destination.py
python scripts/preflight-all.py
```

The focused guard mutation-locks the native declaration/invocation, creator-handle use, expected/final-path comparison, fail-closed mismatch branches and both proof orderings. Moving the first proof after `CopyTo` or the second proof after delete-disposition commit must fail.

## Windows qualification

On a disposable Windows workspace, use only a temporary cache directory and pinned test payload. Exercise ordinary DOS and UNC-style final-path normalization where the host permits it. Verify malformed/unexpected final paths fail before payload copy, and simulated path/directory movement before commit cannot clear delete disposition. Do not publish a release artifact from this qualification.

Hosted CI/source checks are REMOTE_SAFE evidence only. They do not constitute licensed BricsCAD runtime qualification.
