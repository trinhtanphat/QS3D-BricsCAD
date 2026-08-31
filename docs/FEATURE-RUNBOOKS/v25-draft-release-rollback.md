# V25 commercial draft release rollback / restart safety

Lane-Key: `issue-4819`
Tag-create acknowledgement hardening: `issue-4827`
Draft-create acknowledgement hardening: `issue-4880`

## Purpose

The commercial V25 release lane creates a GitHub draft before it can verify downloaded draft bytes and publish. A transient failure after draft creation must not strand transaction-owned remote state and make the same tag permanently non-retryable.

The tag-create REST request has its own commit-unknown boundary: GitHub can create the exact ref while the runner loses the HTTP acknowledgement. Restart safety therefore treats an already-existing exact lightweight tag at the qualified workflow SHA as reusable state without converting it into destructive ownership.

The draft-create REST request has the same commit-unknown property. `issue-4880` binds each create attempt to a unique transaction marker embedded in the draft request and permits acknowledgement recovery only by bounded authenticated enumeration that identifies exactly one draft matching marker, release tag, target workflow SHA, prerelease state, expected release name and draft state. Zero candidates, multiple candidates, moved targets, published state or any identity mismatch fail closed. Recovery never infers identity from the tag alone.

This is a repository-safe release-transaction contract. It does not authorize a commercial release dispatch, signing claim, timestamp claim, or licensed BricsCAD runtime claim.

## Restart-safe tag admission and positive ownership

The workflow classifies the local tag first. No tag is acceptable except an exact lightweight commit tag at `GITHUB_SHA`; mismatched or annotated state fails closed.

Inside the publication transaction, the workflow queries the exact GitHub ref `refs/tags/<release-tag>` through the authenticated REST ref endpoint. If an exact lightweight ref already exists at `GITHUB_SHA`, it is reusable for publication but is explicitly **not deletion-owned by this run**. If the tag is absent, the workflow POSTs the exact ref at `GITHUB_SHA` and sets `tagCreatedByThisRun=true` only after the create response reports the exact requested ref, object type `commit`, and exact workflow SHA.

If the create POST throws, the workflow performs an authoritative exact-ref lookup. An authoritative absence preserves the original failure. If the exact lightweight tag now exists at `GITHUB_SHA`, the create acknowledgement is classified as ambiguous: the tag is reusable, `tagCreatedByThisRun` remains false, and publication may continue. A moved, annotated, mismatched, or unreachable tag remains fail-closed.

The separate `tagReadyForRelease` state records successful exact-tag admission without implying destructive ownership. Only after exact tag admission does the workflow create the draft release.

## Draft-create acknowledgement reconciliation

Immediately before the draft POST, the workflow generates a unique transaction marker for that workflow attempt and places it in the draft body together with the normal commercial release notes. The marker is transaction metadata only; it does not relax tag/SHA/prerelease/name validation.

If the POST returns a normal response, the workflow still validates the returned release as a draft with the exact tag, exact target SHA, expected prerelease state and expected V25 release name before assigning `releaseId`.

If the POST throws, the workflow does not retry creation. Instead it enumerates repository releases through the authenticated API with `per_page=100` and a strict maximum page budget, including drafts. A candidate is recoverable only when all of these are simultaneously true:

- `draft=true`;
- exact `tag_name == RELEASE_TAG`;
- exact `target_commitish == GITHUB_SHA`;
- `prerelease` equals the requested prerelease flag;
- exact release name equals the V25 release name for this tag;
- release body contains the exact unique transaction marker for this workflow attempt;
- positive numeric release id and repository API identity are present.

Exactly one candidate is required. Zero matching drafts means the original create acknowledgement failure remains authoritative for this run. More than one matching candidate is an identity ambiguity and fails closed. A matching marker on a published, moved or otherwise mismatched release also fails closed. Only after this reconciliation produces one exact draft does the workflow assign `releaseId` and proceed with upload/verification. This gives rollback a positive identity even when the create acknowledgement was lost.

## Existing commercial evidence remains mandatory

