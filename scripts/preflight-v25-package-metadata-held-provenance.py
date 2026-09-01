#!/usr/bin/env python3
"""Fail closed if V25 post-package provenance returns to mutable pathname reads."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = (ROOT / "scripts/assert-v25-release-package-identity.ps1").read_text(encoding="utf-8")
PACKAGER = (ROOT / "scripts/package-v25-release.ps1").read_text(encoding="utf-8")
WORKFLOW = (ROOT / ".github/workflows/release-v25.yml").read_text(encoding="utf-8")


def validate(validator: str, packager: str, workflow: str) -> list[str]:
    errors: list[str] = []
    for token in (
        "[IO.FileShare]::Read\n    )",
        "[IO.FileAttributes]::ReparsePoint",
        "$script:MaxMetadataBytes = 65536",
        "[Text.UTF8Encoding]::new($false, $true)",
        "Read-HeldStrictUtf8Metadata",
        "Assert-HeldMetadataBinding -Held $held",
        "ExpectedSourceCommit -notmatch '^[0-9A-Fa-f]{40}$'",
        "([string]$metadata.gitCommit).Trim()",
        "does not match expected source commit",
        "('v' + $productVersion)",
        "does not exactly match source product version",
    ):
        if token not in validator:
            errors.append(f"V25 identity validator missing held-provenance token: {token}")

    if "[IO.FileShare]::ReadWrite" in validator or "[IO.FileShare]::Write" in validator:
        errors.append("V25 package metadata held handle must not share write access")
    if "Get-Content -LiteralPath $MetadataPath" in validator:
        errors.append("V25 identity validator must not admit semantic metadata through a raw pathname Get-Content read")

    for token in (
        "assert-v25-release-package-identity.ps1",
        "-MetadataPath $metadataPath -ExpectedSourceCommit $headBefore",
        "Admitted V25 package source commit",
    ):
        if token not in packager:
            errors.append(f"V25 release packager missing canonical identity-validator token: {token}")
    if "Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json" in packager:
        errors.append("V25 release packager must not duplicate a raw-path semantic metadata read")

    for token in (
        "assert-v25-release-package-identity.ps1",
        "-MetadataPath 'dist\\QS3D-BricsCAD-V25\\PACKAGE-METADATA.json'",
        "-ExpectedSourceCommit $env:GITHUB_SHA",
        "-ExpectedReleaseTag $env:RELEASE_TAG",
    ):
        if token not in workflow:
            errors.append(f"V25 commercial release workflow missing final held identity token: {token}")
    if "Get-Content -LiteralPath 'dist\\QS3D-BricsCAD-V25\\PACKAGE-METADATA.json' -Raw | ConvertFrom-Json" in workflow:
        errors.append("V25 commercial release workflow must not re-admit metadata through a raw pathname read")

    return errors


errors = validate(VALIDATOR, PACKAGER, WORKFLOW)
if errors:
    raise SystemExit("V25 held metadata provenance preflight failed:\n - " + "\n - ".join(errors))

mutations = {
    "validator loses held read sharing": (VALIDATOR.replace("[IO.FileShare]::Read\n    )", "[IO.FileShare]::ReadWrite\n    )", 1), PACKAGER, WORKFLOW),
    "validator loses strict UTF8": (VALIDATOR.replace("[Text.UTF8Encoding]::new($false, $true)", "[Text.UTF8Encoding]::new($false)", 1), PACKAGER, WORKFLOW),
    "validator ignores metadata source": (VALIDATOR.replace("([string]$metadata.gitCommit).Trim()", "([string]$ExpectedSourceCommit).Trim()", 1), PACKAGER, WORKFLOW),
    "packager bypasses validator": (VALIDATOR, PACKAGER.replace("assert-v25-release-package-identity.ps1", "legacy-v25-metadata-read.ps1", 1), WORKFLOW),
    "workflow omits exact source binding": (VALIDATOR, PACKAGER, WORKFLOW.replace("-ExpectedSourceCommit $env:GITHUB_SHA", "-ExpectedSourceCommit $env:RELEASE_TAG", 1)),
    "workflow omits release tag binding": (VALIDATOR, PACKAGER, WORKFLOW.replace("-ExpectedReleaseTag $env:RELEASE_TAG", "-ExpectedReleaseTag $env:GITHUB_SHA", 1)),
}
for label, (validator, packager, workflow) in mutations.items():
    if not validate(validator, packager, workflow):
        raise SystemExit(f"V25 held metadata provenance mutation escaped detection: {label}")

print("PASS V25 held-generation package metadata provenance admission")
