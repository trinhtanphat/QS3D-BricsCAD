#!/usr/bin/env python3
"""Fail closed unless V25 publication ambiguity proves exact identity before compensation."""

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def validate(source: str) -> list[str]:
    errors: list[str] = []

    release_uri = '$releaseUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/$($release.id)"'
    publish_body = "$publishBody = @{ draft = $false } | ConvertTo-Json"
    publish = "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri"
    release_uri_index = source.find(release_uri)
    publish_body_index = source.find(publish_body)
    publish_index = source.find(publish)
    if min(release_uri_index, publish_body_index, publish_index) < 0 or not (release_uri_index < publish_body_index < publish_index):
        return ["missing exact created-release URI binding before ordered V25 public release PATCH"]

    publication_prefix = source[publish_body_index:publish_index]
    try_index_absolute = source.find("try {", publish_body_index, publish_index)
    if try_index_absolute < 0:
        errors.append("public release PATCH is not inside the guarded publication transaction")

    transaction = source[try_index_absolute if try_index_absolute >= 0 else publish_index:]
    catch_index = transaction.find("catch {")
    if catch_index < 0:
        return errors + ["missing publication compensation catch"]
    catch_tail = transaction[catch_index:]

    pre_get = "$authoritativeReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers"
    pre_tag = "$authoritativeReleaseBeforeCompensation.tag_name"
    pre_target = "$authoritativeReleaseBeforeCompensation.target_commitish"
    pre_prerelease = "$authoritativeReleaseBeforeCompensation.prerelease -ne $true"
    pre_assets = "$authoritativeReleaseBeforeCompensation.assets"
    pre_ids = "$releaseBeforeCompensationAssetIds"
    already_draft = "$authoritativeReleaseBeforeCompensation.draft -eq $true"
    compensation_body = "$compensationBody = @{ draft = $true; prerelease = $true } | ConvertTo-Json"
    compensation_patch = "$compensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri"
    reconcile_get = "$authoritativeCompensatedRelease = Invoke-RestMethod -Method Get -Uri $releaseUri"
    terminal = "throw \"V25 release publication safety validation failed after publish PATCH"
    post_main = "$publishMainAfterResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\""

    required = (
        (pre_get, "authoritative pre-compensation GET"),
        (pre_tag, "pre-compensation tag proof"),
        ("$env:RELEASE_TAG", "expected release tag"),
        (pre_target, "pre-compensation target proof"),
        ("$env:RELEASE_COMMIT_SHA", "expected release target"),
        (pre_prerelease, "pre-compensation prerelease proof"),
        (pre_assets, "pre-compensation asset-set proof"),
        (pre_ids, "pre-compensation asset-ID proof"),
        ("$verifiedReleaseAssetIds", "verified asset identity baseline"),
        (already_draft, "already-draft compensation bypass"),
        (compensation_body, "safe-state compensation payload"),
        (compensation_patch, "exact-release compensation PATCH"),
        (reconcile_get, "authoritative post-compensation GET"),
        ("$authoritativeCompensatedRelease.draft -ne $true", "non-public draft postcondition"),
        ("$authoritativeCompensatedRelease.prerelease -ne $true", "prerelease postcondition"),
        (post_main, "post-PATCH protected-main revalidation"),
        ("$publishMainAfter -ne $publishMain", "post-PATCH protected-main identity comparison"),
        (terminal, "fail-closed terminal error after compensation"),
    )
    for token, label in required:
        if token not in transaction:
            errors.append(f"missing {label}: {token}")

    publish_in_transaction = transaction.find(publish)
    post_main_index = transaction.find(post_main)
    pre_get_index = transaction.find(pre_get, catch_index)
    pre_tag_index = transaction.find(pre_tag, catch_index)
    pre_target_index = transaction.find(pre_target, catch_index)
    pre_assets_index = transaction.find(pre_assets, catch_index)
    pre_ids_index = transaction.find(pre_ids, catch_index)
    already_draft_index = transaction.find(already_draft, catch_index)
    compensation_body_index = transaction.find(compensation_body, catch_index)
    compensate_index = transaction.find(compensation_patch, catch_index)
    reconcile_index = transaction.find(reconcile_get, catch_index)
    terminal_index = transaction.find(terminal, catch_index)

    ordered = (
        publish_in_transaction,
        post_main_index,
        catch_index,
        pre_get_index,
        pre_tag_index,
        pre_target_index,
        pre_assets_index,
        pre_ids_index,
        already_draft_index,
        compensation_body_index,
        compensate_index,
        reconcile_index,
        terminal_index,
    )
    if min(ordered) >= 0 and list(ordered) != sorted(ordered):
        errors.append(
            "publication compensation must publish, revalidate main, catch, GET/prove exact release identity, "
            "recognize already-draft state, PATCH only then, reconcile, and fail closed"
        )

    if compensate_index >= 0:
        before_patch = transaction[catch_index:compensate_index]
        for token, label in (
            (pre_get, "GET"),
            (pre_tag, "tag"),
            (pre_target, "target"),
            (pre_prerelease, "prerelease"),
            (pre_assets, "assets"),
            (pre_ids, "asset IDs"),
            ("$verifiedReleaseAssetIds", "verified asset IDs"),
            (already_draft, "already-draft branch"),
        ):
            if token not in before_patch:
                errors.append(f"pre-compensation {label} proof must precede compensation PATCH")

    # Already-draft state must be the no-mutation branch and compensation PATCH must be confined
    # to the matching else block. Merely mentioning an already-draft check before an unconditional
    # PATCH would still mutate a resource that is already in the desired safe state.
    conditional_patch = re.compile(
        r"if\s*\(\s*\$authoritativeReleaseBeforeCompensation\.draft\s*-eq\s*\$true\s*\)\s*\{"
        r"(?:(?!\n\s*\}\s*else\s*\{).)*\n\s*\}\s*else\s*\{\s*"
        r"\$compensationBody\s*=\s*@\{\s*draft\s*=\s*\$true\s*;\s*prerelease\s*=\s*\$true\s*\}\s*\|\s*ConvertTo-Json\s*"
        r"\$compensatedRelease\s*=\s*Invoke-RestMethod\s+-Method\s+Patch\s+-Uri\s+\$releaseUri",
        re.DOTALL,
    )
    if not conditional_patch.search(catch_tail):
        errors.append("compensation PATCH must be confined to the else branch of an authoritative already-draft check")

    post_identity_tokens = (
        "$authoritativeCompensatedRelease.tag_name",
        "$env:RELEASE_TAG",
        "$authoritativeCompensatedRelease.target_commitish",
        "$env:RELEASE_COMMIT_SHA",
        "$authoritativeCompensatedRelease.assets",
        "$verifiedReleaseAssetIds",
    )
    for token in post_identity_tokens:
        if token not in catch_tail:
            errors.append(f"post-compensation reconciliation does not preserve exact release identity: {token}")

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
        "drop exact release URI binding": source.replace(
            '$releaseUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/$($release.id)"',
            '$releaseUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/latest"',
        ),
        "drop pre-compensation GET": source.replace(
            "$authoritativeReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
            "$authoritativeReleaseBeforeCompensation = $publishedRelease",
        ),
        "move compensation before proof": source.replace(
            "$authoritativeReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
            "$compensationBody = @{ draft = $true; prerelease = $true } | ConvertTo-Json\n              $compensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri -Headers $headers -ContentType 'application/json' -Body $compensationBody\n              $authoritativeReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
            1,
        ),
        "make compensation unconditional": source.replace(
            "              else {\n                $compensationBody",
            "              }\n              $compensationBody",
            1,
        ),
        "drop pre-compensation asset IDs": source.replace(
            "$releaseBeforeCompensationAssetIds",
            "$unverifiedReleaseBeforeCompensationAssetIds",
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

    print("PASS: V25 publication ambiguity proves exact created-release identity before mutation, re-drafts only when required, reconciles authority, and fails closed.")


if __name__ == "__main__":
    main()
