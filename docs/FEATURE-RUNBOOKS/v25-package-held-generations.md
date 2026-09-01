# V25 package held file generations

Lane-Key: `issue-4592`

## Source defect

The V25 package path admitted repository files as ordinary non-reparse inputs, then reopened those same pathnames for XML/text reads and `Copy-Item`. A same-path replacement between admission and consumption could therefore make packaging consume a different generation from the one that passed the source-safety check. V26 already rejects that gap.

## Contract

Every package source that crosses from repository input into semantic package construction is consumed from a held read-only generation. `Open-HeldPackageInput` admits one ordinary non-reparse file, opens it with `FileShare.Read`, and retains path, length, UTC-write-time and stream state. Because write/delete sharing is not granted, a same-path replacement cannot silently swap the admitted generation while it is consumed.

Project and command-source text is read through a bounded strict-UTF8 stream. Build artifacts, release scripts, launchers and synthetic samples are copied from the held stream into the package destination. Pre/post path-binding assertions fail closed if the pathname no longer resolves to the admitted state.

Existing output containment, package traversal, forbidden BricsCAD assembly checks, hashes, ZIP construction, version metadata and release policy are unchanged.

## Deterministic regression

`scripts/preflight-v25-package-held-generations.py` requires the held-generation primitives and all V25 consumer call sites, rejects legacy pathname `Get-Content`/`Copy-Item` shortcuts, and contains mutation probes that prove replacement of the lock/copy/text boundaries is detected. `scripts/preflight-package-source-input-safety.py` is the cross-version guard and must enforce the same generation-boundary baseline for V25 and V26.

## Validation boundary

This is repository-safe package/source integrity. No licensed BricsCAD runtime evidence is produced or claimed, no signing or publishing is performed, and hosted CI is not `LOCAL_PASS`.
