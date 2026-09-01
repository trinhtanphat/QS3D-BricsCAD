#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts/assert-v26-release-request.ps1"
WORKFLOW = ROOT / ".github/workflows/release-v26.yml"
PUBLISHER = ROOT / "scripts/publish-v26-release.ps1"
PACKAGE = ROOT / "scripts/package-v26.ps1"
errors = []


def read(path: Path, label: str) -> str:
    if not path.is_file():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"{label} missing required token: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        errors.append(f"{label} contains forbidden loose admission token: {token}")


def strict_semver_tag(tag: str):
    pattern = re.compile(
        r"^v(0|[1-9][0-9]*)\."
        r"(0|[1-9][0-9]*)\."
        r"(0|[1-9][0-9]*)"
        r"(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?"
        r"(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$"
    )
    match = pattern.fullmatch(tag)
    if not match:
        return None
    prerelease = match.group(4)
    if prerelease:
        for identifier in prerelease.split("."):
            if identifier.isdigit() and len(identifier) > 1 and identifier.startswith("0"):
                return None
    return bool(prerelease)


helper = read(HELPER, "V26 release-request helper")
workflow = read(WORKFLOW, "V26 release workflow")
publisher = read(PUBLISHER, "V26 hosted publisher")
package = read(PACKAGE, "V26 packager")

for token in (
    "Set-StrictMode -Version Latest",
    "[Text.RegularExpressions.RegexOptions]::CultureInvariant",
    "^v(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)",
    "(?:-([0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*))?",
    "(?:\\+([0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*))?$",
    "numeric prerelease identifier with a leading zero",
    "prerelease input must match the validated V26 release_tag prerelease state",
    "Stable V26 release requires run_runtime=true.",
    "Stable V26 release requires sign_package=true.",
    "IsPrerelease = $match.Groups[4].Success",
):
    require(helper, token, "V26 release-request helper")

for token in (
    "assert-v26-release-request.ps1",
    "-ReleaseTag $env:RELEASE_TAG",
    "-Prerelease:($env:RELEASE_PRERELEASE -eq 'true')",
    "-RunRuntime:($env:RELEASE_RUN_RUNTIME -eq 'true')",
    "-SignPackage:($env:RELEASE_SIGN_PACKAGE -eq 'true')",
    "if ([bool]$releaseRequest.SignPackage)",
    "needs: qualify",
    "V26_RELEASE_REQUEST_PRERELEASE: ${{ inputs.prerelease }}",
    "V26_RELEASE_REQUEST_SIGN_PACKAGE: ${{ inputs.sign_package }}",
):
    require(workflow, token, "V26 release workflow")

for token in (
    "$isPrerelease = $env:V26_RELEASE_REQUEST_PRERELEASE -eq 'true'",
    "$signPackage = $env:V26_RELEASE_REQUEST_SIGN_PACKAGE -eq 'true'",
):
    require(publisher, token, "V26 hosted publisher")

for token in (
    "^v[0-9]+\\.[0-9]+\\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\\+[0-9A-Za-z.-]+)?$",
    "$hasPrereleaseSuffix = $env:RELEASE_TAG -match '^v[0-9]+\\.[0-9]+\\.[0-9]+-'",
):
    forbid(workflow, token, "V26 release workflow")

# Cross-lock the strict grammar with the package provenance contract instead of
# introducing an independently broader release-request grammar.
for token in (
    "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)",
    "numeric prerelease identifier with a leading zero",
    "must not contain leading or trailing whitespace",
):
    require(package, token, "V26 packager strict SemVer contract")

accepted = {
    "v0.1.0": False,
    "v1.2.3": False,
    "v1.2.3-rc.1": True,
    "v1.2.3-alpha-beta.7+build.4": True,
    "v1.2.3+build.4": False,
}
rejected = (
    " v1.2.3",
    "v1.2.3 ",
    "v01.2.3",
    "v1.02.3",
    "v1.2.03",
    "v1.2",
    "1.2.3",
    "v1.2.3-",
    "v1.2.3-rc..1",
    "v1.2.3-.rc",
    "v1.2.3-rc.",
    "v1.2.3-01",
    "v1.2.3-rc.01",
    "v1.2.3+",
    "v1.2.3+build..4",
    "v1.2.3+build.",
)
for tag, expected_prerelease in accepted.items():
    actual = strict_semver_tag(tag)
    if actual is None or actual != expected_prerelease:
        errors.append(f"strict V26 tag model rejected/misclassified canonical tag: {tag}")
for tag in rejected:
    if strict_semver_tag(tag) is not None:
        errors.append(f"strict V26 tag model accepted malformed/non-canonical tag: {tag}")

# Mutation probes: every safety-bearing source mutation below must be observable
# by the same contract assertions rather than silently weakening admission.
mutations = {
    "broaden-core-numerics": helper.replace("(0|[1-9][0-9]*)", "[0-9]+", 1),
    "allow-empty-prerelease-identifiers": helper.replace("[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*", "[0-9A-Za-z.-]+", 1),
    "remove-leading-zero-rejection": helper.replace("numeric prerelease identifier with a leading zero", "numeric prerelease identifier accepted"),
    "remove-prerelease-binding": helper.replace("prerelease input must match the validated V26 release_tag prerelease state", "prerelease input mismatch ignored"),
}
for name, mutated in mutations.items():
    if mutated == helper:
        errors.append(f"mutation probe did not alter helper source: {name}")
        continue
    required_markers = (
        "^v(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)",
        "(?:-([0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*))?",
        "numeric prerelease identifier with a leading zero",
        "prerelease input must match the validated V26 release_tag prerelease state",
    )
    if all(marker in mutated for marker in required_markers):
        errors.append(f"mutation escaped V26 release-request source contract: {name}")

print("QS3D V26 release-request strict-SemVer preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: manual V26 release admission is strict-SemVer, prerelease-state bound across the qualify/publish split, stable-gate preserving, and package-provenance aligned.")
