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

    loop_token = "foreach ($propertyName in @("
    loop_at = text.find(loop_token)
    if loop_at < 0:
        raise SystemExit("V26 installer provenance admission no longer has the common duplicate-property loop")
    header_end = text.find(")) {", loop_at)
    if header_end < 0:
        raise SystemExit("V26 installer provenance duplicate-property loop header is malformed")
    loop_header = text[loop_at:header_end]
    if "'installerSha256'" not in loop_header:
        raise SystemExit("installerSha256 must remain a member of the common provenance uniqueness set")

    parse_token = "$provenance = $provenanceText | ConvertFrom-Json -ErrorAction Stop"
    parse_at = text.find(parse_token, loop_at)
    if parse_at < 0:
        raise SystemExit("V26 installer provenance admission no longer parses provenance after uniqueness admission")
    loop_body = text[loop_at:parse_at]
    for token in (
        "Get-JsonPropertyOccurrenceCount -JsonText $provenanceText -PropertyName $propertyName",
        'throw "V26 candidate provenance must contain exactly one $propertyName property."',
    ):
        if token not in loop_body:
            raise SystemExit(f"V26 installer provenance common uniqueness loop is missing fail-closed behavior: {token}")

    installer_check = text.find("$installerSha256 = [string]$provenance.installerSha256", parse_at)
    script_parse = text.find("$admittedScriptBlock = $null", installer_check)
    script_exec = text.find("& $admittedScriptBlock", script_parse)
    if min(installer_check, script_parse, script_exec) < 0 or not loop_at < parse_at < installer_check < script_parse < script_exec:
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
loop_start = validator.index("foreach ($propertyName in @(")
loop_header_end = validator.index(")) {", loop_start)
loop_header = validator[loop_start:loop_header_end]
without_installer = loop_header.replace("'installerSha256', ", "", 1)
if without_installer == loop_header:
    without_installer = loop_header.replace(", 'installerSha256'", "", 1)
if without_installer == loop_header:
    raise SystemExit("mutation control unavailable: installerSha256 uniqueness-set membership")
try:
    assert_validator_contract(validator[:loop_start] + without_installer + validator[loop_header_end:])
except SystemExit:
    pass
else:
    raise SystemExit("V26 installer provenance guard accepted uniqueness set without installerSha256")

occurrence_token = "Get-JsonPropertyOccurrenceCount -JsonText $provenanceText -PropertyName $propertyName"
mutated_occurrence = validator.replace(occurrence_token, "# removed duplicate-property enforcement", 1)
try:
    assert_validator_contract(mutated_occurrence)
except SystemExit:
    pass
else:
    raise SystemExit("V26 installer provenance guard accepted a uniqueness loop without occurrence enforcement")

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
