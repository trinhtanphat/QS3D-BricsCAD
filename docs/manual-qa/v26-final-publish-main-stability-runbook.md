# V26 final publish protected-main stability qualification

Issue: #6093  
Lane: C05 CI / Release / Release Safety

## Contract

The V26 publisher is a best-effort fail-closed transaction over separate GitHub branch and Release APIs. It does **not** claim an atomic branch/release compare-and-swap primitive.

Immediately before final publication the existing publisher admission must still prove that the workflow SHA is an ancestor of protected `main`, that no release-relevant path has advanced, and that the API/fetched `main` identities agree. After the `draft=false` PATCH returns, the publisher must repeat protected-main admission before accepting publication as successful.

If the post-PATCH admission fails, the workflow records publication safety as invalidated and must not reinterpret the already-published release as a successful ambiguous acknowledgement. The run must fail closed and require manual release review; an already-published release must not be destructively rolled back by the draft rollback path.

## Qualification scenarios

1. **Stable main:** final pre-PATCH admission passes, PATCH publishes the exact verified release, post-PATCH admission passes, release identity/assets are rechecked, run succeeds.
2. **Release-relevant main advances before PATCH:** existing final admission fails and the release remains a draft.
3. **Release-relevant main advances after PATCH but before post-check:** post-PATCH admission fails, `publicationSafetyInvalidated` is set, reconciliation observes `draft=false` but refuses ambiguous-ack success and requires manual review.
4. **PATCH acknowledgement is transport-ambiguous while protected main remains safe:** reconciliation may accept the already-published release only after exact release/tag/asset identity checks and only when publication safety was not invalidated.
5. **Release published by another actor before this run's PATCH:** reconciliation remains fail-closed because `publishPatchAttempted` is false.
6. **Non-release-only main advancement:** allowed only under the existing release-relevant path classification; provenance remains pinned to the qualified `GITHUB_SHA`.

## Required automated evidence

- `scripts/preflight-v26-final-publish-main-stability.py` is auto-discovered by aggregate feature guards.
- PowerShell syntax validation passes for `scripts/publish-v26-release.ps1`.
- Fresh exact-head Shared CI passes all required preflight/core gates.
- Mutation probes fail when the post-PATCH check, safety-invalidated assignment, or acknowledgement safety guard is removed.

## Release operator note

A post-PATCH safety failure means GitHub may already expose the release publicly. Treat the workflow failure as a release-safety incident requiring operator review of protected-main drift and published artifact provenance. Do not automatically delete or recreate the release/tag unless the repository's explicit recovery procedure proves ownership and identity.
