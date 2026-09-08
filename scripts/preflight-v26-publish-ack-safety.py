#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"

PATCH_ATTEMPT = "$publishPatchAttempted = $true"
SAFETY_UNPROVEN = "$publicationSafetyInvalidated = $true"
KNOWN_INVALID_INIT = "$publicationSafetyKnownInvalid = $false"
KNOWN_INVALID_SET = "$publicationSafetyKnownInvalid = $true"
PATCH = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
POST_VALIDATE = "Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'"
SAFETY_CLEAR = "$publicationSafetyInvalidated = $false"
RECONCILE_PUBLISHED = "if ($reconciledRelease.draft -eq $false) {"
KNOWN_INVALID_GUARD = "if ($publicationSafetyKnownInvalid)"
RECONCILE_VALIDATE = "Assert-ProtectedMainStableForPublisherMutation -Phase 'publish-acknowledgement-reconciliation'"
VERIFY = "Assert-PublishedReleaseMatchesVerifiedTransaction `"
ACK_SUCCESS = "treating publication as committed"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    attempt = text.find(PATCH_ATTEMPT)
    patch = text.find(PATCH, attempt)
    require(attempt >= 0 and patch > attempt, "V26 final publish PATCH sequence was not found.")

    known_init = text.find(KNOWN_INVALID_INIT, 0, attempt)
    require(known_init >= 0, "V26 publisher must initialize sticky known-invalid publication safety before mutation.")
    unproven = text.find(SAFETY_UNPROVEN, attempt, patch)
    require(
        unproven > attempt,
        "V26 publication safety must become unproven before PATCH so a committed-but-unacknowledged publish cannot fail open.",
    )

    post_validate = text.find(POST_VALIDATE, patch)
    require(post_validate > patch, "V26 publisher must revalidate protected main after an acknowledged publish PATCH.")
    clear = text.find(SAFETY_CLEAR, post_validate)
    require(
        clear > post_validate,
        "V26 publication safety may clear only after successful post-PATCH protected-main validation.",
    )
    verify_normal = text.find(VERIFY, clear)
    require(
        verify_normal > clear,
        "V26 returned publication identity may be accepted only after post-PATCH main safety is proven.",
    )
    known_set = text.find(KNOWN_INVALID_SET, post_validate, verify_normal)
    require(
        known_set > post_validate,
        "V26 failed post-PATCH validation must set sticky known-invalid publication safety before identity acceptance.",
    )

    reconcile = text.find(RECONCILE_PUBLISHED, verify_normal)
    require(reconcile > verify_normal, "V26 ambiguous acknowledgement reconciliation block was not found.")
    ack = text.find(ACK_SUCCESS, reconcile)
    require(ack > reconcile, "V26 ambiguous acknowledgement success marker was not found.")

    known_guard = text.find(KNOWN_INVALID_GUARD, reconcile, ack)
    require(
        known_guard > reconcile,
        "V26 acknowledgement reconciliation must reject a previously observed post-PATCH safety failure.",
    )
    reconcile_validate = text.find(RECONCILE_VALIDATE, known_guard, ack)
    require(
        reconcile_validate > known_guard,
        "V26 ambiguous acknowledgement must independently revalidate protected main before success.",
    )
    reconcile_clear = text.find(SAFETY_CLEAR, reconcile_validate, ack)
    require(
        reconcile_clear > reconcile_validate,
        "V26 acknowledgement reconciliation may clear unproven safety only after main revalidation.",
    )
    verify_reconciled = text.find(VERIFY, reconcile_clear, ack)
    require(
        verify_reconciled > reconcile_clear,
        "V26 reconciled release identity verification must run only after publication-safety proof and clear.",
    )


text = PUBLISHER.read_text(encoding="utf-8")
validate(text)

mutation_snippets = (
    PATCH_ATTEMPT
    + "\n  # Once the mutation starts, publication safety is unproven until protected main is revalidated.\n  "
    + SAFETY_UNPROVEN,
    POST_VALIDATE + "\n    " + SAFETY_CLEAR,
    KNOWN_INVALID_SET,
    KNOWN_INVALID_GUARD,
    RECONCILE_VALIDATE + "\n        " + SAFETY_CLEAR,
)
for snippet in mutation_snippets:
    require(snippet in text, f"Mutation probe could not find sequence: {snippet}")
    mutated = text.replace(snippet, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing sequence: {snippet}")

print("PASS V26 publish acknowledgement safety")
