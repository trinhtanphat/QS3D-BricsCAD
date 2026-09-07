#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"

PATCH = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
SAFETY_INIT = "$publicationSafetyInvalidated = $false"
SAFETY_UNPROVEN = "$publicationSafetyInvalidated = $true"
POST_VALIDATE = "Assert-V25ProtectedMainStillAdmitted -ExpectedMain $finalMain -Phase 'post-release-publish'"
SAFETY_CLEAR = "$publicationSafetyInvalidated = $false"
RECONCILE_PUBLISHED = "if ($reconciledRelease.draft -eq $false) {"
RECONCILE_VALIDATE = "Assert-V25ProtectedMainStillAdmitted -ExpectedMain $finalMain -Phase 'publish-acknowledgement-reconciliation'"
INVALIDATED_GUARD = "if ($publicationSafetyInvalidated)"
VERIFY_PUBLISHED = "Assert-PublishedReleaseMatchesVerifiedTransaction `"
ACK_SUCCESS = "treating publication as committed"
HELPER = "function Assert-V25ProtectedMainStillAdmitted {"
HELPER_FETCH = "$currentMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\" -Headers $headers"
HELPER_SHA = "$currentMain = ([string]$currentMainResponse.sha).Trim().ToLowerInvariant()"
HELPER_EXACT = "if ($currentMain -ne $ExpectedMain) {"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    helper = text.find(HELPER)
    require(helper >= 0, "V25 publisher must define one reusable protected-main post-publication admission helper.")
    helper_fetch = text.find(HELPER_FETCH, helper)
    helper_sha = text.find(HELPER_SHA, helper_fetch)
    helper_exact = text.find(HELPER_EXACT, helper_sha)
    require(helper_fetch > helper and helper_sha > helper_fetch and helper_exact > helper_sha,
            "V25 post-publication main helper must re-read, normalize and exact-compare protected main.")

    safety_init = text.find(SAFETY_INIT)
    patch = text.find(PATCH)
    require(safety_init >= 0 and safety_init < patch,
            "V25 publisher must initialize publication-safety state before final publication.")
    require(patch >= 0, "V25 publisher final publish PATCH was not found.")

    unproven = text.rfind(SAFETY_UNPROVEN, safety_init, patch)
    require(unproven > safety_init,
            "V25 publication safety must become unproven before the final publish PATCH so transport ambiguity cannot fail open.")
    post_validate = text.find(POST_VALIDATE, patch)
    require(post_validate > patch,
            "V25 publisher must revalidate exact protected main after the final publish PATCH.")
    clear = text.find(SAFETY_CLEAR, post_validate)
    require(clear > post_validate,
            "V25 publication safety may clear only after successful post-PATCH protected-main revalidation.")
    verify_after_patch = text.find(VERIFY_PUBLISHED, clear)
    require(verify_after_patch > clear,
            "V25 post-PATCH main safety must be proven before returned published-release identity acceptance.")

    reconcile = text.find(RECONCILE_PUBLISHED, verify_after_patch)
    require(reconcile > verify_after_patch,
            "V25 published-state acknowledgement reconciliation block was not found.")
    ack = text.find(ACK_SUCCESS, reconcile)
    require(ack > reconcile, "V25 ambiguous publication acknowledgement recovery marker was not found.")
    reconcile_validate = text.find(RECONCILE_VALIDATE, reconcile, ack)
    require(reconcile_validate > reconcile,
            "V25 ambiguous publish acknowledgement must independently revalidate protected main before success.")
    reconcile_clear = text.find(SAFETY_CLEAR, reconcile_validate, ack)
    require(reconcile_clear > reconcile_validate,
            "V25 acknowledgement recovery may clear safety only after successful main revalidation.")
    invalidated_guard = text.find(INVALIDATED_GUARD, reconcile_clear, ack)
    require(invalidated_guard > reconcile_clear,
            "V25 acknowledgement recovery must reject any still-unproven publication safety before success.")
    reconciliation_verify = text.find(VERIFY_PUBLISHED, invalidated_guard, ack)
    require(reconciliation_verify > invalidated_guard,
            "V25 acknowledgement safety proof must execute before reconciled release identity acceptance.")


text = WORKFLOW.read_text(encoding="utf-8")
validate(text)

for marker in (
    HELPER, HELPER_FETCH, HELPER_SHA, HELPER_EXACT, SAFETY_INIT, SAFETY_UNPROVEN,
    POST_VALIDATE, RECONCILE_VALIDATE, INVALIDATED_GUARD,
):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V25 final-publish protected-main stability fence")
