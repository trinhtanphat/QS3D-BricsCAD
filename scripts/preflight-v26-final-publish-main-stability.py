#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"

PATCH_ATTEMPT = "$publishPatchAttempted = $true"
PATCH = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
SAFETY_INIT = "$publicationSafetyInvalidated = $false"
SAFETY_UNPROVEN = "$publicationSafetyInvalidated = $true"
KNOWN_INVALID_INIT = "$publicationSafetyKnownInvalid = $false"
KNOWN_INVALID_SET = "$publicationSafetyKnownInvalid = $true"
POST_CHECK = "Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'"
SAFETY_CLEAR = "$publicationSafetyInvalidated = $false"
RECONCILE_PUBLISHED = "if ($reconciledRelease.draft -eq $false) {"
KNOWN_INVALID_GUARD = "if ($publicationSafetyKnownInvalid)"
RECONCILE_CHECK = "Assert-ProtectedMainStableForPublisherMutation -Phase 'publish-acknowledgement-reconciliation'"
VERIFY_PUBLISHED = "Assert-PublishedReleaseMatchesVerifiedTransaction `"
ACK_SUCCESS = "treating publication as committed"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    safety_init = text.find(SAFETY_INIT)
    patch_attempt = text.find(PATCH_ATTEMPT)
    patch = text.find(PATCH, patch_attempt)
    require(safety_init >= 0 and safety_init < patch_attempt,
            "V26 publisher must initialize publication-safety state before final publication.")
    known_init = text.find(KNOWN_INVALID_INIT, safety_init, patch_attempt)
    require(known_init > safety_init,
            "V26 publisher must initialize sticky known-invalid safety before final publication.")
    require(patch_attempt >= 0 and patch > patch_attempt,
            "V26 publisher final publish PATCH sequence was not found.")

    unproven = text.find(SAFETY_UNPROVEN, patch_attempt, patch)
    require(unproven > patch_attempt,
            "V26 publication safety must become unproven before final PATCH.")

    post_check = text.find(POST_CHECK, patch)
    require(post_check > patch,
            "V26 publisher must revalidate protected main after the final publish PATCH.")
    clear_after_patch = text.find(SAFETY_CLEAR, post_check)
    require(clear_after_patch > post_check,
            "V26 publication safety may clear only after post-PATCH protected-main validation.")

    verify_after_patch = text.find(VERIFY_PUBLISHED, clear_after_patch)
    require(verify_after_patch > clear_after_patch,
            "V26 post-PATCH main safety must be proven before published-release identity acceptance.")
    known_invalid_set = text.find(KNOWN_INVALID_SET, post_check, verify_after_patch)
    require(known_invalid_set > post_check,
            "V26 failed post-PATCH validation must set sticky known-invalid safety before identity acceptance.")

    reconcile = text.find(RECONCILE_PUBLISHED, verify_after_patch)
    require(reconcile > verify_after_patch,
            "V26 published-state acknowledgement reconciliation block was not found.")
    ack = text.find(ACK_SUCCESS, reconcile)
    require(ack > reconcile,
            "V26 ambiguous publication acknowledgement recovery marker was not found.")

    known_guard = text.find(KNOWN_INVALID_GUARD, reconcile, ack)
    require(known_guard > reconcile,
            "V26 acknowledgement recovery must reject a previously observed post-PATCH safety failure.")
    reconcile_check = text.find(RECONCILE_CHECK, known_guard, ack)
    require(reconcile_check > known_guard,
            "V26 ambiguous acknowledgement must independently revalidate protected main before success.")
    reconcile_clear = text.find(SAFETY_CLEAR, reconcile_check, ack)
    require(reconcile_clear > reconcile_check,
            "V26 acknowledgement recovery may clear unproven safety only after protected-main revalidation.")
    reconciliation_verify = text.find(VERIFY_PUBLISHED, reconcile_clear, ack)
    require(reconciliation_verify > reconcile_clear,
            "V26 acknowledgement safety proof must execute before reconciled published-release identity acceptance.")


text = PUBLISHER.read_text(encoding="utf-8")
validate(text)

mutation_snippets = (
    KNOWN_INVALID_INIT,
    PATCH_ATTEMPT + "\n  # Once the mutation starts, publication safety is unproven until protected main is revalidated.\n  " + SAFETY_UNPROVEN,
    POST_CHECK + "\n    " + SAFETY_CLEAR,
    KNOWN_INVALID_SET,
    KNOWN_INVALID_GUARD,
    RECONCILE_CHECK + "\n        " + SAFETY_CLEAR,
)
for snippet in mutation_snippets:
    require(snippet in text, f"Mutation probe could not find required sequence: {snippet}")
    mutated = text.replace(snippet, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {snippet}")

print("PASS V26 final-publish protected-main stability fence")
