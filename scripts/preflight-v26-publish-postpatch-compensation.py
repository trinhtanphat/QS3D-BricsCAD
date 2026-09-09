#!/usr/bin/env python3
"""Fail closed unless V26 post-PATCH safety invalidation removes only the exact stale release."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "publish-v26-release.ps1"


def function_body(source: str, name: str) -> str:
    marker = f"function {name}"
    start = source.find(marker)
    if start < 0:
        return ""
    next_function = source.find("\nfunction ", start + len(marker))
    return source[start:] if next_function < 0 else source[start:next_function]


def validate(source: str) -> list[str]:
    errors: list[str] = []
    helper_name = "Remove-PublishedReleaseAfterSafetyInvalidation"
    helper = function_body(source, helper_name)

    required = (
        (f"function {helper_name}", "dedicated exact-release compensation helper"),
        ("Invoke-RestMethod -Method Delete -Uri $ReleaseUri", "exact release DELETE"),
        ("Test-GitHubNotFound", "authoritative 404 classification"),
        ("V26 release compensation DELETE is authoritatively committed", "absence postcondition"),
        ("$releaseId = [long]0", "local release ownership reset after exact deletion"),
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
    call_index = catch_tail.find(helper_name)
    reset_index = catch_tail.find("$releaseId = [long]0")
    throw_index = catch_tail.find(
        "V26 release publication completed, but protected-main safety revalidation failed after publish PATCH"
    )
    if call_index < 0:
        errors.append("post-PATCH safety catch does not invoke exact-release compensation")
    if "-ReleaseSnapshot $published" in catch_tail:
        errors.append("post-PATCH safety compensation still trusts the stale PATCH response snapshot")
    if reset_index < 0:
        errors.append("post-PATCH safety catch does not clear deleted release ownership")
    if throw_index < 0:
        errors.append("post-PATCH safety catch lost the existing fail-closed terminal error")
    if call_index >= 0 and reset_index >= 0 and call_index > reset_index:
        errors.append("release ownership is cleared before exact-release compensation succeeds")
    if reset_index >= 0 and throw_index >= 0 and reset_index > throw_index:
        errors.append("release ownership is cleared only after the fail-closed throw")
    if "$publicationSafetyKnownInvalid = $true" not in catch_tail:
        errors.append("post-PATCH safety catch no longer records known-invalid publication safety")

    if helper:
        if "[Parameter(Mandatory = $true)]$ReleaseSnapshot" in helper:
            errors.append("compensation helper accepts a caller-supplied release snapshot instead of refreshing authority")

        current_get = "$currentRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers"
        proof_current = "-ReleaseSnapshot $currentRelease"
        delete_call = "Invoke-RestMethod -Method Delete -Uri $ReleaseUri"
        remaining_get = "$remainingRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers"
        current_get_index = helper.find(current_get)
        proof_current_index = helper.find(proof_current)
        delete_index = helper.find(delete_call)
        remaining_get_index = helper.find(remaining_get)
        not_found_index = helper.find("Test-GitHubNotFound")
        committed_index = helper.find("V26 release compensation DELETE is authoritatively committed")

        if current_get_index < 0:
            errors.append("compensation helper does not refresh the authoritative release immediately before destructive proof")
        if proof_current_index < 0:
            errors.append("compensation helper does not prove the fresh authoritative release snapshot")
        if delete_index < 0:
            errors.append("compensation helper is missing exact release DELETE")
        if remaining_get_index < 0:
            errors.append("compensation helper is missing authoritative post-DELETE reconciliation GET")
        if min(current_get_index, proof_current_index, delete_index, remaining_get_index) >= 0 and not (
            current_get_index < proof_current_index < delete_index < remaining_get_index
        ):
            errors.append("compensation helper must refresh, prove, DELETE, then reconcile in that exact order")
        if min(not_found_index, committed_index) < 0 or (delete_index >= 0 and not_found_index < delete_index):
            errors.append("compensation helper must classify authoritative absence after the DELETE attempt")
        if "draft = $true" in helper or "-Method Patch" in helper:
            errors.append("post-PATCH compensation must not depend on a second release-state PATCH")

    return errors


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    errors = validate(source)
    if errors:
        raise SystemExit("ERROR: V26 post-PATCH publish compensation guard failed:\n - " + "\n - ".join(errors))

    mutations = {
        "drop helper/call": source.replace(
            "Remove-PublishedReleaseAfterSafetyInvalidation",
            "Remove_PublishedReleaseAfterSafetyInvalidation_MUTATED",
        ),
        "drop fresh authoritative GET": source.replace(
            "$currentRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers",
            "$currentRelease = $published",
        ),
        "prove stale snapshot": source.replace(
            "-ReleaseSnapshot $currentRelease",
            "-ReleaseSnapshot $published",
        ),
        "drop exact DELETE": source.replace(
            "Invoke-RestMethod -Method Delete -Uri $ReleaseUri",
            "Invoke-RestMethod -Method Head -Uri $ReleaseUri",
        ),
        "drop authoritative reconciliation GET": source.replace(
            "$remainingRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers",
            "$remainingRelease = $currentRelease",
        ),
        "drop 404 classifier": source.replace("Test-GitHubNotFound", "Test_GitHubNotFound_MUTATED"),
        "drop ownership reset": source.replace("$releaseId = [long]0", "$releaseId = [long]-1"),
    }
    for label, mutated in mutations.items():
        if mutated == source:
            raise SystemExit(f"ERROR: V26 publish compensation mutation fixture did not alter source: {label}")
        if not validate(mutated):
            raise SystemExit(f"ERROR: V26 publish compensation guard survived mutation: {label}")

    print("PASS: V26 post-PATCH safety invalidation refreshes authority, proves exact ownership, deletes, and reconciles absence.")


if __name__ == "__main__":
    main()
