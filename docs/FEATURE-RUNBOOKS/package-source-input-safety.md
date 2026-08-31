# V25/V26 package source-input filesystem safety

Status: `SOURCE_READY / PENDING_BRANCH_CI`

Lane-Key: `issue-4386`

Baseline source audited: `main@f6a68f8618999305c46da1cc49b925576ccdd5e6`.

## Defect boundary

The package builders already fail closed around their `dist` output trees, but current-main input admission relied largely on `Test-Path` before reading, copying, transforming, or recursively scanning build/repository inputs. On a contaminated Windows/self-hosted workspace, a reparse-backed input could therefore redirect package bytes outside the intended repository/source boundary while the final package destination itself remained safe.

This lane binds input provenance only. It does not redesign package identity, signing, release publication, or runtime acceptance.

## Required ordering

For both V25 and V26, packaging now performs the following before any packaging output mutation:

1. canonicalize and require the repository root to be an ordinary non-reparse directory;
2. require each source input to remain contained by that repository root;
3. reject every existing ancestor that is non-directory or reparse-backed;
4. require input directories to be ordinary non-reparse directories and input files to be ordinary non-reparse `FileInfo` leaves;
5. bind the build-output root, project files, release/generator inputs, synthetic sample root/files, and command-source roots/files;
6. traverse command source explicitly without following a reparse-backed directory or accepting a non-regular filesystem entry;
7. only after those admissions may output staging/copy/hash/archive work proceed.

V26 additionally binds the V25-to-V26 script transformer and each V25 source script before transformation. V25 binds package launchers and release scripts before copying them into the package.

## Preserved contracts

The change preserves existing strict product-version/SemVer checks, V25/V26 identity separation, required/forbidden payload rules, command discovery semantics, package metadata, SHA256 manifest generation, safe `dist` traversal, ZIP generation, signature observations, and release/runtime boundaries.

The deterministic auto-discovered `scripts/preflight-package-source-input-safety.py` locks the cross-major helper/call-site contract, admission-before-output ordering, non-reparse/regular-file requirements, and mutation probes that remove the source binder or restore unsafe recursive source scanning.

## Runtime boundary

This is repository-safe release/package tooling. Hosted/static CI may prove source and build integrity, but it does not prove production signing, licensed BricsCAD V25/V26 runtime, clean-machine installation, or `LOCAL_PASS`.

## Merge boundary

The canonical branch must pass exact-head branch CI, reconcile non-force with current `main`, then pass fresh protected current-candidate `preflight` and `core`. Merge uses expected-head protection only after freshness, collision, and mergeability gates pass, followed by exact protected-main verification.
