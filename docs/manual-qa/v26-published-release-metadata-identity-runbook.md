# V26 published release metadata transaction identity

## Purpose

The V26 publisher must not publish a release whose mutable GitHub release metadata drifted after this workflow admitted its transaction-owned draft. Release id, tag, target SHA and uploaded assets are necessary but not sufficient transaction identity: a concurrent actor can edit the draft name or body while leaving those fields intact.

## Admission contract

1. Draft creation/reconciliation still validates the exact release id/repository URL, `draft=true`, release tag, qualified `GITHUB_SHA`, expected release name, prerelease state, and the run-unique `QS3D-DRAFT-CREATE-V26:<run>:<attempt>:<nonce>` transaction marker.
2. Only after that admission succeeds, the publisher snapshots the **exact server-returned body** as `$expectedPublishedBody`. This intentionally preserves any server-generated release-note text together with the transaction marker rather than reconstructing body text later.
3. Asset upload, held-generation hashing, remote download verification, exact lightweight-tag SHA verification, protected-main ancestry/drift classification, and second-main confirmation remain unchanged.

## Publication commit point

The final PATCH is one atomic request containing:

- `draft = false`
- `name = $expectedReleaseName`
- `body = $expectedPublishedBody`

This closes the metadata TOCTOU window at the publication commit point. A concurrent draft edit before the PATCH is overwritten by the exact qualified metadata instead of being made public and only noticed afterward.

The shared `Assert-PublishedReleaseMatchesVerifiedTransaction` then requires exact ordinal equality for the published name and admitted body in addition to the existing release id/repository/tag/target/prerelease/remote-tag/assets identity checks. Both the direct PATCH response and the ambiguous-PATCH acknowledgement GET use the same assertion and exact expected values.

## Failure semantics

If the final request fails before authoritative publication, existing rollback semantics remain fail-closed. If the PATCH acknowledgement is ambiguous and the release is already public, reconciliation succeeds only when the authoritative release snapshot matches the exact qualified metadata and all existing identity/assets invariants. A public release with a mismatched name/body is not accepted as committed and requires manual cleanup, consistent with existing ambiguous-publication safety behavior.

## Adversarial qualification

The auto-discovered `scripts/preflight-v26-published-release-metadata-identity.py` must fail when any of these regressions is introduced:

1. remove the exact published release-name comparison;
2. remove the exact published release-body comparison;
3. remove `name = $expectedReleaseName` from the final atomic PATCH;
4. remove `body = $expectedPublishedBody` from the final atomic PATCH;
5. omit expected body wiring from either the direct publish assertion or acknowledgement reconciliation assertion;
6. snapshot the body before validating the run-unique transaction marker;
7. remove the initial transaction-marker admission check.

Run the auto-discovered preflight suite and full Shared CI on the exact candidate head. Do not weaken the assertion, skip source guards, or accept stale GREEN from a different SHA.
