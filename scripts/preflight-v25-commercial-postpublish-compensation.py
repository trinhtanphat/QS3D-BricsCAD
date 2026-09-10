#!/usr/bin/env python3
"""Fail closed unless manual V25 publication safety failure is compensated non-public."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"


def contract_errors(source: str) -> list[str]:
    errors: list[str] = []
    safety_marker = "$publicationSafetyKnownInvalid = $true"
    reconciliation_marker = "if ($reconciledRelease.draft -eq $false)"
    exact_uri = '$releaseUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/$releaseId"'
    pre_get = "$authoritativeCommercialReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers"
    pre_release_id_proof = "[long]$authoritativeCommercialReleaseBeforeCompensation.id -ne $releaseId"
    pre_url_proof = "[string]$authoritativeCommercialReleaseBeforeCompensation.url, $releaseUri"
    pre_name_proof = "[string]$authoritativeCommercialReleaseBeforeCompensation.name, $expectedReleaseName"
    pre_body_proof = "([string]$authoritativeCommercialReleaseBeforeCompensation.body).IndexOf($draftTransactionMarker"
    pre_asset_id_proof = "[long]$preMatches[0].id -ne $expectedAssetId"
    pre_asset_size_proof = "[int64]$preMatches[0].size -ne [int64]$localAssets[$expectedAsset].Length"
    unambiguous_state = "$authoritativeCommercialReleaseBeforeCompensation.draft -ne $true -and $authoritativeCommercialReleaseBeforeCompensation.draft -ne $false"
    already_draft = "$authoritativeCommercialReleaseBeforeCompensation.draft -eq $true"
    compensation_body = "$commercialCompensationBody = @{ draft = $true; prerelease = $true } | ConvertTo-Json"
    compensation_patch = "$commercialCompensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri"
    post_get = "$authoritativeCommercialReleaseAfterCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers"
    post_release_id_proof = "[long]$authoritativeCommercialReleaseAfterCompensation.id -ne $releaseId"
    post_url_proof = "[string]$authoritativeCommercialReleaseAfterCompensation.url, $releaseUri"
    post_name_proof = "[string]$authoritativeCommercialReleaseAfterCompensation.name, $expectedReleaseName"
    post_body_proof = "([string]$authoritativeCommercialReleaseAfterCompensation.body).IndexOf($draftTransactionMarker"
    post_asset_id_proof = "[long]$postMatches[0].id -ne $expectedAssetId"
    post_asset_size_proof = "[int64]$postMatches[0].size -ne [int64]$localAssets[$expectedAsset].Length"
    terminal = "V25 commercial publication safety invalidation was compensated to a non-public exact release"

    safety = source.find(safety_marker)
    reconciliation = source.find(reconciliation_marker, safety if safety >= 0 else 0)
    if safety < 0 or reconciliation < 0:
        return ["missing manual V25 post-publish safety-invalid reconciliation region"]

    region = source[reconciliation:]
    required = (
        (exact_uri, "immutable release-ID URI binding"),
        (pre_get, "authoritative pre-compensation GET"),
        (pre_release_id_proof, "pre-compensation immutable release-ID proof"),
        (pre_url_proof, "pre-compensation repository release-URL proof"),
        ("$authoritativeCommercialReleaseBeforeCompensation.tag_name", "pre-compensation tag proof"),
        ("$authoritativeCommercialReleaseBeforeCompensation.target_commitish", "pre-compensation target proof"),
        (pre_name_proof, "pre-compensation release-name proof"),
        (pre_body_proof, "pre-compensation transaction-marker proof"),
        ("$authoritativeCommercialReleaseBeforeCompensation.assets", "pre-compensation asset proof"),
        ("$verifiedAssetIds", "verified asset-ID baseline"),
        (pre_asset_id_proof, "pre-compensation exact asset-ID proof"),
        (pre_asset_size_proof, "pre-compensation exact asset-size proof"),
        (unambiguous_state, "explicit ambiguous draft-state rejection"),
        (already_draft, "already-draft no-mutation branch"),
        (compensation_body, "safe-state compensation payload"),
        (compensation_patch, "exact-release compensation PATCH"),
        (post_get, "authoritative post-compensation GET"),
        (post_release_id_proof, "post-compensation immutable release-ID proof"),
        (post_url_proof, "post-compensation repository release-URL proof"),
        ("$authoritativeCommercialReleaseAfterCompensation.draft -ne $true", "non-public postcondition"),
        ("$authoritativeCommercialReleaseAfterCompensation.tag_name", "post-compensation tag proof"),
        ("$authoritativeCommercialReleaseAfterCompensation.target_commitish", "post-compensation target proof"),
        (post_name_proof, "post-compensation release-name proof"),
        (post_body_proof, "post-compensation transaction-marker proof"),
        ("$authoritativeCommercialReleaseAfterCompensation.assets", "post-compensation asset proof"),
        (post_asset_id_proof, "post-compensation exact asset-ID proof"),
        (post_asset_size_proof, "post-compensation exact asset-size proof"),
        (terminal, "terminal compensated-safety failure"),
    )
    for token, label in required:
        haystack = source if token == exact_uri else region
        if token not in haystack:
            errors.append(f"missing {label}: {token}")

    indexes = [
        region.find(token)
        for token in (
            pre_get,
            pre_release_id_proof,
            pre_url_proof,
            pre_name_proof,
            pre_body_proof,
            pre_asset_id_proof,
            pre_asset_size_proof,
            unambiguous_state,
            already_draft,
            compensation_body,
            compensation_patch,
            post_get,
            post_release_id_proof,
            post_url_proof,
            post_name_proof,
            post_body_proof,
            post_asset_id_proof,
            post_asset_size_proof,
            terminal,
        )
    ]
    if min(indexes) >= 0 and indexes != sorted(indexes):
        errors.append(
            "commercial compensation must GET/prove exact release+transaction+asset identity, reject ambiguous draft state, branch on already-draft, PATCH only then, GET/reconcile exact identity, then fail terminally"
        )

    conditional_patch = re.compile(
        r"if\s*\(\s*\$authoritativeCommercialReleaseBeforeCompensation\.draft\s*-eq\s*\$true\s*\)\s*\{"
        r"(?:(?!\n\s*\}\s*else\s*\{).)*\n\s*\}\s*else\s*\{\s*"
        r"\$commercialCompensationBody\s*=\s*@\{\s*draft\s*=\s*\$true\s*;\s*prerelease\s*=\s*\$true\s*\}\s*\|\s*ConvertTo-Json.*?"
        r"\$commercialCompensatedRelease\s*=\s*Invoke-RestMethod\s+-Method\s+Patch\s+-Uri\s+\$releaseUri",
        re.DOTALL,
    )
    if not conditional_patch.search(region):
        errors.append(
            "commercial compensation PATCH must be confined to the false branch of the authoritative already-draft check"
        )

    ambiguous_index = region.find(unambiguous_state)
    branch_index = region.find(already_draft)
    if ambiguous_index >= 0 and branch_index >= 0 and ambiguous_index > branch_index:
        errors.append("ambiguous draft-state rejection must occur before the already-draft/PATCH branch")

    if "Invoke-RestMethod -Method Delete -Uri $releaseUri" in region:
        errors.append("commercial safety compensation must be reversible re-draft, never destructive release deletion")

    pre = region.find(pre_get)
    patch = region.find(compensation_patch)
    if pre >= 0 and patch >= 0:
        before_patch = region[pre:patch]
        for token, label in (
            (pre_release_id_proof, "immutable release ID"),
            (pre_url_proof, "repository release URL"),
            ("$authoritativeCommercialReleaseBeforeCompensation.tag_name", "tag"),
            ("$env:RELEASE_TAG", "expected tag"),
            ("$authoritativeCommercialReleaseBeforeCompensation.target_commitish", "target"),
            ("$env:GITHUB_SHA", "expected target"),
            (pre_name_proof, "release name"),
            (pre_body_proof, "transaction marker"),
            ("$authoritativeCommercialReleaseBeforeCompensation.assets", "assets"),
            ("$verifiedAssetIds", "verified asset IDs"),
            (pre_asset_id_proof, "exact remote asset IDs"),
            (pre_asset_size_proof, "exact remote asset sizes"),
            (unambiguous_state, "unambiguous draft state"),
        ):
            if token not in before_patch:
                errors.append(f"pre-compensation {label} proof must precede mutation")

    post = region.find(post_get)
    terminal_index = region.find(terminal)
    if post >= 0 and terminal_index >= 0:
        after_patch = region[post:terminal_index]
        for token, label in (
            (post_release_id_proof, "immutable release ID"),
            (post_url_proof, "repository release URL"),
            (post_name_proof, "release name"),
            (post_body_proof, "transaction marker"),
            ("$authoritativeCommercialReleaseAfterCompensation.assets", "assets"),
            ("$verifiedAssetIds", "verified asset IDs"),
            (post_asset_id_proof, "exact remote asset IDs"),
            (post_asset_size_proof, "exact remote asset sizes"),
        ):
            if token not in after_patch:
                errors.append(f"post-compensation {label} proof must precede terminal failure")
    return errors


def self_test() -> list[str]:
    exact_uri = '$releaseUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/$releaseId"'
    pre_release_id_proof = "[long]$authoritativeCommercialReleaseBeforeCompensation.id -ne $releaseId"
    pre_url_proof = "[string]$authoritativeCommercialReleaseBeforeCompensation.url, $releaseUri"
    pre_name_proof = "[string]$authoritativeCommercialReleaseBeforeCompensation.name, $expectedReleaseName"
    pre_body_proof = "([string]$authoritativeCommercialReleaseBeforeCompensation.body).IndexOf($draftTransactionMarker"
    pre_asset_id_proof = "[long]$preMatches[0].id -ne $expectedAssetId"
    pre_asset_size_proof = "[int64]$preMatches[0].size -ne [int64]$localAssets[$expectedAsset].Length"
    post_release_id_proof = "[long]$authoritativeCommercialReleaseAfterCompensation.id -ne $releaseId"
    post_url_proof = "[string]$authoritativeCommercialReleaseAfterCompensation.url, $releaseUri"
    post_name_proof = "[string]$authoritativeCommercialReleaseAfterCompensation.name, $expectedReleaseName"
    post_body_proof = "([string]$authoritativeCommercialReleaseAfterCompensation.body).IndexOf($draftTransactionMarker"
    post_asset_id_proof = "[long]$postMatches[0].id -ne $expectedAssetId"
    post_asset_size_proof = "[int64]$postMatches[0].size -ne [int64]$localAssets[$expectedAsset].Length"
    unambiguous_state = "$authoritativeCommercialReleaseBeforeCompensation.draft -ne $true -and $authoritativeCommercialReleaseBeforeCompensation.draft -ne $false"
    safe = "\n".join([
        exact_uri,
        "$publicationSafetyKnownInvalid = $true",
        "if ($reconciledRelease.draft -eq $false) {",
        "  $authoritativeCommercialReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        f"  if ({pre_release_id_proof}) {{ throw 'release-id' }}",
        f"  if (-not [string]::Equals({pre_url_proof}, [StringComparison]::Ordinal)) {{ throw 'url' }}",
        "  if (-not [string]::Equals([string]$authoritativeCommercialReleaseBeforeCompensation.tag_name, $env:RELEASE_TAG, [StringComparison]::Ordinal)) { throw 'tag' }",
        "  if (-not [string]::Equals([string]$authoritativeCommercialReleaseBeforeCompensation.target_commitish, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase)) { throw 'target' }",
        f"  if (-not [string]::Equals({pre_name_proof}, [StringComparison]::Ordinal)) {{ throw 'name' }}",
        f"  if ({pre_body_proof}, [StringComparison]::Ordinal) -lt 0) {{ throw 'body' }}",
        "  $preAssets = @($authoritativeCommercialReleaseBeforeCompensation.assets)",
        "  if ($preAssets.Count -ne $verifiedAssetIds.Count) { throw 'assets' }",
        f"  if ({pre_asset_id_proof} -or {pre_asset_size_proof}) {{ throw 'asset' }}",
        f"  if ({unambiguous_state}) {{ throw 'state' }}",
        "  if ($authoritativeCommercialReleaseBeforeCompensation.draft -eq $true) {",
        "    Write-Host 'already safe'",
        "  } else {",
        "    $commercialCompensationBody = @{ draft = $true; prerelease = $true } | ConvertTo-Json",
        "    $commercialCompensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri -Headers $headers -Body $commercialCompensationBody",
        "  }",
        "  $authoritativeCommercialReleaseAfterCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        f"  if ({post_release_id_proof}) {{ throw 'release-id2' }}",
        f"  if (-not [string]::Equals({post_url_proof}, [StringComparison]::Ordinal)) {{ throw 'url2' }}",
        "  if ($authoritativeCommercialReleaseAfterCompensation.draft -ne $true) { throw 'public' }",
        "  if ($authoritativeCommercialReleaseAfterCompensation.tag_name -ne $env:RELEASE_TAG) { throw 'tag2' }",
        "  if ($authoritativeCommercialReleaseAfterCompensation.target_commitish -ne $env:GITHUB_SHA) { throw 'target2' }",
        f"  if (-not [string]::Equals({post_name_proof}, [StringComparison]::Ordinal)) {{ throw 'name2' }}",
        f"  if ({post_body_proof}, [StringComparison]::Ordinal) -lt 0) {{ throw 'body2' }}",
        "  $postAssets = @($authoritativeCommercialReleaseAfterCompensation.assets)",
        "  if ($postAssets.Count -ne $verifiedAssetIds.Count) { throw 'assets2' }",
        f"  if ({post_asset_id_proof} -or {post_asset_size_proof}) {{ throw 'asset2' }}",
        "  throw 'V25 commercial publication safety invalidation was compensated to a non-public exact release'",
        "}",
    ])
    errors: list[str] = []
    if contract_errors(safe):
        errors.append("guard rejected intended exact pre-proof/ambiguous-state/conditional-redraft/post-proof contract")
    mutations = {
        "unconditional patch": safe.replace("  } else {\n    $commercialCompensationBody", "  }\n  $commercialCompensationBody", 1),
        "drop pre GET": safe.replace("$authoritativeCommercialReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", "$authoritativeCommercialReleaseBeforeCompensation = $reconciledRelease", 1),
        "drop pre release ID proof": safe.replace(f"  if ({pre_release_id_proof}) {{ throw 'release-id' }}\n", "", 1),
        "drop pre release URL proof": safe.replace(f"  if (-not [string]::Equals({pre_url_proof}, [StringComparison]::Ordinal)) {{ throw 'url' }}\n", "", 1),
        "drop pre release name proof": safe.replace(f"  if (-not [string]::Equals({pre_name_proof}, [StringComparison]::Ordinal)) {{ throw 'name' }}\n", "", 1),
        "drop pre transaction proof": safe.replace(f"  if ({pre_body_proof}, [StringComparison]::Ordinal) -lt 0) {{ throw 'body' }}\n", "", 1),
        "drop pre asset ID proof": safe.replace(pre_asset_id_proof, "$false", 1),
        "drop pre asset size proof": safe.replace(pre_asset_size_proof, "$false", 1),
        "drop post GET": safe.replace("$authoritativeCommercialReleaseAfterCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", "$authoritativeCommercialReleaseAfterCompensation = $commercialCompensatedRelease", 1),
        "drop post release ID proof": safe.replace(f"  if ({post_release_id_proof}) {{ throw 'release-id2' }}\n", "", 1),
        "drop post release URL proof": safe.replace(f"  if (-not [string]::Equals({post_url_proof}, [StringComparison]::Ordinal)) {{ throw 'url2' }}\n", "", 1),
        "drop post release name proof": safe.replace(f"  if (-not [string]::Equals({post_name_proof}, [StringComparison]::Ordinal)) {{ throw 'name2' }}\n", "", 1),
        "drop post transaction proof": safe.replace(f"  if ({post_body_proof}, [StringComparison]::Ordinal) -lt 0) {{ throw 'body2' }}\n", "", 1),
        "drop post asset ID proof": safe.replace(post_asset_id_proof, "$false", 1),
        "drop post asset size proof": safe.replace(post_asset_size_proof, "$false", 1),
        "drop ambiguous state rejection": safe.replace(f"  if ({unambiguous_state}) {{ throw 'state' }}\n", "", 1),
        "delete instead": safe.replace("$commercialCompensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri", "$commercialCompensatedRelease = Invoke-RestMethod -Method Delete -Uri $releaseUri", 1),
    }
    for label, mutated in mutations.items():
        if not contract_errors(mutated):
            errors.append(f"guard survived mutation: {label}")
    return errors


def main() -> int:
    errors = self_test()
    if not WORKFLOW.is_file():
        errors.append("missing manual V25 commercial release workflow")
    else:
        try:
            errors.extend(contract_errors(WORKFLOW.read_text(encoding="utf-8")))
        except (OSError, UnicodeError) as exc:
            errors.append(f"could not inspect manual V25 workflow: {exc}")
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1
    print("PASS: manual V25 post-publish safety invalidation is exact-release/transaction/asset bound, ambiguity-safe, conditionally re-drafted, and authoritatively proved non-public before terminal failure.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
