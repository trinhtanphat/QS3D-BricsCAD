# V25 final publish protected-main stability qualification

## Scope
This qualification covers the final commercial-release boundary in `.github/workflows/release-v25.yml`, specifically the non-atomic gap between the last protected-main admission and the GitHub Release `draft=false` PATCH.

## Required invariants
1. The workflow SHA remains the exact qualified source identity and an ancestor of protected `main` before publication.
2. Release-relevant protected-main drift before the PATCH remains fail-closed.
3. Immediately after the PATCH, the workflow re-reads protected `main` and compares it with the exact final-main identity admitted immediately before publication.
4. A post-PATCH main change marks publication safety invalidated before any published-release identity is accepted.
5. If the PATCH acknowledgement is ambiguous and the release is observed published, acknowledgement recovery must reject a known publication-safety invalidation before treating publication as committed.
6. Once publication may already be public, the workflow must not attempt destructive rollback solely to hide the race; fail and require manual release review.

## Qualification scenarios
- Stable main: final admission, PATCH, post-PATCH re-read and release identity verification all succeed.
- Main advances through a release-relevant path before PATCH: publication is refused before mutation.
- Main advances after final admission but before/while PATCH completes: post-PATCH comparison invalidates publication safety and the run fails for manual review.
- Main advances only through non-release paths before final admission: existing source-provenance semantics remain unchanged.
- Transport/acknowledgement ambiguity with unchanged main: a uniquely matching published transaction may be reconciled only after full release identity verification.
- Transport/acknowledgement ambiguity plus known post-PATCH main invalidation: reconciliation must fail closed and must never report committed success.

## CI admission
The auto-discovered guard `scripts/preflight-v25-final-publish-main-stability.py` must pass on the exact candidate SHA, along with the repository's required preflight/core checks. Re-run fresh checks after any reconciliation with protected `main`.
