#!/usr/bin/env python3
"""Fail closed unless V25 post-PATCH safety failures return the exact release to draft."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def validate(source: str) -> list[str]:
    errors: list[str] = []

    publish = "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri"
    publish_index = source.find(publish)
    if publish_index < 0:
        return ["missing V25 public release PATCH"]

    post_publish = source[publish_index:]
    required = (
        ("try {", "post-PATCH guarded validation region"),
        ("catch {", "post-PATCH compensation catch"),
        ("$compensationBody = @{ draft = $true; prerelease = $true } | ConvertTo-Json", "safe-state compensation payload"),
        ("$compensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri", "exact-release compensation PATCH"),
        ("$authoritativeCompensatedRelease = Invoke-RestMethod -Method Get -Uri $releaseUri", "authoritative compensation reconciliation GET"),
        ("$authoritativeCompensatedRelease.draft -ne $true", "non-public draft postcondition"),
        ("$authoritativeCompensatedRelease.prerelease -ne $true", "prerelease postcondition"),
        ("$publishMainAfterResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"", "post-PATCH protected-main revalidation"),
        ("$publishMainAfter -ne $publishMain", "post-PATCH protected-main identity comparison"),
        ("throw \"V25 release publication safety validation failed after publish PATCH", "fail-closed terminal error after compensation"),
    )
    for token, label in required:
        if token not in post_publish:
            errors.append(f"missing {label}: {token}")

    try_index = post_publish.find("try {")
    catch_index = post_publish.find("catch {")
    compensate_index = post_publish.find("$compensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri")
    reconcile_index = post_publish.find("$authoritativeCompensatedRelease = Invoke-RestMethod -Method Get -Uri $releaseUri")
    throw_index = post_publish.find("throw \"V25 release publication safety validation failed after publish PATCH")
    post_main_index = post_publish.find("$publishMainAfterResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"")

    if min(try_index, catch_index, compensate_index, reconcile_index, throw_index) >= 0:
        if not (try_index < catch_index < compensate_index < reconcile_index < throw_index):
            errors.append("post-PATCH compensation must catch validation failure, re-draft, reconcile, then fail closed")
    if try_index >= 0 and post_main_index >= 0 and not (try_index < post_main_index < catch_index):
        errors.append("post-PATCH protected-main revalidation must be inside the guarded validation region")

    catch_tail = post_publish[catch_index:] if catch_index >= 0 else ""
    identity_tokens = (
        "$authoritativeCompensatedRelease.tag_name",
        "$env:RELEASE_TAG",
        "$authoritativeCompensatedRelease.target_commitish",
        "$env:RELEASE_COMMIT_SHA",
        "$authoritativeCompensatedRelease.assets",
        "$verifiedReleaseAssetIds",
    )
    for token in identity_tokens:
        if token not in catch_tail:
            errors.append(f"compensation reconciliation does not preserve exact release identity: {token}")

    if "Invoke-RestMethod -Method Delete -Uri $releaseUri" in catch_tail:
        errors.append("V25 compensation must prefer reversible re-draft over destructive release deletion")

    return errors


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    errors = validate(source)
    if errors:
        raise SystemExit("ERROR: V25 post-PATCH publish compensation guard failed:\n - " + "\n - ".join(errors))

    mutations = {
        "drop compensation PATCH": source.replace(
            "$compensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri",
            "$compensatedRelease = Invoke-RestMethod -Method Get -Uri $releaseUri",
        ),
        "drop authoritative reconciliation": source.replace(
            "$authoritativeCompensatedRelease = Invoke-RestMethod -Method Get -Uri $releaseUri",
            "$authoritativeCompensatedRelease = $compensatedRelease",
        ),
        "drop draft postcondition": source.replace(
            "$authoritativeCompensatedRelease.draft -ne $true",
            "$authoritativeCompensatedRelease.draft -ne $false",
        ),
        "drop post-PATCH main check": source.replace(
            "$publishMainAfterResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"",
            "$publishMainAfterResponse = $publishMainResponse",
        ),
        "drop terminal failure": source.replace(
            "throw \"V25 release publication safety validation failed after publish PATCH",
            "Write-Warning \"V25 release publication safety validation failed after publish PATCH",
        ),
    }
    for label, mutated in mutations.items():
        if mutated == source:
            raise SystemExit(f"ERROR: V25 publish compensation mutation fixture did not alter source: {label}")
        if not validate(mutated):
            raise SystemExit(f"ERROR: V25 publish compensation guard survived mutation: {label}")

    print("PASS: V25 post-PATCH safety failures re-draft the exact release, reconcile authority, and fail closed.")


if __name__ == "__main__":
    main()
