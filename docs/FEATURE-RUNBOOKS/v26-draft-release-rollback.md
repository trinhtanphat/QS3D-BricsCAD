# V26 draft release rollback / restart safety

Lane-Key: `issue-4780`

## Purpose

The manual V26 release lane creates a GitHub draft release before uploading and remotely verifying package assets. A post-create failure must not strand transaction-owned remote state, because a same-tag retry would otherwise fail before publication and require manual cleanup.

This runbook defines the bounded automatic rollback contract. It does not authorize release dispatch, signing, or licensed BricsCAD runtime claims.

## Positive transaction ownership

Absence immediately before draft creation is not sufficient ownership proof: another actor could create the same tag at the same qualified SHA between an absence check and the release POST. The workflow therefore does not infer ownership from absence.

Inside the publication transaction, `.github/workflows/release-v26.yml` first creates the exact GitHub ref `refs/tags/<release-tag>` through the GitHub REST API with `sha = GITHUB_SHA`. The transaction marks `tagCreatedByThisRun=true` only after the create response returns both the exact expected ref name and the exact qualified workflow SHA. A pre-existing tag causes the GitHub create-ref request to fail and is never owned or deleted by this transaction.

The draft release is created only after positive tag ownership is established. The workflow records `releaseId` only after receiving a positive release identity. `releaseId=0` is an intentional rollback state for failures after tag creation but before a trustworthy draft identity is available.

## Publication envelope

After exact ref ownership is established, the existing publication requirements remain inside one `try` envelope:

1. the created remote tag must resolve exactly to the qualified `GITHUB_SHA`;
2. the draft release must return a positive identity, remain a draft, and match the exact transaction tag;
3. only V26 package assets are uploaded;
4. the draft asset set must exactly match the expected set;
5. remote size and held-generation SHA-256 verification must match local assets;
6. the remote tag is re-resolved and must still target `GITHUB_SHA` after asset verification;
7. only then may the draft be patched to `draft=false`.

Any exception after positive tag ownership enters bounded rollback. An error before ownership is established is surfaced unchanged and does not invoke destructive cleanup.

## Bounded rollback helper

`scripts/rollback-v26-draft-release.ps1` accepts the exact repository, optional positive release id, release tag, qualified workflow SHA, positive `TagCreatedByThisRun` proof, and workflow token. It fails closed unless the destructive preconditions remain true.

Before any cleanup it verifies the exact remote tag still resolves unambiguously to the qualified workflow SHA.

When `ReleaseId > 0`, the helper additionally verifies before draft deletion:

- the fetched release id matches exactly;
- fetched release URL is exactly the expected repository/release API URL;
- release is still a draft;
- release tag exactly matches the transaction tag.

When `ReleaseId = 0`, the helper does not guess a draft identity. This safely covers the case where the transaction created its tag but draft creation failed before returning a trustworthy release id.

After the optional draft deletion, and always before tag deletion, the helper exhaustively enumerates authenticated repository releases in bounded pages. GitHub's release listing exposes drafts to callers with push access, unlike the published-only release-by-tag endpoint. If any draft or published release has the transaction tag, tag deletion is refused. The helper then re-resolves the tag and requires the exact qualified workflow SHA again before deleting it.

This makes ambiguous draft-POST responses fail closed: if GitHub actually created a draft but the workflow never obtained a trustworthy id, the authenticated release enumeration discovers that draft and prevents deleting the tag underneath it. If release enumeration exceeds the bounded page budget, cleanup also fails closed instead of assuming absence.

The helper does not use force push or broad Git tag deletion. Tag deletion is a single GitHub REST deletion for the exact escaped `refs/tags/<release-tag>` identity after all checks above.

## Error reporting

The workflow preserves the original publication error. If bounded rollback succeeds, the step fails with `V26 publication failed after transaction tag creation` and explicitly states that automatic rollback completed, making a same-tag retry safe.

If rollback itself fails closed, the step reports `Automatic V26 draft rollback failed` together with both the original publication error and rollback error, and requires manual cleanup before retry. The original failure is never replaced by a false success.

## Deterministic source guard

`scripts/preflight-v26-draft-release-rollback.py` checks helper and workflow ordering and runs mutation probes for these independent safety properties:

- positive transaction tag ownership rather than absence inference;
- exact created-ref identity;
- exact created-ref SHA binding;
- draft-only release deletion when a release id is known;
- draft-inclusive exhaustive release-owner enumeration before tag deletion;
- exact-SHA ownership before cleanup;
- exact-SHA recheck before tag deletion;
- workflow rollback wiring after publication failure.

The guard must fail if any of these properties is removed while the surrounding source remains otherwise unchanged.

## Validation boundary

Repository-safe acceptance for this lane is source/guard/hosted CI only. Do not manually dispatch `release-v26.yml` just to test this source change. A real release dispatch remains owner-controlled and may require signing credentials plus licensed BricsCAD V26 runtime evidence according to the existing release policy.

For merge readiness, require exact-head Shared CI, latest-main reconciliation, protected PR `preflight` + `core`, expected-head merge, and exact protected-main verification.
