#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"
GENERATOR = ROOT / "scripts" / "new-v26-candidate-provenance.ps1"
VALIDATOR = ROOT / "scripts" / "assert-v26-candidate-identity.ps1"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def require(path: Path, tokens: list[str]) -> None:
    text = read(path)
    missing = [token for token in tokens if token not in text]
    if missing:
        raise SystemExit(
            f"{path.relative_to(ROOT)} missing V26 cloud installer provenance contract token(s): {missing}"
        )


workflow = read(WORKFLOW)
generator = read(GENERATOR)
validator = read(VALIDATOR)

require(
    WORKFLOW,
    [
        "BRICSCAD_V26_PINNED_MSI_SHA256:",
        "needs.installer-cache.outputs.msi_sha256",
        "new-v26-candidate-provenance.ps1",
        "assert-v26-candidate-identity.ps1",
        "-AdmittedScript '.\\scripts\\publish-v26-release.ps1'",
    ],
)
require(
    GENERATOR,
    [
        "[string]$InstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256",
        "$InstallerSha256 -cnotmatch '^[0-9a-f]{64}$'",
        "V26 admitted installer SHA-256 must be canonical lowercase 64-hex",
        "installerSha256 = $InstallerSha256",
        "InstallerSha256 = $InstallerSha256",
    ],
)
require(
    VALIDATOR,
    [
        "[string]$ExpectedInstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256",
        "$ExpectedInstallerSha256 -cnotmatch '^[0-9A-Fa-f]{64}$'",
        "$expectedInstallerSha256Canonical = $ExpectedInstallerSha256.ToLowerInvariant()",
        "Get-JsonPropertyOccurrenceCount",
        "-PropertyName 'installerSha256') -ne 1",
        "$installerSha256 = [string]$provenance.installerSha256",
        "$installerSha256 -cnotmatch '^[0-9a-f]{64}$'",
        "[string]::Equals($installerSha256, $expectedInstallerSha256Canonical, [StringComparison]::Ordinal)",
        "V26 candidate provenance installer digest mismatch",
        "InstallerSha256=$installerSha256",
    ],
)

# Admission must prove unique/canonical installer identity before publisher parsing/execution.
unique_check = validator.index("-PropertyName 'installerSha256') -ne 1")
installer_check = validator.index("$installerSha256 = [string]$provenance.installerSha256")
script_parse = validator.index("$admittedScriptBlock = $null")
script_exec = validator.index("& $admittedScriptBlock")
if not unique_check < installer_check < script_parse < script_exec:
    raise SystemExit("V26 installer provenance admission must precede admitted publisher parsing/execution")

# installer-cache remains the sole canonicalizing authority for candidate creation; release-time
# admission normalizes the already-admitted 64-hex environment value before exact comparison.
if workflow.count("needs.installer-cache.outputs.msi_sha256") < 2:
    raise SystemExit("V26 cloud workflow no longer carries the admitted installer digest across jobs")

mutations = {
    "generator environment binding": (generator, "[string]$InstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256"),
    "generator canonicality": (generator, "$InstallerSha256 -cnotmatch '^[0-9a-f]{64}$'"),
    "generator provenance field": (generator, "installerSha256 = $InstallerSha256"),
    "validator environment binding": (validator, "[string]$ExpectedInstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256"),
    "validator expected syntax": (validator, "$ExpectedInstallerSha256 -cnotmatch '^[0-9A-Fa-f]{64}$'"),
    "validator expected normalization": (validator, "$expectedInstallerSha256Canonical = $ExpectedInstallerSha256.ToLowerInvariant()"),
    "validator unique property admission": (validator, "-PropertyName 'installerSha256') -ne 1"),
    "validator provenance field": (validator, "$installerSha256 = [string]$provenance.installerSha256"),
    "validator canonicality": (validator, "$installerSha256 -cnotmatch '^[0-9a-f]{64}$'"),
    "validator exact digest equality": (validator, "[string]::Equals($installerSha256, $expectedInstallerSha256Canonical, [StringComparison]::Ordinal)"),
}
for label, (text, token) in mutations.items():
    if token not in text:
        raise SystemExit(f"mutation control unavailable: {label}")
    mutated = text.replace(token, "", 1)
    if token in mutated:
        raise SystemExit(f"mutation control did not remove guarded token: {label}")

print("PASS V26 cloud installer provenance source guard")
