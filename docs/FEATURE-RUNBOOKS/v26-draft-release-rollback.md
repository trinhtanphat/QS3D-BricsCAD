# V26 draft release rollback / restart safety

Origin lane: `issue-4780`  
Publish-acknowledgement hardening: `issue-4812`  
Rollback DELETE-acknowledgement hardening: `issue-4815`  
Tag-create acknowledgement hardening: `issue-4827`

## Purpose

The manual V26 release lane creates a GitHub draft release before uploading and remotely verifying package assets. A post-create failure must not strand transaction-owned remote state, because a same-tag retry would otherwise fail before publication and require manual cleanup.

The final `draft=false` publish request, destructive cleanup requests, and the initial tag-create request all have commit-unknown boundaries. GitHub can commit one of those mutations while the runner loses its HTTP acknowledgement. The workflow/helper therefore reconcile authoritative remote state rather than inferring success or failure from transport outcomes alone.

This runbook defines bounded automatic rollback and acknowledgement-reconciliation contracts. It does not authorize release dispatch, signing, or licensed BricsCAD runtime claims.

## Restart-safe tag admission and positive ownership

The workflow first classifies any local tag. It accepts only an exact lightweight commit tag at the qualified `GITHUB_SHA`; mismatched or annotated state fails closed.

Inside the publication transaction, the workflow queries the exact GitHub ref `refs/tags/<release-tag>`. If an exact lightweight ref already exists at `GITHUB_SHA`, it is reusable for publication but is not deletion-owned by this run. If the ref is absent, the workflow POSTs the exact tag ref at `GITHUB_SHA` and sets `tagCreatedByThisRun=true` only after the create response returns the exact requested ref, object type `commit`, and exact qualified SHA.

If the create POST throws, the workflow performs an authoritative exact-ref lookup. An authoritative absence preserves the original create failure. If the exact lightweight ref now exists at `GITHUB_SHA`, the acknowledgement is classified as ambiguous: the tag becomes reusable but `tagCreatedByThisRun` remains false. A moved, annotated, mismatched, or unreachable tag fails closed.

The separate `tagReadyForRelease` state records successful exact-tag admission without implying destructive ownership. The draft release is created only after that admission. `releaseId=0` remains an intentional rollback state until a positive draft identity is received.

## Publication envelope and publish acknowledgement

Before the final publish PATCH, the transaction verifies exact lightweight tag identity at the qualified SHA, exact expected assets, remote byte lengths, and held-generation SHA-256 parity; every successfully verified remote asset contributes its positive GitHub asset id to the transaction identity set. The workflow marks `publishPatchAttempted=true` immediately before the final `draft=false` PATCH.

If the PATCH acknowledgement is lost, the workflow fetches the exact release. An already-published release is accepted as committed only if the PATCH was attempted and exact release id/API URL, tag, target SHA, prerelease state, remote tag target, asset names/counts, previously verified remote asset IDs, and local byte lengths all still match. A still-draft release proceeds to bounded rollback. Mismatched or unreachable state fails closed to manual review.

## Bounded rollback helper

`scripts/rollback-v26-draft-release.ps1` requires exact repository, optional positive release id, release tag, qualified workflow SHA, positive `TagCreatedByThisRun` proof, and workflow token. It fails closed unless destructive preconditions remain true.

Before cleanup it resolves the exact remote tag and requires the qualified workflow SHA. When `ReleaseId > 0`, the helper fetches the exact release and requires exact id/API URL, draft state, and transaction tag before attempting draft deletion.

After the optional draft deletion, and always before any tag deletion, the helper exhaustively enumerates authenticated repository releases in bounded pages. Any draft or published release still owning the tag blocks tag deletion. If `TagCreatedByThisRun=false`, the exact admitted tag is preserved with `TagDeleted=false`; rollback of an exact owned draft can therefore complete without deleting a reusable tag whose creation ownership is unknown. Only positive tag-creation ownership permits the helper to continue to exact-SHA re-resolution and exact tag DELETE. It never uses force push or broad Git deletion.

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

Immediately before an owned tag DELETE, the existing exhaustive release-owner check and exact remote-SHA recheck remain mandatory. If the exact tag DELETE throws, the helper queries the exact GitHub ref identity through the non-destructive GET-ref endpoint.

- HTTP 404 is the only automatic success reconciliation: the exact tag is authoritatively absent, so deletion is treated as committed.
- If the ref still exists, the helper requires the exact expected ref identity and workflow SHA merely to classify it; it still fails closed and does not retry DELETE.
- A moved/mismatched ref fails closed.
- Any non-404 reconciliation failure fails closed with both errors.

This means a lost DELETE response can no longer produce a false manual-cleanup blocker when GitHub actually committed the exact deletion, while an uncertain or changed remote state still cannot be converted into success. A reusable tag admitted without positive creation ownership is never sent to the destructive tag-delete path.

## Deterministic source guard

`scripts/preflight-v26-draft-release-rollback.py` pins restart-safe exact-tag admission, reusable non-owned exact-tag behavior, positive tag ownership, ambiguous tag-create acknowledgement reconciliation, draft-only deletion, exhaustive release-owner enumeration, non-owned tag preservation, exact-SHA checks, publish acknowledgement reconciliation, and rollback wiring. It mutation-probes:

- exact reusable-tag lookup and lightweight commit/SHA identity;
- positive ownership only from an acknowledged exact create response;
- ambiguous create acknowledgement reconciliation without promotion to deletion ownership;
- `tagReadyForRelease` admission independently from tag ownership;
- explicit classification of GitHub 404 as the sole authoritative absence signal;
- exact-release GET after a draft DELETE exception;
- committed-draft-deletion recovery only on authoritative absence;
- refusal to infer success when the exact draft still exists or changed;
- non-owned tag preservation after draft rollback;
- exact-ref GET after a tag DELETE exception;
- exact ref/SHA validation when an owned tag still exists;
- committed-tag-deletion recovery only on authoritative absence;
- ordering of each reconciliation while preserving release-owner enumeration, final SHA recheck, publish acknowledgement reconciliation, and bounded rollback.

The guard must fail if any independent property is removed while surrounding source remains otherwise unchanged.

## Validation boundary

Repository-safe acceptance for this lane is source/guard/hosted CI only. Do not manually dispatch `release-v26.yml` to test rollback source changes. A real release remains owner-controlled and may require signing credentials plus licensed BricsCAD V26 runtime evidence according to existing release policy.

For merge readiness, require exact-head Shared CI, latest-main reconciliation, protected PR `preflight` + `core`, expected-head merge, and exact protected-main verification.
