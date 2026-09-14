#!/usr/bin/env python3
from pathlib import Path
import re

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


def assert_validator_contract(text: str) -> None:
    required_tokens = [
        "[string]$ExpectedInstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256",
        "$ExpectedInstallerSha256 -cnotmatch '^[0-9A-Fa-f]{64}$'",
        "$expectedInstallerSha256Canonical = $ExpectedInstallerSha256.ToLowerInvariant()",
        "Get-JsonPropertyOccurrenceCount",
        "$installerSha256 = [string]$provenance.installerSha256",
        "$installerSha256 -cnotmatch '^[0-9a-f]{64}$'",
        "[string]::Equals($installerSha256, $expectedInstallerSha256Canonical, [StringComparison]::Ordinal)",
        "V26 candidate provenance installer digest mismatch",
        "InstallerSha256=$installerSha256",
    ]
    missing = [token for token in required_tokens if token not in text]
    if missing:
        raise SystemExit(f"validator missing V26 installer provenance contract token(s): {missing}")

    set_match = re.search(r"\$provenanceExpectedPropertyCounts\s*=\s*@\{([^}]*)\}", text)
    if set_match is None:
        raise SystemExit("V26 installer provenance admission no longer has the path-scoped provenance property-count set")
    set_keys = tuple(re.findall(r"([A-Za-z][A-Za-z0-9]*)\s*=\s*1\b", set_match.group(1)))
    if "installerSha256" not in set_keys:
        raise SystemExit("installerSha256 must remain a member of the provenance property-count set")

    parse_token = "$provenance = $provenanceText | ConvertFrom-Json -ErrorAction Stop"
    parse_at = text.find(parse_token, set_match.start())
    assert_token = "Assert-JsonPropertyCounts -JsonText $provenanceText -ExpectedPropertyCounts $provenanceExpectedPropertyCounts"
    assert_at = text.find(assert_token, set_match.start(), parse_at if parse_at >= 0 else len(text))
    if parse_at < 0 or assert_at < 0 or not set_match.start() < assert_at < parse_at:
        raise SystemExit("V26 installer provenance property-count admission must run before provenance JSON parsing")

    installer_check = text.find("$installerSha256 = [string]$provenance.installerSha256", parse_at)
    script_parse = text.find("$admittedScriptBlock = $null", installer_check)
    script_exec = text.find("& $admittedScriptBlock", script_parse)
    if min(installer_check, script_parse, script_exec) < 0 or not set_match.start() < assert_at < parse_at < installer_check < script_parse < script_exec:
        raise SystemExit("V26 installer provenance uniqueness admission must precede extraction and publisher parsing/execution")


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
assert_validator_contract(validator)

# installer-cache remains the sole canonicalizing authority for candidate creation; release-time
# admission normalizes the already-admitted 64-hex environment value before exact comparison.
if workflow.count("needs.installer-cache.outputs.msi_sha256") < 2:
    raise SystemExit("V26 cloud workflow no longer carries the admitted installer digest across jobs")

# Adversarial self-tests prove this guard is semantic rather than satisfiable by decoy tokens.
set_match = re.search(r"\$provenanceExpectedPropertyCounts\s*=\s*@\{([^}]*)\}", validator)
if set_match is None:
    raise SystemExit("mutation control unavailable: provenance property-count set")
set_text = set_match.group(0)
without_installer = re.sub(r"installerSha256\s*=\s*1\s*;?", "", set_text, count=1)
if without_installer == set_text:
    raise SystemExit("mutation control unavailable: installerSha256 property-count membership")
try:
    assert_validator_contract(validator[:set_match.start()] + without_installer + validator[set_match.end():])
except SystemExit:
    pass
else:
    raise SystemExit("V26 installer provenance guard accepted property-count set without installerSha256")

assert_token = "Assert-JsonPropertyCounts -JsonText $provenanceText -ExpectedPropertyCounts $provenanceExpectedPropertyCounts"
mutated_assert = validator.replace(assert_token, "# removed provenance property-count enforcement", 1)
try:
    assert_validator_contract(mutated_assert)
except SystemExit:
    pass
else:
    raise SystemExit("V26 installer provenance guard accepted provenance parsing without property-count enforcement")

mutations = {
    "generator environment binding": (generator, "[string]$InstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256"),
    "generator canonicality": (generator, "$InstallerSha256 -cnotmatch '^[0-9a-f]{64}$'"),
    "generator provenance field": (generator, "installerSha256 = $InstallerSha256"),
    "validator environment binding": (validator, "[string]$ExpectedInstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256"),
    "validator expected syntax": (validator, "$ExpectedInstallerSha256 -cnotmatch '^[0-9A-Fa-f]{64}$'"),
    "validator expected normalization": (validator, "$expectedInstallerSha256Canonical = $ExpectedInstallerSha256.ToLowerInvariant()"),
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
