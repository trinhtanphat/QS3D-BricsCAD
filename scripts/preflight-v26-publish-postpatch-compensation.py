#!/usr/bin/env python3
"""Fail closed unless V26 post-PATCH safety invalidation is automatically compensated."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "publish-v26-release.ps1"


def function_body(source: str, name: str) -> str:
    marker = f"function {name}"
    start = source.find(marker)
    if start < 0:
        return ""
    next_function = source.find("\nfunction ", start + len(marker))
    if next_function < 0:
        return source[start:]
    return source[start:next_function]


def validate(source: str) -> list[str]:
    errors: list[str] = []
    helper_name = "Restore-PublishedReleaseAfterSafetyInvalidation"
    helper = function_body(source, helper_name)

    required = (
        (f"function {helper_name}", "dedicated compensation helper"),
        ("Assert-PublishedReleaseMatchesVerifiedTransaction", "pre-compensation exact published identity proof"),
        ("draft = $true", "preferred re-draft request"),
        ("Invoke-RestMethod -Method Patch -Uri $ReleaseUri", "exact release re-draft PATCH"),
        ("Invoke-RestMethod -Method Delete -Uri $ReleaseUri", "exact release deletion fallback"),
        ("Invoke-RestMethod -Method Get -Uri $ReleaseUri", "authoritative compensation reconciliation GET"),
        ("Compensated V26 release must be draft after post-PATCH safety invalidation", "draft postcondition"),
        ("V26 release compensation DELETE is authoritatively committed", "delete-absence postcondition"),
        ("Test-GitHubNotFound", "authoritative 404 classification"),
    )
    for token, label in required:
        if token not in source:
            errors.append(f"missing {label}: {token}")

    phase = "Assert-ProtectedMainStableForPublisherMutation -Phase 'post-release-publish'"
    phase_index = source.find(phase)
    if phase_index < 0:
        errors.append("missing post-release-publish protected-main revalidation")
        return errors

    catch_index = source.find("catch {", phase_index)
    if catch_index < 0:
        errors.append("missing catch for post-release-publish safety invalidation")
        return errors
    catch_tail = source[catch_index : catch_index + 6500]

    call_index = catch_tail.find(helper_name)
    invalid_index = catch_tail.find("$publicationSafetyKnownInvalid = $true")
    throw_index = catch_tail.find(
        "V26 release publication completed, but protected-main safety revalidation failed after publish PATCH"
    )
    if call_index < 0:
        errors.append("post-PATCH safety catch does not invoke compensation")
    if throw_index < 0:
        errors.append("post-PATCH safety catch lost the existing fail-closed terminal error")
    if call_index >= 0 and throw_index >= 0 and call_index > throw_index:
        errors.append("post-PATCH compensation occurs only after the fail-closed throw")
    if invalid_index < 0:
        errors.append("post-PATCH safety catch no longer records known-invalid publication safety")

    if helper:
        proof_index = helper.find("Assert-PublishedReleaseMatchesVerifiedTransaction")
        patch_index = helper.find("Invoke-RestMethod -Method Patch -Uri $ReleaseUri")
        delete_index = helper.find("Invoke-RestMethod -Method Delete -Uri $ReleaseUri")
        get_index = helper.find("Invoke-RestMethod -Method Get -Uri $ReleaseUri")
        if min(proof_index, patch_index, delete_index, get_index) < 0:
            errors.append("compensation helper is missing exact proof/re-draft/delete-fallback/reconciliation stages")
        elif proof_index > patch_index:
            errors.append("compensation helper mutates the release before proving exact published transaction identity")

        fallback_marker = "catch {"
        fallback_index = helper.find(fallback_marker, patch_index if patch_index >= 0 else 0)
        if fallback_index < 0 or delete_index < fallback_index:
            errors.append("exact release deletion must be a fallback after re-draft/reconciliation failure")

        delete_tail = helper[delete_index:]
        if "Test-GitHubNotFound" not in delete_tail:
            errors.append("delete fallback does not reconcile exact release absence through authoritative 404")

    return errors


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    errors = validate(source)
    if errors:
        raise SystemExit("ERROR: V26 post-PATCH publish compensation guard failed:\n - " + "\n - ".join(errors))

    mutations = {
        "drop compensation helper/call": source.replace(
            "Restore-PublishedReleaseAfterSafetyInvalidation",
            "Restore_PublishedReleaseAfterSafetyInvalidation_MUTATED",
        ),
        "drop redraft state": source.replace("draft = $true", "draft = $false"),
        "drop compensation patch": source.replace(
            "Invoke-RestMethod -Method Patch -Uri $ReleaseUri",
            "Invoke-RestMethod -Method Put -Uri $ReleaseUri",
        ),
        "drop deletion fallback": source.replace(
            "Invoke-RestMethod -Method Delete -Uri $ReleaseUri",
            "Invoke-RestMethod -Method Head -Uri $ReleaseUri",
        ),
        "drop authoritative reconciliation": source.replace(
            "Invoke-RestMethod -Method Get -Uri $ReleaseUri",
            "Invoke-RestMethod -Method Options -Uri $ReleaseUri",
        ),
        "drop absence classification": source.replace(
            "Test-GitHubNotFound",
            "Test_GitHubNotFound_MUTATED",
        ),
    }
    for label, mutated in mutations.items():
        if mutated == source:
            raise SystemExit(f"ERROR: V26 publish compensation mutation fixture did not alter source: {label}")
        if not validate(mutated):
            raise SystemExit(f"ERROR: V26 publish compensation guard survived mutation: {label}")

    print("PASS: V26 post-PATCH safety invalidation has bounded exact-release compensation with delete fallback.")


if __name__ == "__main__":
    main()
