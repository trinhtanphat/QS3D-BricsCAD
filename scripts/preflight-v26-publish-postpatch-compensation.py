#!/usr/bin/env python3
"""Fail closed unless V26 post-PATCH safety invalidation is compensated to draft."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "publish-v26-release.ps1"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"ERROR: V26 publish compensation missing {label}: {token}")


def validate(source: str) -> list[str]:
    errors: list[str] = []

    required = (
        ("function Restore-PublishedReleaseToDraftAfterSafetyInvalidation", "dedicated compensation helper"),
        ("Assert-PublishedReleaseMatchesVerifiedTransaction", "pre-compensation exact published identity proof"),
        ("draft = $true", "draft compensation request"),
        ("Invoke-RestMethod -Method Patch -Uri $ReleaseUri", "exact release compensation PATCH"),
        ("Invoke-RestMethod -Method Get -Uri $ReleaseUri", "authoritative compensation reconciliation GET"),
        ("Compensated V26 release must be draft after post-PATCH safety invalidation", "draft postcondition"),
        ("Restore-PublishedReleaseToDraftAfterSafetyInvalidation", "post-PATCH safety catch compensation call"),
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
    catch_tail = source[catch_index : catch_index + 5000]

    call = "Restore-PublishedReleaseToDraftAfterSafetyInvalidation"
    call_index = catch_tail.find(call)
    invalid_index = catch_tail.find("$publicationSafetyKnownInvalid = $true")
    throw_index = catch_tail.find("V26 release publication completed, but protected-main safety revalidation failed after publish PATCH")
    if call_index < 0:
        errors.append("post-PATCH safety catch does not invoke compensation")
    if throw_index < 0:
        errors.append("post-PATCH safety catch lost the existing fail-closed terminal error")
    if call_index >= 0 and throw_index >= 0 and call_index > throw_index:
        errors.append("post-PATCH compensation occurs only after the fail-closed throw")
    if invalid_index < 0:
        errors.append("post-PATCH safety catch no longer records known-invalid publication safety")

    helper_index = source.find("function Restore-PublishedReleaseToDraftAfterSafetyInvalidation")
    if helper_index >= 0:
        helper_end = source.find("\n}\n", helper_index)
        helper = source[helper_index : helper_end + 3 if helper_end >= 0 else helper_index + 10000]
        proof_index = helper.find("Assert-PublishedReleaseMatchesVerifiedTransaction")
        patch_index = helper.find("Invoke-RestMethod -Method Patch -Uri $ReleaseUri")
        get_index = helper.find("Invoke-RestMethod -Method Get -Uri $ReleaseUri")
        draft_check_index = helper.find("Compensated V26 release must be draft after post-PATCH safety invalidation")
        if min(proof_index, patch_index, get_index, draft_check_index) < 0:
            errors.append("compensation helper is missing proof/PATCH/reconcile/draft-postcondition stages")
        elif not (proof_index < patch_index < get_index < draft_check_index):
            errors.append("compensation helper must prove exact published identity before PATCH and reconcile draft state afterward")

    return errors


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    errors = validate(source)
    if errors:
        raise SystemExit("ERROR: V26 post-PATCH publish compensation guard failed:\n - " + "\n - ".join(errors))

    # Mutation-lock the critical primitives so the guard cannot pass by merely
    # mentioning the compensation contract in comments/dead text.
    mutations = {
        "drop compensation call": source.replace(
            "Restore-PublishedReleaseToDraftAfterSafetyInvalidation",
            "Restore_PublishedReleaseToDraftAfterSafetyInvalidation_MUTATED",
        ),
        "drop redraft state": source.replace("draft = $true", "draft = $false"),
        "drop compensation patch": source.replace(
            "Invoke-RestMethod -Method Patch -Uri $ReleaseUri",
            "Invoke-RestMethod -Method Put -Uri $ReleaseUri",
        ),
        "drop authoritative reconciliation": source.replace(
            "Invoke-RestMethod -Method Get -Uri $ReleaseUri",
            "Invoke-RestMethod -Method Head -Uri $ReleaseUri",
        ),
    }
    for label, mutated in mutations.items():
        if mutated == source:
            raise SystemExit(f"ERROR: V26 publish compensation mutation fixture did not alter source: {label}")
        if not validate(mutated):
            raise SystemExit(f"ERROR: V26 publish compensation guard survived mutation: {label}")

    print("PASS: V26 post-PATCH safety invalidation compensates the exact published release back to draft.")


if __name__ == "__main__":
    main()
