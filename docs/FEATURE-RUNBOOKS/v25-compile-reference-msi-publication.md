# V25 compile-reference MSI publication generation

Issue: #5824  
Lane-Key: `issue-5824`  
Runtime: REMOTE_SAFE build/CI infrastructure.

## Invariant

A downloaded BricsCAD V25 MSI generation must remain admitted while its bytes are published into the canonical cache. Publication must not release the staged handle and later resolve the staging pathname again. The canonical destination is fresh-only for this publication attempt, is durably flushed, and is immediately re-admitted against the same expected SHA-256 before any trust/extraction use.

## Remote validation

Run:

```text
python scripts/preflight-v25-compile-reference-msi-publication.py
python scripts/preflight-all.py
```

The focused guard must reject premature staging-handle disposal, pathname `Move` publication, destructive removal of an unbound canonical destination, and omission of post-publication held re-admission.

## Preserved contracts

Keep the existing pinned mirror/public/fallback source policy, ordinary/non-reparse path checks, expected SHA-256 validation, Authenticode verification, fresh extraction root, process-tree timeout cleanup, extracted-tree reparse rejection, and final V25 assembly validation.

## Failure semantics

If the canonical MSI destination already exists but fails exact pinned-generation admission, do not delete or replace it during a download attempt. Fail the candidate and let the acquisition fail closed rather than introducing an unbound destructive replacement window. A partial fresh canonical destination created by the current attempt may be removed only after this attempt still owns that fresh generation and publication has failed.

No hosted result from this runbook is a licensed BricsCAD runtime PASS.
