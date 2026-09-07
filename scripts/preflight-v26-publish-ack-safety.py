#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"

PATCH_ATTEMPT = "$publishPatchAttempted = $true"
SAFETY_UNPROVEN = "$publicationSafetyInvalidated = $true"
PATCH = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
POST_VALIDATE = "Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'"
SAFETY_CLEAR = "$publicationSafetyInvalidated = $false"
RECONCILE_PUBLISHED = "if ($reconciledRelease.draft -eq $false) {"
RECONCILE_VALIDATE = "Assert-ProtectedMainStableForPublisherMutation -Phase 'publish-acknowledgement-reconciliation'"
INVALIDATED_GUARD = "if ($publicationSafetyInvalidated)"
VERIFY = "Assert-PublishedReleaseMatchesVerifiedTransaction `"
ACK_SUCCESS = "treating publication as committed"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    attempt = text.find(PATCH_ATTEMPT)
    patch = text.find(PATCH, attempt)
    require(attempt >= 0 and patch > attempt, "V26 final publish PATCH sequence was not found.")

    unproven = text.find(SAFETY_UNPROVEN, attempt, patch)
    require(unproven > attempt,
            "V26 publication safety must become unproven before PATCH so a committed-but-unacknowledged publish cannot fail open.")

    post_validate = text.find(POST_VALIDATE, patch)
    require(post_validate > patch, "V26 publisher must revalidate protected main after an acknowledged publish PATCH.")
    clear = text.find(SAFETY_CLEAR, post_validate)
    require(clear > post_validate,
            "V26 publication safety may clear only after successful post-PATCH protected-main validation.")
    verify_normal = text.find(VERIFY, clear)
    require(verify_normal > clear,
            "V26 returned publication identity may be accepted only after post-PATCH main safety is proven.")

    reconcile = text.find(RECONCILE_PUBLISHED, verify_normal)
    require(reconcile > verify_normal, "V26 ambiguous acknowledgement reconciliation block was not found.")
    ack = text.find(ACK_SUCCESS, reconcile)
    require(ack > reconcile, "V26 ambiguous acknowledgement success marker was not found.")

    reconcile_validate = text.find(RECONCILE_VALIDATE, reconcile, ack)
    require(reconcile_validate > reconcile,
            "V26 ambiguous acknowledgement must independently revalidate protected main before success.")
    reconcile_clear = text.find(SAFETY_CLEAR, reconcile_validate, ack)
    require(reconcile_clear > reconcile_validate,
            "V26 acknowledgement reconciliation may clear unproven safety only after main revalidation.")
    guard = text.find(INVALIDATED_GUARD, reconcile_clear, ack)
    require(guard > reconcile_clear,
            "V26 acknowledgement reconciliation must reject still-invalid publication safety before accepting release identity.")
    verify_reconciled = text.find(VERIFY, guard, ack)
    require(verify_reconciled > guard,
            "V26 reconciled release identity verification must run after publication-safety proof.")


text = PUBLISHER.read_text(encoding="utf-8")
validate(text)

for marker in (SAFETY_UNPROVEN, POST_VALIDATE, SAFETY_CLEAR, RECONCILE_VALIDATE, INVALIDATED_GUARD):
    require(marker in text, f"Mutation probe could not find marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V26 publish acknowledgement safety")
