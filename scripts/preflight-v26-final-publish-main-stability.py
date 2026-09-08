#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"

PATCH = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
SAFETY_INIT = "$publicationSafetyInvalidated = $false"
POST_CHECK = "Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'"
INVALIDATED_SET = "$publicationSafetyInvalidated = $true"
RECONCILE_PUBLISHED = "if ($reconciledRelease.draft -eq $false) {"
INVALIDATED_GUARD = "if ($publicationSafetyInvalidated)"
VERIFY_PUBLISHED = "Assert-PublishedReleaseMatchesVerifiedTransaction `"
ACK_SUCCESS = "treating publication as committed"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    safety_init = text.find(SAFETY_INIT)
    patch = text.find(PATCH)
    require(safety_init >= 0 and safety_init < patch,
            "V26 publisher must initialize publication-safety state before final publication.")
    require(patch >= 0, "V26 publisher final publish PATCH was not found.")

    post_check = text.find(POST_CHECK, patch)
    require(post_check > patch,
            "V26 publisher must revalidate protected main after the final publish PATCH.")

    invalidated_set = text.find(INVALIDATED_SET, post_check)
    require(invalidated_set > post_check,
            "V26 post-publish protected-main failure must mark publication safety invalidated after the post-PATCH check.")

    verify_after_patch = text.find(VERIFY_PUBLISHED, invalidated_set)
    require(verify_after_patch > invalidated_set,
            "V26 post-PATCH safety handling must complete before published-release identity acceptance.")

    reconcile = text.find(RECONCILE_PUBLISHED, verify_after_patch)
    require(reconcile > verify_after_patch,
            "V26 published-state acknowledgement reconciliation block was not found.")

    ack = text.find(ACK_SUCCESS, reconcile)
    require(ack > reconcile, "V26 ambiguous publication acknowledgement recovery marker was not found.")

    invalidated_guard = text.find(INVALIDATED_GUARD, reconcile, ack)
    require(invalidated_guard > reconcile,
            "V26 acknowledgement recovery must reject known protected-main safety invalidation before treating publication as committed.")

    reconciliation_verify = text.find(VERIFY_PUBLISHED, invalidated_guard, ack)
    require(reconciliation_verify > invalidated_guard,
            "V26 acknowledgement safety guard must execute before reconciled published-release identity acceptance.")


text = PUBLISHER.read_text(encoding="utf-8")
validate(text)

for marker in (SAFETY_INIT, POST_CHECK, INVALIDATED_SET, INVALIDATED_GUARD):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V26 final-publish protected-main stability fence")
