#!/usr/bin/env python3
"""Fail closed unless V25 publication ambiguity returns the exact release to draft."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def validate(source: str) -> list[str]:
    errors: list[str] = []

    publish_body = "$publishBody = @{ draft = $false } | ConvertTo-Json"
    publish = "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri"
    publish_body_index = source.find(publish_body)
    publish_index = source.find(publish)
    if publish_body_index < 0 or publish_index < 0 or publish_body_index >= publish_index:
        return ["missing ordered V25 public release PATCH"]

    # The public mutation itself must be inside the guarded transaction. A successful server-side
    # PATCH followed by a lost/failed HTTP acknowledgement is still publication ambiguity and must
    # enter the same fail-safe re-draft path as an ordinary post-PATCH invariant failure.
    publication_prefix = source[publish_body_index:publish_index]
    try_index_absolute = source.find("try {", publish_body_index, publish_index)
    if try_index_absolute < 0:
        errors.append("public release PATCH is not inside the guarded publication transaction")

    transaction = source[try_index_absolute if try_index_absolute >= 0 else publish_index:]
    required = (
        ("catch {", "publication compensation catch"),
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
        if token not in transaction:
            errors.append(f"missing {label}: {token}")

    publish_in_transaction = transaction.find(publish)
    catch_index = transaction.find("catch {")
    compensate_index = transaction.find("$compensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri")
    reconcile_index = transaction.find("$authoritativeCompensatedRelease = Invoke-RestMethod -Method Get -Uri $releaseUri")
    throw_index = transaction.find("throw \"V25 release publication safety validation failed after publish PATCH")
    post_main_index = transaction.find("$publishMainAfterResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"")

    if min(publish_in_transaction, catch_index, compensate_index, reconcile_index, throw_index) >= 0:
        if not (publish_in_transaction < catch_index < compensate_index < reconcile_index < throw_index):
            errors.append("publication compensation must guard PATCH, catch failure, re-draft, reconcile, then fail closed")
    if post_main_index >= 0 and catch_index >= 0 and not (publish_in_transaction < post_main_index < catch_index):
        errors.append("post-PATCH protected-main revalidation must be inside the guarded publication transaction")

    catch_tail = transaction[catch_index:] if catch_index >= 0 else ""
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

    if publication_prefix.count("try {") != 1:
        errors.append("public release PATCH must have exactly one local guarded transaction opener")

    return errors


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    errors = validate(source)
    if errors:
        raise SystemExit("ERROR: V25 post-PATCH publish compensation guard failed:\n - " + "\n - ".join(errors))

    mutations = {
        "unguard public PATCH": source.replace(
            "try {\n            $publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri",
            "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri\n          try {",
        ),
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

    print("PASS: V25 publication ambiguity re-drafts the exact release, reconciles authority, and fails closed.")


if __name__ == "__main__":
    main()
