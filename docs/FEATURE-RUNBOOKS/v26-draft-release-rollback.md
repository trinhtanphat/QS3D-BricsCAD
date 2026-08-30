# V26 draft release rollback / restart safety

Origin lane: `issue-4780`  
Publish-acknowledgement hardening: `issue-4812`

## Purpose

The manual V26 release lane creates a GitHub draft release before uploading and remotely verifying package assets. A post-create failure must not strand transaction-owned remote state, because a same-tag retry would otherwise fail before publication and require manual cleanup.

The final `draft=false` publish request also has a commit-unknown boundary: GitHub may commit publication but the runner can lose the HTTP acknowledgement. The workflow must distinguish an exact already-committed publication from a still-owned draft before deciding whether rollback is appropriate.

This runbook defines the bounded automatic rollback and acknowledgement-reconciliation contracts. It does not authorize release dispatch, signing, or licensed BricsCAD runtime claims.

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
6. each successfully hash-verified remote asset contributes its positive GitHub asset id to the transaction identity set;
7. the remote tag is re-resolved and must still target `GITHUB_SHA` after asset verification;
8. the workflow marks `publishPatchAttempted=true` immediately before the final `draft=false` PATCH;
9. only then may the draft be published.

An error before positive tag ownership is surfaced unchanged and does not invoke destructive cleanup. A failure after ownership enters acknowledgement reconciliation or bounded rollback depending on the authoritative remote state.

## Publish acknowledgement reconciliation

A transport exception from the final publish PATCH is not itself evidence that publication failed. When a positive release id exists, the catch path first fetches that exact release API identity.

If the release is still a draft, the transaction continues into the existing bounded rollback path.

If the release is already published (`draft=false`), the workflow treats the publication as committed only when all of these checks succeed:

- the final publish PATCH was actually attempted by this workflow run;
- fetched release id and release API URL match the exact transaction identity;
- the release tag is exactly the requested transaction tag;
- `target_commitish` is exactly the qualified `GITHUB_SHA`;
- prerelease state matches the validated release request;
- the remote tag still resolves exactly to `GITHUB_SHA`;
- the published asset count exactly matches the expected set;
- every published asset name is unique and expected;
- every published asset id equals the exact remote asset id recorded only after that asset passed the earlier held-generation SHA-256 verification;
- each published asset still reports the expected local byte length.

Only after all checks pass does the workflow emit an acknowledgement-recovery message and return success without invoking rollback. This is not optimistic recovery: it is authoritative reconciliation of the exact transaction whose assets were already hash-verified before the PATCH.

If the release is published before this run attempted its final PATCH, if any identity/tag/SHA/prerelease/asset check disagrees, or if authoritative release state cannot be fetched, the workflow fails closed and requires manual cleanup/review. It never infers success from a transport failure alone.

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

If authoritative reconciliation proves the exact release is already published after a final PATCH acknowledgement failure, the workflow reports that the exact qualified release is already committed and exits successfully without destructive cleanup.

If the exact release remains a draft and bounded rollback succeeds, the step fails with `V26 publication failed after transaction tag creation` and explicitly states that automatic rollback completed, making a same-tag retry safe.

If acknowledgement reconciliation is ambiguous or fails, the step reports `V26 publication acknowledgement reconciliation failed` with both the original publication error and reconciliation error and requires manual cleanup/review. If rollback itself fails closed, the step reports `Automatic V26 draft rollback failed` together with both the original publication error and rollback error. The original failure is never replaced by an unverified success.

## Deterministic source guard

`scripts/preflight-v26-draft-release-rollback.py` checks helper and workflow ordering and runs mutation probes for these independent safety properties:

- positive transaction tag ownership rather than absence inference;
- exact created-ref identity;
- exact created-ref SHA binding;
- draft-only release deletion when a release id is known;
- draft-inclusive exhaustive release-owner enumeration before tag deletion;
- exact-SHA ownership before cleanup;
- exact-SHA recheck before tag deletion;
- capture of positive remote asset ids only after held-generation SHA-256 verification;
- explicit proof that the final publish PATCH was attempted;
- authoritative exact-release GET after a publication exception;
- exact published release/tag/SHA/prerelease/asset-identity validation before acknowledgement recovery;
- workflow rollback wiring when the authoritative state is still a draft.

The guard must fail if any of these properties is removed while the surrounding source remains otherwise unchanged.

## Validation boundary

Repository-safe acceptance for this lane is source/guard/hosted CI only. Do not manually dispatch `release-v26.yml` just to test this source change. A real release dispatch remains owner-controlled and may require signing credentials plus licensed BricsCAD V26 runtime evidence according to the existing release policy.

For `issue-4812`, the canonical reservation is intentionally limited to the V26 workflow, focused deterministic guard, and this runbook. The rollback helper remains unchanged from the already-merged `issue-4780` contract.

For merge readiness, require exact-head Shared CI, latest-main reconciliation, protected PR `preflight` + `core`, expected-head merge, and exact protected-main verification.
