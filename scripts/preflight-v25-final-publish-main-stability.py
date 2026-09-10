#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"

PATCH = "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri"
SAFETY_INIT = "$publicationSafetyInvalidated = $false"
SAFETY_UNPROVEN = "$publicationSafetyInvalidated = $true"
KNOWN_INVALID_INIT = "$publicationSafetyKnownInvalid = $false"
KNOWN_INVALID_SET = "$publicationSafetyKnownInvalid = $true"
POST_VALIDATE = "Assert-V25ProtectedMainStillAdmitted -ExpectedMain $finalMain -Phase 'post-release-publish'"
SAFETY_CLEAR = "$publicationSafetyInvalidated = $false"
RECONCILE_PUBLISHED = "if ($reconciledRelease.draft -eq $false) {"
KNOWN_INVALID_GUARD = "if ($publicationSafetyKnownInvalid)"
RECONCILE_VALIDATE = "Assert-V25ProtectedMainStillAdmitted -ExpectedMain $finalMain -Phase 'publish-acknowledgement-reconciliation'"
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
    require(
        helper_fetch > helper and helper_sha > helper_fetch and helper_exact > helper_sha,
        "V25 post-publication main helper must re-read, normalize and exact-compare protected main.",
    )

    safety_init = text.find(SAFETY_INIT)
    patch = text.find(PATCH)
    require(safety_init >= 0 and safety_init < patch,
            "V25 publisher must initialize publication-safety state before final publication.")
    known_init = text.find(KNOWN_INVALID_INIT, safety_init, patch)
    require(known_init > safety_init,
            "V25 publisher must initialize sticky known-invalid safety state before final publication.")
    require(patch >= 0, "V25 publisher final publish PATCH was not found.")

    unproven = text.rfind(SAFETY_UNPROVEN, known_init, patch)
    require(unproven > known_init,
            "V25 publication safety must become unproven before final PATCH so committed/lost acknowledgement cannot fail open.")
    post_validate = text.find(POST_VALIDATE, patch)
    require(post_validate > patch,
            "V25 publisher must revalidate exact protected main after an acknowledged final publish PATCH.")
    clear = text.find(SAFETY_CLEAR, post_validate)
    require(clear > post_validate,
            "V25 publication safety may clear only after successful post-PATCH protected-main revalidation.")
    verify_after_patch = text.find(VERIFY_PUBLISHED, clear)
    require(verify_after_patch > clear,
            "V25 post-PATCH main safety must be proven before returned published-release identity acceptance.")
    known_set = text.find(KNOWN_INVALID_SET, post_validate, verify_after_patch)
    require(known_set > post_validate,
            "V25 failed post-PATCH validation must set sticky known-invalid safety before identity acceptance.")

    reconcile = text.find(RECONCILE_PUBLISHED, verify_after_patch)
    require(reconcile > verify_after_patch,
            "V25 published-state acknowledgement reconciliation block was not found.")
    ack = text.find(ACK_SUCCESS, reconcile)
    require(ack > reconcile, "V25 ambiguous publication acknowledgement recovery marker was not found.")
    known_guard = text.find(KNOWN_INVALID_GUARD, reconcile, ack)
    require(known_guard > reconcile,
            "V25 acknowledgement recovery must reject an observed post-PATCH safety failure before retrying admission.")
    reconcile_validate = text.find(RECONCILE_VALIDATE, known_guard, ack)
    require(reconcile_validate > known_guard,
            "V25 ambiguous publish acknowledgement must independently revalidate protected main before success.")
    reconcile_clear = text.find(SAFETY_CLEAR, reconcile_validate, ack)
    require(reconcile_clear > reconcile_validate,
            "V25 acknowledgement recovery may clear unproven safety only after successful main revalidation.")
    reconciliation_verify = text.find(VERIFY_PUBLISHED, reconcile_clear, ack)
    require(reconciliation_verify > reconcile_clear,
            "V25 acknowledgement safety proof must execute before reconciled release identity acceptance.")


def remove_first_after(source: str, marker: str, target: str, label: str) -> str:
    marker_index = source.find(marker)
    require(marker_index >= 0, f"Mutation probe could not find marker for {label}: {marker}")
    target_index = source.find(target, marker_index + len(marker))
    require(target_index >= 0, f"Mutation probe could not find target for {label}: {target}")
    return source[:target_index] + "__QS3D_MUTATION_REMOVED__" + source[target_index + len(target):]


text = WORKFLOW.read_text(encoding="utf-8")
validate(text)

mutation_snippets = (
    HELPER,
    HELPER_FETCH,
    HELPER_SHA,
    HELPER_EXACT,
    KNOWN_INVALID_INIT,
    SAFETY_UNPROVEN,
    POST_VALIDATE,
    KNOWN_INVALID_SET,
    KNOWN_INVALID_GUARD,
    RECONCILE_VALIDATE,
)
for snippet in mutation_snippets:
    require(snippet in text, f"Mutation probe could not find required marker: {snippet}")
    mutated = text.replace(snippet, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {snippet}")

for label, marker in (
    ("post-PATCH safety clear", POST_VALIDATE),
    ("acknowledgement-reconciliation safety clear", RECONCILE_VALIDATE),
):
    mutated = remove_first_after(text, marker, SAFETY_CLEAR, label)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing {label}")

print("PASS V25 final-publish protected-main stability fence")
