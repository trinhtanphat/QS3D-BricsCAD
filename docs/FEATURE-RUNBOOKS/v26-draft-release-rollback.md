# V26 draft release rollback / restart safety

Lane-Key: `issue-4780`

## Purpose

The manual V26 release lane creates a GitHub draft release before uploading and remotely verifying package assets. GitHub may create the requested tag as part of that draft transaction. A post-create failure must not strand a draft/tag owned by the current run, because a same-tag retry would otherwise fail before publication and require manual cleanup.

This runbook defines the bounded automatic rollback contract. It does not authorize release dispatch, signing, or licensed BricsCAD runtime claims.

## Transaction ownership

Before creating the draft, `.github/workflows/release-v26.yml` resolves the exact remote tag and requires that `refs/tags/<release-tag>` is absent. Only after that proof does the workflow POST the new draft and mark `releaseCreatedByThisRun=true` after receiving a usable release id.

A pre-existing remote tag is therefore never owned by this transaction and is never eligible for automatic deletion. If draft creation does not return a usable release identity, cleanup is intentionally not guessed; the workflow surfaces the publication failure for manual inspection.

## Post-create publication envelope

Once the run owns the draft, the existing publication requirements remain unchanged inside one `try` envelope:

1. the remote tag must resolve exactly to the qualified `GITHUB_SHA`;
2. only V26 package assets are uploaded;
3. the draft asset set must exactly match the expected set;
4. remote size and held-generation SHA-256 verification must match local assets;
5. the remote tag is re-resolved and must still target `GITHUB_SHA` after asset verification;
6. only then may the draft be patched to `draft=false`.

Any exception after owned draft creation enters bounded rollback.

## Bounded rollback helper

`scripts/rollback-v26-draft-release.ps1` accepts the exact repository, release id, release tag, qualified workflow SHA, the pre-create tag-absence proof, and the workflow token. The helper fails closed unless all destructive preconditions remain true.

Before deleting the draft it verifies:

- repository is a valid `owner/name` identity;
- release id is positive and matches the fetched release object;
- fetched release URL is exactly the expected repository/release API URL;
- release is still a draft;
- release tag exactly matches the transaction tag;
- the tag-absence-before-create proof is true;
- the remote tag resolves unambiguously to the qualified workflow SHA.

After deleting the draft release, but before deleting the tag, it verifies that no release still owns the tag and re-resolves the tag to the same qualified workflow SHA. If the release became published, the tag moved, the tag is ambiguous, the release identity mismatches, or any other state is uncertain, the helper refuses tag deletion and the workflow reports an explicit manual-cleanup blocker.

The helper does not use force push or broad Git tag deletion. Tag deletion is a single GitHub REST deletion for the exact escaped `refs/tags/<release-tag>` identity after the checks above.

## Error reporting

The workflow preserves the original publication error. If bounded rollback succeeds, the step fails with `V26 publication failed after draft creation` and explicitly states that automatic rollback completed, making a same-tag retry safe.

If rollback itself fails closed, the step reports `Automatic V26 draft rollback failed` together with both the original publication error and rollback error, and requires manual cleanup before retry. The original failure is never replaced by a false success.

## Deterministic source guard

`scripts/preflight-v26-draft-release-rollback.py` checks helper and workflow ordering and runs mutation probes for these independent safety properties:

- pre-existing remote tag protection;
- draft-only release deletion;
- exact-SHA ownership before draft deletion;
- exact-SHA recheck before tag deletion;
- workflow rollback wiring after publication failure.

The guard must fail if any of these properties is removed while the surrounding source remains otherwise unchanged.

## Validation boundary

Repository-safe acceptance for this lane is source/guard/hosted CI only. Do not manually dispatch `release-v26.yml` just to test this source change. A real release dispatch remains owner-controlled and may require signing credentials plus licensed BricsCAD V26 runtime evidence according to the existing release policy.

For merge readiness, require exact-head Shared CI, latest-main reconciliation, protected PR `preflight` + `core`, expected-head merge, and exact protected-main verification.