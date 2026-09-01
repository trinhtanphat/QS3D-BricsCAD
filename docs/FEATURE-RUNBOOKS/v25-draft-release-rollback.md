# V25 commercial draft release rollback / restart safety

Lane-Key: `issue-4819`
Tag-create acknowledgement hardening: `issue-4827`
Draft-create acknowledgement hardening: `issue-4880`
Publish acknowledgement hardening: `issue-4935`

## Purpose

The commercial V25 release lane creates a GitHub draft before it can verify downloaded draft bytes and publish. A transient failure after draft creation must not strand transaction-owned remote state and make the same tag permanently non-retryable.

The tag-create REST request has its own commit-unknown boundary: GitHub can create the exact ref while the runner loses the HTTP acknowledgement. Restart safety therefore treats an already-existing exact lightweight tag at the qualified workflow SHA as reusable state without converting it into destructive ownership.

The draft-create REST request has the same commit-unknown property. `issue-4880` binds each create attempt to a unique transaction marker embedded in the draft request and permits acknowledgement recovery only by bounded authenticated enumeration that identifies exactly one draft matching marker, release tag, target workflow SHA, prerelease state, expected release name and draft state. Zero candidates, multiple candidates, moved targets, published state or any identity mismatch fail closed. Recovery never infers identity from the tag alone.

The final publish PATCH is also a commit-unknown boundary. `issue-4935` records positive proof that this workflow attempted that PATCH and, only after a transport/error path, re-fetches the exact release by its already-validated numeric id. A lost PATCH acknowledgement is accepted as committed publication only when the authoritative release and remote tag still prove the exact verified transaction. This prevents a successful publication from being misclassified as a failed draft that should be rolled back, without inferring success from transport failure alone.

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

## Publish acknowledgement reconciliation

The workflow initializes `publishPatchAttempted=false` and changes it to `true` immediately before the final `PATCH` that requests `draft=false`. The flag is evidence about this workflow attempt only; it is not publication proof.

If the publication path throws after a positive `releaseId` exists, the workflow performs an authenticated GET of that exact release URI before deciding whether draft rollback is safe. If the authoritative state is still the exact draft, normal bounded rollback remains available. A published state is accepted as a committed publication only when all of the following remain true:

- this workflow positively attempted the publish PATCH;
- exact release id and repository API URL match the transaction-owned release;
- `draft=false`, exact release tag, exact workflow target SHA, expected V25 release name and requested prerelease state all match;
- the release body still contains this attempt's exact draft-transaction marker;
- the exact lightweight remote tag still resolves to `GITHUB_SHA`;
- the release contains exactly the expected asset names;
- the asset ids are exactly those captured after the pre-publication draft verification; and
- each authoritative published asset size equals the corresponding local verified asset byte length.

Only this conjunction converts a transport/error path into truthful committed publication, and no rollback/delete follows. If the release became published before `publishPatchAttempted=true`, or the GET is unreachable, the draft state is ambiguous, or any release/tag/asset identity moved or mismatches, acknowledgement reconciliation throws with explicit manual-cleanup diagnostics. That path is deliberately non-destructive and never infers success. A confirmed exact draft continues to the existing rollback helper instead.

## Existing commercial evidence remains mandatory

The transaction does not weaken the pre-publication commercial gates. The candidate remains bound to exact product version, source SHA, Authenticode signer and trusted timestamp, checksum/provenance metadata, signed update manifest, and the licensed V25 runtime policy. After draft creation, the exact expected asset set is uploaded and downloaded again; held-generation SHA-256 and packaged signature checks remain before publication.

Publish acknowledgement recovery does not create new evidence. It only re-validates the authoritative identities and byte sizes already established by the same workflow transaction. It does not replace signing, timestamp, downloaded-byte, provenance or licensed-runtime requirements.

## Bounded rollback

`scripts/rollback-v25-draft-release.ps1` receives the exact repository, release id (zero or positive), release tag, workflow SHA, positive tag-ownership proof, and workflow token.

