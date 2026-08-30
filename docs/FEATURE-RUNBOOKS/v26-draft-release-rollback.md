# V26 draft release rollback / restart safety

Origin lane: `issue-4780`  
Publish-acknowledgement hardening: `issue-4812`  
Rollback DELETE-acknowledgement hardening: `issue-4815`

## Purpose

The manual V26 release lane creates a GitHub draft release before uploading and remotely verifying package assets. A post-create failure must not strand transaction-owned remote state, because a same-tag retry would otherwise fail before publication and require manual cleanup.

The final `draft=false` publish request has a commit-unknown boundary, and the destructive cleanup requests have the same class of boundary: GitHub can commit a DELETE while the runner loses its HTTP acknowledgement. The workflow/helper therefore reconcile authoritative remote state rather than inferring success or failure from transport outcomes alone.

This runbook defines bounded automatic rollback and acknowledgement-reconciliation contracts. It does not authorize release dispatch, signing, or licensed BricsCAD runtime claims.

## Positive transaction ownership

Absence immediately before draft creation is not sufficient ownership proof. The workflow first creates the exact GitHub ref `refs/tags/<release-tag>` through the GitHub REST API with `sha = GITHUB_SHA` and marks `tagCreatedByThisRun=true` only after the create response returns the exact expected ref and qualified SHA. A pre-existing tag is never owned or deleted by this transaction.

The draft release is created only after positive tag ownership is established. The workflow records `releaseId` only after receiving a positive release identity. `releaseId=0` remains an intentional rollback state for failures after tag creation but before a trustworthy draft identity is available.

## Publication envelope and publish acknowledgement

Before the final publish PATCH, the transaction verifies exact tag ownership, exact expected assets, remote byte lengths, and held-generation SHA-256 parity; every successfully verified remote asset contributes its positive GitHub asset id to the transaction identity set. The workflow marks `publishPatchAttempted=true` immediately before the final `draft=false` PATCH.

If the PATCH acknowledgement is lost, the workflow fetches the exact release. An already-published release is accepted as committed only if the PATCH was attempted and exact release id/API URL, tag, target SHA, prerelease state, remote tag target, asset names/counts, previously verified remote asset IDs, and local byte lengths all still match. A still-draft release proceeds to bounded rollback. Mismatched or unreachable state fails closed to manual review.

## Bounded rollback helper

`scripts/rollback-v26-draft-release.ps1` requires exact repository, optional positive release id, release tag, qualified workflow SHA, positive `TagCreatedByThisRun` proof, and workflow token. It fails closed unless destructive preconditions remain true.

Before cleanup it resolves the exact remote tag and requires the qualified workflow SHA. When `ReleaseId > 0`, the helper fetches the exact release and requires exact id/API URL, draft state, and transaction tag before attempting draft deletion.

After the optional draft deletion, and always before tag deletion, the helper exhaustively enumerates authenticated repository releases in bounded pages. Any draft or published release still owning the tag blocks tag deletion. The helper then re-resolves the tag and requires the exact qualified workflow SHA immediately before the exact tag DELETE. It never uses force push or broad Git deletion.

## Destructive DELETE acknowledgement reconciliation

A failed HTTP acknowledgement after a DELETE is not enough to classify the destructive operation as failed. `issue-4815` closes both commit-unknown boundaries without introducing blind retries.

### Draft DELETE

If the exact draft DELETE call throws, the helper immediately performs an authenticated GET of that exact release API identity.

- HTTP 404 is the only automatic success reconciliation: the exact release is authoritatively absent, so the helper treats the draft deletion as committed and continues.
- If GET succeeds, the helper validates exact release id, API URL, draft state, and transaction tag. Even when all those values still match, the helper fails closed because the owned draft still exists; it does not blindly retry DELETE.
- A published, mismatched, or otherwise changed release fails closed.
- A reconciliation transport/API failure other than authoritative 404 also fails closed with both the original DELETE error and reconciliation error.

Only after this reconciliation or a normally acknowledged DELETE does exhaustive release-owner enumeration run.

### Tag DELETE

Immediately before tag DELETE, the existing exhaustive release-owner check and exact remote-SHA recheck remain mandatory. If the exact tag DELETE throws, the helper queries the exact GitHub ref identity through the non-destructive GET-ref endpoint.

- HTTP 404 is the only automatic success reconciliation: the exact tag is authoritatively absent, so deletion is treated as committed.
- If the ref still exists, the helper requires the exact expected ref identity and workflow SHA merely to classify it; it still fails closed and does not retry DELETE.
- A moved/mismatched ref fails closed.
- Any non-404 reconciliation failure fails closed with both errors.

This means a lost DELETE response can no longer produce a false manual-cleanup blocker when GitHub actually committed the exact deletion, while an uncertain or changed remote state still cannot be converted into success.

## Deterministic source guard

`scripts/preflight-v26-draft-release-rollback.py` retains the positive tag ownership, draft-only deletion, exhaustive release-owner enumeration, exact-SHA checks, publish acknowledgement reconciliation, and rollback wiring contracts. It additionally requires and mutation-probes:

- explicit classification of GitHub 404 as the sole authoritative absence signal;
- exact-release GET after a draft DELETE exception;
- committed-draft-deletion recovery only on authoritative absence;
- refusal to infer success when the exact draft still exists or changed;
- exact-ref GET after a tag DELETE exception;
- exact ref/SHA validation when the tag still exists;
- committed-tag-deletion recovery only on authoritative absence;
- ordering of each reconciliation immediately after its destructive request while preserving release-owner enumeration and the final SHA recheck before tag deletion.

The guard must fail if any of these properties is removed while surrounding source remains otherwise unchanged.

## Validation boundary

Repository-safe acceptance for this lane is source/guard/hosted CI only. Do not manually dispatch `release-v26.yml` to test rollback source changes. A real release remains owner-controlled and may require signing credentials plus licensed BricsCAD V26 runtime evidence according to existing release policy.

For `issue-4815`, the canonical reservation is limited to the rollback helper, focused deterministic guard, and this runbook. The release workflow remains unchanged from the already-merged `issue-4812` contract.

For merge readiness, require exact-head Shared CI, latest-main reconciliation, protected PR `preflight` + `core`, expected-head merge, and exact protected-main verification.
