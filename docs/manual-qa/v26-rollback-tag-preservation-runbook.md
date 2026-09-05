# V26 rollback tag-preservation qualification

## Safety invariant

A failed V26 publication transaction may delete only the exact draft release it owns and has revalidated. Rollback must never delete the release tag. The exact tag is preserved at the qualified workflow SHA so a subsequent publish attempt can safely reuse it.

This closes the remote TOCTOU window that existed when rollback first enumerated releases owning a tag and later issued a separate destructive tag-ref DELETE. A release created or attached to the tag between those remote operations could otherwise lose its tag even though the earlier ownership proof was already stale.

## Automated admission

`python scripts/preflight-v26-rollback-tag-preservation.py`

The auto-discovered guard requires:

- exhaustive release-owner checking before rollback completion;
- an exact remote tag SHA recheck against the workflow SHA;
- deletion of only the transaction-owned draft release;
- no tag-ref DELETE URI, no tag DELETE request, and no tag-delete reconciliation helper;
- explicit `TagDeleted = $false` results for both tag-ownership branches;
- preservation messaging that documents safe retry semantics.

The guard mutation-tests removal of draft deletion, release-owner exhaustion, exact-tag resolution, preservation intent, and the non-destructive result.

## Manual adversarial qualification

Use a disposable test repository and token with only the permissions required by the V26 release workflow. Never run this qualification against a production release tag.

1. Create a lightweight V26-format tag pointing to the exact candidate workflow SHA.
2. Create a draft release for that tag and record the release id.
3. Invoke the rollback script with the exact repository, release id, tag, workflow SHA, `TagCreatedByThisRun=$true`, and token.
4. Verify the draft release is gone and the tag still resolves to the exact original SHA.
5. Re-run the publisher's exact-tag preparation path and verify the existing exact tag is reused rather than recreated or moved.
6. Repeat with `TagCreatedByThisRun=$false`; verify the same tag-preservation result.
7. Adversarially move the tag before the final exact-tag check. Rollback must fail closed rather than claim restart safety.
8. Adversarially leave/create any release owning the tag during release-owner exhaustion. Rollback must fail closed and must not perform any tag deletion.

## Evidence to retain

Record the candidate commit SHA, Shared CI run id, preflight/core conclusions, disposable repository/tag, exact tag SHA before and after rollback, deleted draft release id, and publisher retry result. This runbook is REMOTE_SAFE release-infrastructure qualification; it does not claim licensed BricsCAD runtime `LOCAL_PASS`.
