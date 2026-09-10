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
    already_draft = "$authoritativeCommercialReleaseBeforeCompensation.draft -eq $true"
    compensation_body = "$commercialCompensationBody = @{ draft = $true; prerelease = $true } | ConvertTo-Json"
    compensation_patch = "$commercialCompensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri"
    post_get = "$authoritativeCommercialReleaseAfterCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers"
    terminal = "V25 commercial publication safety invalidation was compensated to a non-public exact release"

    safety = source.find(safety_marker)
    reconciliation = source.find(reconciliation_marker, safety if safety >= 0 else 0)
    if safety < 0 or reconciliation < 0:
        return ["missing manual V25 post-publish safety-invalid reconciliation region"]

    region = source[reconciliation:]
    required = (
        (exact_uri, "immutable release-ID URI binding"),
        (pre_get, "authoritative pre-compensation GET"),
        ("$authoritativeCommercialReleaseBeforeCompensation.tag_name", "pre-compensation tag proof"),
        ("$authoritativeCommercialReleaseBeforeCompensation.target_commitish", "pre-compensation target proof"),
        ("$authoritativeCommercialReleaseBeforeCompensation.assets", "pre-compensation asset proof"),
        ("$verifiedAssetIds", "verified asset-ID baseline"),
        (already_draft, "already-draft no-mutation branch"),
        (compensation_body, "safe-state compensation payload"),
        (compensation_patch, "exact-release compensation PATCH"),
        (post_get, "authoritative post-compensation GET"),
        ("$authoritativeCommercialReleaseAfterCompensation.draft -ne $true", "non-public postcondition"),
        ("$authoritativeCommercialReleaseAfterCompensation.tag_name", "post-compensation tag proof"),
        ("$authoritativeCommercialReleaseAfterCompensation.target_commitish", "post-compensation target proof"),
        ("$authoritativeCommercialReleaseAfterCompensation.assets", "post-compensation asset proof"),
        (terminal, "terminal compensated-safety failure"),
    )
    for token, label in required:
        if token not in source if token == exact_uri else token not in region:
            errors.append(f"missing {label}: {token}")

    indexes = [region.find(token) for token in (pre_get, already_draft, compensation_body, compensation_patch, post_get, terminal)]
    if min(indexes) >= 0 and indexes != sorted(indexes):
        errors.append("commercial compensation must GET/prove, branch on already-draft, PATCH only then, GET/reconcile, then fail terminally")

    conditional_patch = re.compile(
        r"if\s*\(\s*\$authoritativeCommercialReleaseBeforeCompensation\.draft\s*-eq\s*\$true\s*\)\s*\{"
        r"(?:(?!\n\s*\}\s*else\s*\{).)*\n\s*\}\s*else\s*\{\s*"
        r"\$commercialCompensationBody\s*=\s*@\{\s*draft\s*=\s*\$true\s*;\s*prerelease\s*=\s*\$true\s*\}\s*\|\s*ConvertTo-Json.*?"
        r"\$commercialCompensatedRelease\s*=\s*Invoke-RestMethod\s+-Method\s+Patch\s+-Uri\s+\$releaseUri",
        re.DOTALL,
    )
    if not conditional_patch.search(region):
        errors.append("commercial compensation PATCH must be confined to the else branch of the authoritative already-draft check")

    if "Invoke-RestMethod -Method Delete -Uri $releaseUri" in region:
        errors.append("commercial safety compensation must be reversible re-draft, never destructive release deletion")

    pre = region.find(pre_get)
    patch = region.find(compensation_patch)
    if pre >= 0 and patch >= 0:
        before_patch = region[pre:patch]
        for token, label in (
            ("$authoritativeCommercialReleaseBeforeCompensation.tag_name", "tag"),
            ("$env:RELEASE_TAG", "expected tag"),
            ("$authoritativeCommercialReleaseBeforeCompensation.target_commitish", "target"),
            ("$env:GITHUB_SHA", "expected target"),
            ("$authoritativeCommercialReleaseBeforeCompensation.assets", "assets"),
            ("$verifiedAssetIds", "verified asset IDs"),
        ):
            if token not in before_patch:
                errors.append(f"pre-compensation {label} proof must precede mutation")
    return errors


def self_test() -> list[str]:
    exact_uri = '$releaseUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/$releaseId"'
    safe = "\n".join([
        exact_uri,
        "$publicationSafetyKnownInvalid = $true",
        "if ($reconciledRelease.draft -eq $false) {",
        "  $authoritativeCommercialReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        "  if (-not [string]::Equals([string]$authoritativeCommercialReleaseBeforeCompensation.tag_name, $env:RELEASE_TAG, [StringComparison]::Ordinal)) { throw 'tag' }",
        "  if (-not [string]::Equals([string]$authoritativeCommercialReleaseBeforeCompensation.target_commitish, $env:GITHUB_SHA, [StringComparison]::OrdinalIgnoreCase)) { throw 'target' }",
        "  $preAssets = @($authoritativeCommercialReleaseBeforeCompensation.assets)",
        "  if ($preAssets.Count -ne $verifiedAssetIds.Count) { throw 'assets' }",
        "  if ($authoritativeCommercialReleaseBeforeCompensation.draft -eq $true) {",
        "    Write-Host 'already safe'",
        "  } else {",
        "    $commercialCompensationBody = @{ draft = $true; prerelease = $true } | ConvertTo-Json",
        "    $commercialCompensatedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri -Headers $headers -Body $commercialCompensationBody",
        "  }",
        "  $authoritativeCommercialReleaseAfterCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        "  if ($authoritativeCommercialReleaseAfterCompensation.draft -ne $true) { throw 'public' }",
        "  if ($authoritativeCommercialReleaseAfterCompensation.tag_name -ne $env:RELEASE_TAG) { throw 'tag2' }",
        "  if ($authoritativeCommercialReleaseAfterCompensation.target_commitish -ne $env:GITHUB_SHA) { throw 'target2' }",
        "  $postAssets = @($authoritativeCommercialReleaseAfterCompensation.assets)",
        "  if ($postAssets.Count -ne $verifiedAssetIds.Count) { throw 'assets2' }",
        "  throw 'V25 commercial publication safety invalidation was compensated to a non-public exact release'",
        "}",
    ])
    errors: list[str] = []
    if contract_errors(safe):
        errors.append("guard rejected intended exact pre-proof/conditional-redraft/post-proof contract")
    mutations = {
        "unconditional patch": safe.replace("  } else {\n    $commercialCompensationBody", "  }\n  $commercialCompensationBody", 1),
        "drop pre GET": safe.replace("$authoritativeCommercialReleaseBeforeCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", "$authoritativeCommercialReleaseBeforeCompensation = $reconciledRelease", 1),
        "drop post GET": safe.replace("$authoritativeCommercialReleaseAfterCompensation = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", "$authoritativeCommercialReleaseAfterCompensation = $commercialCompensatedRelease", 1),
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
    print("PASS: manual V25 post-publish safety invalidation is compensated to an exact non-public release before terminal failure.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
