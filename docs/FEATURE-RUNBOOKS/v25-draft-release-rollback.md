# V25 commercial draft release rollback / restart safety

Lane-Key: `issue-4787`

## Purpose

The commercial V25 release lane creates a GitHub draft before it can verify downloaded draft bytes and publish. A transient failure after draft creation must not strand transaction-owned remote state and make the same tag permanently non-retryable.

This is a repository-safe release-transaction contract. It does not authorize a commercial release dispatch, signing claim, timestamp claim, or licensed BricsCAD runtime claim.

## Positive tag ownership

The workflow does not infer tag ownership from an earlier absence check. Inside the publication transaction it first creates exact `refs/tags/<release-tag>` with the GitHub REST refs API at the exact qualified `GITHUB_SHA`. Ownership becomes true only after the response reports the exact requested ref, object type `commit`, and exact workflow SHA. A pre-existing ref causes create-ref to fail and is never owned by this run.

Only after positive tag ownership does the workflow create a draft release using the exact tag. `releaseId` stays zero until a positive draft identity is returned. This preserves a fail-closed state for ambiguous create responses.

## Existing commercial evidence remains mandatory

The transaction does not weaken the pre-publication commercial gates. The candidate remains bound to exact product version, source SHA, Authenticode signer and trusted timestamp, checksum/provenance metadata, signed update manifest, and the licensed V25 runtime policy. After draft creation, the exact expected asset set is uploaded and downloaded again; held-generation SHA-256 and packaged signature checks remain before publication.

## Bounded rollback

`scripts/rollback-v25-draft-release.ps1` receives the exact repository, release id (zero or positive), release tag, workflow SHA, positive tag-ownership proof, and workflow token.

Before cleanup it requires the exact remote tag to resolve unambiguously to the qualified workflow SHA. When `ReleaseId > 0`, it fetches the exact release URI and verifies id, repository URL, draft state and tag before deleting that draft. A published or mismatched release is never deleted.

When `ReleaseId = 0`, the helper does not guess a release identity. After optional exact draft deletion it enumerates authenticated repository releases in bounded pages. This includes drafts for the push-authorized workflow token. If any draft or published release still owns the tag, or release enumeration exceeds the bounded page budget, tag deletion is refused.

Immediately before deleting the exact Git ref, the helper resolves the tag again and requires the same qualified workflow SHA. It uses no force push and no broad tag cleanup.

An ambiguous draft-create response therefore fails closed: if GitHub created a draft but the workflow did not receive a trustworthy id, authenticated release enumeration finds ownership and requires explicit manual cleanup instead of deleting the tag underneath an unknown draft.

## Failure reporting

If rollback succeeds, publication still fails and reports the original publication error plus an explicit statement that automatic rollback completed and same-tag retry is safe. If rollback fails closed, the workflow reports both the original publication error and rollback error and requires manual cleanup.

## Deterministic guard

`scripts/preflight-v25-draft-release-rollback.py` pins positive tag ownership, exact ref/SHA creation, zero-or-positive release identity, draft-only deletion, draft-inclusive release-owner enumeration, exact-SHA recheck and workflow catch/rollback wiring. Mutation probes must fail closed when any independent safety property is removed.

## Validation boundary

Do not dispatch `.github/workflows/release-v25.yml` solely to validate this change. Merge readiness is repository-safe source validation: exact-head Shared CI, current-main reconciliation, protected PR `preflight` + `core`, expected-head merge and exact protected-main verification. Commercial signing and licensed-host evidence remain governed by the existing owner-controlled release workflow.
