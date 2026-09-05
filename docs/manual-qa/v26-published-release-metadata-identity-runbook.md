# V26 published release metadata transaction identity

## Purpose

The V26 publisher must not publish a release whose mutable GitHub release metadata drifted after this workflow admitted its transaction-owned draft. Release id and uploaded-asset identity alone are not sufficient transaction identity: a concurrent actor can edit tag/target/prerelease/name/body fields while the draft remains otherwise usable.

## Admission contract

1. Draft creation/reconciliation still validates the exact release id/repository URL, `draft=true`, release tag, qualified `GITHUB_SHA`, expected release name, prerelease state, and the run-unique `QS3D-DRAFT-CREATE-V26:<run>:<attempt>:<nonce>` transaction marker.
2. Only after that admission succeeds, the publisher snapshots the **exact server-returned body** as `$expectedPublishedBody`. This intentionally preserves any server-generated release-note text together with the transaction marker rather than reconstructing body text later.
3. Asset upload, held-generation hashing, remote download verification, exact lightweight-tag SHA verification, protected-main ancestry/drift classification, and second-main confirmation remain unchanged.

## Publication commit point

The final PATCH is one atomic request containing the complete mutable identity already qualified by the workflow:

- `draft = false`
- `tag_name = $env:RELEASE_TAG`
- `target_commitish = $env:GITHUB_SHA`
- `prerelease = $isPrerelease`
- `name = $expectedReleaseName`
- `body = $expectedPublishedBody`

This closes the metadata TOCTOU window at the publication commit point. A concurrent draft edit before the PATCH is overwritten by the exact qualified metadata instead of being made public and only noticed afterward.

The shared `Assert-PublishedReleaseMatchesVerifiedTransaction` then re-validates release id/repository, `draft=false`, exact tag, target SHA, prerelease state, exact ordinal name/body, remote lightweight-tag SHA, and exact asset identity/size. Both the direct PATCH response and the ambiguous-PATCH acknowledgement GET use the same assertion and expectations.

## Failure semantics

If the final request fails before authoritative publication, existing rollback semantics remain fail-closed. If the PATCH acknowledgement is ambiguous and the release is already public, reconciliation succeeds only when the authoritative release snapshot matches the exact qualified metadata and all existing identity/assets invariants. A public release with mismatched qualified metadata is not accepted as committed and requires manual cleanup, consistent with existing ambiguous-publication safety behavior.

## Adversarial qualification

The auto-discovered `scripts/preflight-v26-published-release-metadata-identity.py` must fail when any of these regressions is introduced:

1. remove the exact published release-name or body comparison;
2. remove/comment `tag_name = $env:RELEASE_TAG` from the final atomic PATCH;
3. remove/comment `target_commitish = $env:GITHUB_SHA` from the final atomic PATCH;
4. remove/comment `prerelease = $isPrerelease` from the final atomic PATCH;
5. remove/comment exact name or admitted-body fields from the final atomic PATCH;
6. omit expected name/body wiring from either the direct publish assertion or acknowledgement reconciliation assertion;
7. snapshot the body before validating the run-unique transaction marker;
8. remove the initial transaction-marker admission check.

Run the auto-discovered preflight suite and full Shared CI on the exact candidate head. Do not weaken the assertion, skip source guards, or accept stale GREEN from a different SHA.
