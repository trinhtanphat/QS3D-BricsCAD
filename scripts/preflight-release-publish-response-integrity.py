#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
v25 = (root / ".github" / "workflows" / "release-v25.yml").read_text(encoding="utf-8")
v26 = (root / ".github" / "workflows" / "release-v26.yml").read_text(encoding="utf-8")


def validate_success_response(workflow: str, version: str) -> list[str]:
    errors: list[str] = []
    patch = workflow.find("$published = Invoke-RestMethod -Method Patch -Uri $releaseUri")
    catch = workflow.find("$publicationError = $_", patch + 1)
    if patch < 0 or catch <= patch:
        return [f"{version} final publish PATCH/catch scope is missing"]
    success_scope = workflow[patch:catch]
    assertion = success_scope.find("Assert-PublishedReleaseMatchesVerifiedTransaction")
    if assertion < 0:
        return [f"{version} successful final PATCH response lacks exact published-transaction verification"]
    required = [
        "-ReleaseSnapshot $published",
        "-ReleaseUri $releaseUri",
        "-ReleaseId $releaseId",
        "-VerifiedAssetIds $verifiedAssetIds",
        "-LocalAssets $localAssets",
    ]
    if version == "V25":
        required += [
            "-ExpectedReleaseName $expectedReleaseName",
            "-ExpectedAssets $assetNames",
            "-IsPrerelease:($env:RELEASE_PRERELEASE -eq 'true')",
            "-TransactionMarker $draftTransactionMarker",
        ]
    else:
        required += ["-ExpectedAssets $expectedAssets", "-IsPrerelease $isPrerelease"]
    for token in required:
        if token not in success_scope:
            errors.append(f"{version} successful publish verification missing: {token}")
    if success_scope.find("$publishPatchAttempted = $true") < 0:
        errors.append(f"{version} successful publish verification lost final-PATCH attempt proof")
    if success_scope.find("$published = Invoke-RestMethod -Method Patch") > assertion:
        errors.append(f"{version} exact transaction verification must follow the PATCH response")
    return errors


def validate(v25_text: str, v26_text: str) -> list[str]:
    return validate_success_response(v25_text, "V25") + validate_success_response(v26_text, "V26")


errors = validate(v25, v26)
if errors:
    raise SystemExit("Release successful publish-response integrity failed: " + "; ".join(errors))

checks = [
    ("V25 response binding", v25.replace("-ReleaseSnapshot $published", "-ReleaseSnapshot $null", 1), v26),
    ("V26 response binding", v25, v26.replace("-ReleaseSnapshot $published", "-ReleaseSnapshot $null", 1)),
]
for label, test_v25, test_v26 in checks:
    if not validate(test_v25, test_v26):
        raise SystemExit(f"Release publish-response regression probe did not fail closed: {label}")

print("PASS V25/V26 successful publish responses require exact transaction verification")
