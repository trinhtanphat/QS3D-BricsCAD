#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"

PATCH = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
SAFETY_INIT = "$publicationSafetyInvalidated = $false"
POST_FETCH = "$postPublishMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\" -Headers $headers"
POST_SHA = "$postPublishMain = ([string]$postPublishMainResponse.sha).Trim().ToLowerInvariant()"
POST_COMPARE = "if ($postPublishMain -ne $finalMain) {"
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
            "V25 publisher must initialize publication-safety state before final publication.")
    require(patch >= 0, "V25 publisher final publish PATCH was not found.")

    post_fetch = text.find(POST_FETCH, patch)
    require(post_fetch > patch,
            "V25 publisher must re-read protected main after the final publish PATCH.")
    post_sha = text.find(POST_SHA, post_fetch)
    require(post_sha > post_fetch,
            "V25 post-PATCH protected-main response must be normalized to an exact SHA.")
    post_compare = text.find(POST_COMPARE, post_sha)
    require(post_compare > post_sha,
            "V25 publisher must compare post-PATCH protected main to the admitted final-main identity.")
    invalidated_set = text.find(INVALIDATED_SET, post_compare)
    require(invalidated_set > post_compare,
            "V25 post-publish main drift must mark publication safety invalidated.")

    verify_after_patch = text.find(VERIFY_PUBLISHED, invalidated_set)
    require(verify_after_patch > invalidated_set,
            "V25 post-PATCH safety handling must finish before published-release identity acceptance.")
    reconcile = text.find(RECONCILE_PUBLISHED, verify_after_patch)
    require(reconcile > verify_after_patch,
            "V25 published-state acknowledgement reconciliation block was not found.")
    ack = text.find(ACK_SUCCESS, reconcile)
    require(ack > reconcile, "V25 ambiguous publication acknowledgement recovery marker was not found.")
    invalidated_guard = text.find(INVALIDATED_GUARD, reconcile, ack)
    require(invalidated_guard > reconcile,
            "V25 acknowledgement recovery must reject known post-PATCH safety invalidation before success.")
    reconciliation_verify = text.find(VERIFY_PUBLISHED, invalidated_guard, ack)
    require(reconciliation_verify > invalidated_guard,
            "V25 acknowledgement safety guard must execute before reconciled release acceptance.")


text = WORKFLOW.read_text(encoding="utf-8")
validate(text)

for marker in (SAFETY_INIT, POST_FETCH, POST_SHA, POST_COMPARE, INVALIDATED_SET, INVALIDATED_GUARD):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V25 final-publish protected-main stability fence")