The transaction does not weaken the pre-publication commercial gates. The candidate remains bound to exact product version, source SHA, Authenticode signer and trusted timestamp, checksum/provenance metadata, signed update manifest, and the licensed V25 runtime policy. After draft creation, the exact expected asset set is uploaded and downloaded again; held-generation SHA-256 and packaged signature checks remain before publication.

## Bounded rollback

`scripts/rollback-v25-draft-release.ps1` receives the exact repository, release id (zero or positive), release tag, workflow SHA, positive tag-ownership proof, and workflow token.

Before cleanup it requires the exact remote tag to resolve unambiguously to the qualified workflow SHA. When `ReleaseId > 0`, it fetches the exact release URI and verifies id, repository URL, draft state and tag before deleting that draft. A published or mismatched release is never deleted.

The destructive DELETE acknowledgement itself is reconciled fail-closed. If the exact draft DELETE throws, the helper performs an authenticated GET of that same release id. Only an authoritative 404 is accepted as proof that the draft deletion committed despite the lost acknowledgement. If the release still exists, the helper validates exact id/repository/draft/tag identity and refuses to assume deletion; a published, mismatched, or unreachable reconciliation also fails closed.

When `ReleaseId = 0`, the helper does not guess a release identity. After optional exact draft deletion it enumerates authenticated repository releases in bounded pages. This includes drafts for the push-authorized workflow token. If any draft or published release still owns the tag, or release enumeration exceeds the bounded page budget, tag deletion is refused.

After the release-owner scan, a non-owned reusable tag is preserved and reported with `TagDeleted=false`. Only a tag with positive `TagCreatedByThisRun=true` proof is eligible for deletion. Immediately before deleting such an owned exact Git ref, the helper resolves the tag again and requires the same qualified workflow SHA. It uses no force push and no broad tag cleanup. If that exact tag-ref DELETE throws, the helper GETs the same escaped ref. Only authoritative 404 proves committed deletion. A surviving ref must still be exactly `refs/tags/<release-tag>` at the qualified workflow SHA and is then treated as a failed deletion; a moved/mismatched/unreachable ref fails closed.

This separation permits rollback of an exactly-owned draft even when the admitted tag is reusable but not deletion-owned. Same-SHA same-tag retry therefore remains possible without inferring ownership from an ambiguous create acknowledgement.

## Failure reporting

If rollback succeeds, publication still fails and reports the original publication error plus an explicit statement that automatic rollback completed and same-tag retry is safe. If rollback fails closed, the workflow reports both the original publication error and rollback error and requires manual cleanup.

A network exception after a destructive DELETE is no longer itself enough to declare rollback failure. Same-tag retry safety is claimed only when the exact destructive resource is authoritatively absent or the ordinary DELETE returned successfully, while a reusable non-owned tag is intentionally preserved.

## Deterministic guard

`scripts/preflight-v25-draft-release-rollback.py` pins restart-safe exact-tag admission, positive creation ownership, ambiguous tag-create acknowledgement reconciliation, reusable non-owned exact-tag behavior, draft-create transaction marker generation, bounded draft-inclusive acknowledgement reconciliation, exact marker/tag/SHA/name/prerelease/draft identity, unique-candidate recovery, zero/multiple-candidate failure, draft-only deletion, draft-inclusive release-owner enumeration, non-owned tag preservation, exact-SHA recheck, exact-resource DELETE acknowledgement reconciliation, and workflow catch/rollback wiring. Mutation probes must fail closed when exact-tag identity, authoritative-absence classification, ambiguous-create non-ownership, transaction-marker identity, unique draft recovery, surviving-resource identity checks, or any independent safety property is removed.

## Validation boundary

Do not dispatch `.github/workflows/release-v25.yml` solely to validate this change. Merge readiness is repository-safe source validation: exact-head Shared CI, current-main reconciliation, protected PR `preflight` + `core`, expected-head merge and exact protected-main verification. Commercial signing and licensed-host evidence remain governed by the existing owner-controlled release workflow.
