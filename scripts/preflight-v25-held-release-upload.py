#!/usr/bin/env python3
"""Fail closed if V25 commercial draft upload leaves admitted local generations."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = (ROOT / ".github/workflows/release-v25.yml").read_text(encoding="utf-8")
UPLOADER = (ROOT / "scripts/invoke-v25-held-release-upload.ps1").read_text(encoding="utf-8")


def validate(workflow: str, uploader: str) -> list[str]:
    errors: list[str] = []
    required_uploader = (
        "[IO.File]::Open($item.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)",
        "[Security.Cryptography.SHA256]::Create()",
        "$digest = $sha.ComputeHash($stream)",
        "$stream.Position = 0",
        "[IO.FileAttributes]::ReparsePoint",
        "[System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)",
        "& gh release upload $ReleaseTag ([string]$asset.Path) --repo $Repository",
        "foreach ($asset in $held)",
        "if ([int64]$current.Length -ne [int64]$asset.Length)",
        "if ($null -ne $asset.Stream) { $asset.Stream.Dispose() }",
    )
    for token in required_uploader:
        if token not in uploader:
            errors.append(f"V25 held uploader missing token: {token}")

    required_workflow = (
        "scripts/invoke-v25-held-release-upload.ps1",
        "$admittedAssets = @(& .\\scripts\\invoke-v25-held-release-upload.ps1",
        "$localAssets[$asset.Name] = $asset",
        "$localLength = [int64]$LocalAssets[$expectedAsset].Length",
        "$localHash = [string]$localAssets[$name].Sha256",
    )
    for token in required_workflow:
        if token not in workflow:
            errors.append(f"V25 workflow missing held-upload binding: {token}")

    forbidden_workflow = (
        "& gh release upload $env:RELEASE_TAG $resolvedAsset --repo $env:GITHUB_REPOSITORY",
        "$localLength = [int64](Get-Item -LiteralPath ([string]$LocalAssets[$expectedAsset])).Length",
        "$localHash = (& .\\scripts\\verify-v25-held-file.ps1 -Operation Hash -Path (Join-Path $dist $name)).Trim()",
    )
    for token in forbidden_workflow:
        if token in workflow:
            errors.append(f"V25 workflow reopens or uploads an unheld local pathname: {token}")

    return errors


errors = validate(WORKFLOW, UPLOADER)
if errors:
    raise SystemExit("V25 held release upload preflight failed:\n - " + "\n - ".join(errors))

mutations = {
    "write sharing restored": UPLOADER.replace("[IO.FileShare]::Read)", "[IO.FileShare]::ReadWrite)", 1),
    "reparse rejection removed": UPLOADER.replace("[IO.FileAttributes]::ReparsePoint", "[IO.FileAttributes]::Hidden", 1),
    "held hash removed": UPLOADER.replace("$digest = $sha.ComputeHash($stream)", "$digest = [byte[]]::new(32)", 1),
    "upload leaves helper": UPLOADER.replace("& gh release upload $ReleaseTag ([string]$asset.Path) --repo $Repository", "Write-Host 'upload elsewhere'", 1),
}
for label, uploader in mutations.items():
    if not validate(WORKFLOW, uploader):
        raise SystemExit(f"V25 held release upload mutation escaped detection: {label}")

workflow_mutations = {
    "local hash pathname reopen": WORKFLOW.replace("$localHash = [string]$localAssets[$name].Sha256", "$localHash = (& .\\scripts\\verify-v25-held-file.ps1 -Operation Hash -Path (Join-Path $dist $name)).Trim()", 1),
    "local length pathname reopen": WORKFLOW.replace("$localLength = [int64]$LocalAssets[$expectedAsset].Length", "$localLength = [int64](Get-Item -LiteralPath ([string]$LocalAssets[$expectedAsset])).Length", 1),
}
for label, workflow in workflow_mutations.items():
    if not validate(workflow, UPLOADER):
        raise SystemExit(f"V25 held release upload workflow mutation escaped detection: {label}")

print("PASS V25 commercial draft upload and verification stay bound to held local generations")