Before cleanup it requires the exact remote tag to resolve unambiguously to the qualified workflow SHA. When `ReleaseId > 0`, it fetches the exact release URI and verifies id, repository URL, draft state and tag before deleting that draft. A published or mismatched release is never deleted.

The destructive DELETE acknowledgement itself is reconciled fail-closed. If the exact draft DELETE throws, the helper performs an authenticated GET of that same release id. Only an authoritative 404 is accepted as proof that the draft deletion committed despite the lost acknowledgement. If the release still exists, the helper validates exact id/repository/draft/tag identity and refuses to assume deletion; a published, mismatched, or unreachable reconciliation also fails closed.

When `ReleaseId = 0`, the helper does not guess a release identity. After optional exact draft deletion it enumerates authenticated repository releases in bounded pages. This includes drafts for the push-authorized workflow token. If any draft or published release still owns the tag, or release enumeration exceeds the bounded page budget, tag deletion is refused.

After the release-owner scan, a non-owned reusable tag is preserved and reported with `TagDeleted=false`. Only a tag with positive `TagCreatedByThisRun=true` proof is eligible for deletion. Immediately before deleting such an owned exact Git ref, the helper resolves the tag again and requires the same qualified workflow SHA. It uses no force push and no broad tag cleanup. If that exact tag-ref DELETE throws, the helper GETs the same escaped ref. Only authoritative 404 proves committed deletion. A surviving ref must still be exactly `refs/tags/<release-tag>` at the qualified workflow SHA and is then treated as a failed deletion; a moved/mismatched/unreachable ref fails closed.

This separation permits rollback of an exactly-owned draft even when the admitted tag is reusable but not deletion-owned. Same-SHA same-tag retry therefore remains possible without inferring ownership from an ambiguous create acknowledgement.

## Failure reporting

If authoritative publish reconciliation proves the exact qualified release is already published after this workflow attempted PATCH, the workflow reports that publication as committed and performs no destructive cleanup.

If authoritative state is the exact owned draft, rollback follows the existing path. If rollback succeeds, publication still fails and reports the original publication error plus an explicit statement that automatic rollback completed and same-tag retry is safe. If rollback fails closed, the workflow reports both the original publication error and rollback error and requires manual cleanup.

If publish acknowledgement reconciliation itself is unreachable, ambiguous or identity-mismatched, the workflow fails closed with the original publication error plus the reconciliation error and requires manual cleanup. It does not invoke rollback against a state that may already be published.

A network exception after a destructive DELETE is no longer itself enough to declare rollback failure. Same-tag retry safety is claimed only when the exact destructive resource is authoritatively absent or the ordinary DELETE returned successfully, while a reusable non-owned tag is intentionally preserved.

## Deterministic guard

`scripts/preflight-v25-draft-release-rollback.py` pins restart-safe exact-tag admission, positive creation ownership, ambiguous tag-create acknowledgement reconciliation, reusable non-owned exact-tag behavior, draft-create transaction marker generation, bounded draft-inclusive acknowledgement reconciliation, exact marker/tag/SHA/name/prerelease/draft identity, unique-candidate recovery, zero/multiple-candidate failure, publish-attempt proof, authoritative exact-release GET after publication error, exact published release/tag/transaction-marker/asset-id/asset-byte reconciliation, pre-PATCH publication veto, draft-only deletion, draft-inclusive release-owner enumeration, non-owned tag preservation, exact-SHA recheck, exact-resource DELETE acknowledgement reconciliation, and workflow catch/rollback wiring.

Draft-create identity checks are scoped to `Resolve-AmbiguousDraftCreate`, while published-state identity checks are scoped independently to `Assert-PublishedReleaseMatchesVerifiedTransaction`. This prevents duplicate release-identity tokens in one function from accidentally satisfying a mutation that removed the corresponding independent safety check in the other function. Mutation probes must fail closed when any independent safety property is removed.

## Validation boundary

Do not dispatch `.github/workflows/release-v25.yml` solely to validate this change. Merge readiness is repository-safe source validation: exact-head Shared CI, current-main reconciliation, protected PR `preflight` + `core`, expected-head merge and exact protected-main verification. Commercial signing and licensed-host evidence remain governed by the existing owner-controlled release workflow.