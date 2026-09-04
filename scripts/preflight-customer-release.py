from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(rel):
    path = ROOT / rel
    if not path.is_file():
        errors.append("missing customer-release source: " + rel)
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing customer-release contract: " + token)


runtime = read("src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs")
readiness = read("src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs")
package = read("scripts/package-v25.ps1")
release_package = read("scripts/package-v25-release.ps1")
finalize = read("scripts/finalize-v25-signed-package.ps1")
plugin_project = read("src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj")
core_project = read("src/QS3D.Core/QS3D.Core.csproj")
commercial = read(".github/workflows/release-v25.yml")
cloud = read(".github/workflows/release-v25-cloud.yml")

STRICT_SEMVER = re.compile(r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$")

def is_strict_semver(value):
    m = STRICT_SEMVER.fullmatch(value)
    if not m: return False
    prerelease = m.group(4)
    return not prerelease or all(not (part.isdigit() and len(part) > 1 and part.startswith("0")) for part in prerelease.split("."))

for value in ("0.0.0", "1.2.3", "1.2.3-rc.1", "1.2.3-rc.1+build.4"):
    if not is_strict_semver(value): errors.append("strict SemVer rejected valid value: " + value)
for value in ("01.2.3", "1.02.3", "1.2.03", "1.2.3-01", "1.2.3-rc.01", "v1.2.3"):
    if is_strict_semver(value): errors.append("strict SemVer accepted invalid value: " + value)

for token in (
    '[CommandMethod("QS3DRUNTIMECHECK"', "Environment.Is64BitProcess", "private const int ExpectedRuntimeMajor = 26;",
    "private const int ExpectedRuntimeMajor = 25;", "var expectedRuntime = NativeRuntimeAssembliesMatch(brxAssembly, tdAssembly);",
    "private static bool NativeRuntimeAssembliesMatch", '"PACKAGE-METADATA.json"', 'JsonString(text, "productVersion")',
    'JsonString(text, "signedPayloadSignerThumbprint")', "diskVersionMatches", "diskFingerprintMatches",
): require(runtime, token, "RuntimeDiagnosticsCommands.cs")
if "Major(brxAssembly) == 25" in runtime or "Major(tdAssembly) == 25" in runtime:
    errors.append("shared runtime diagnostics must not hard-code V25 identity")

for token in ('[CommandMethod("QS3DRELEASECHECK", CommandFlags.Modal)]', "ProjectContextCoordinator.TryGetReadOnly(document, out var project)", 'ExpectedRuntimeLabel + " runtime/private-DWG gate'):
    require(readiness, token, "ReleaseReadinessCommands.cs")
if "ProjectContextCoordinator.GetOrCreate(document)" in readiness:
    errors.append("QS3DRELEASECHECK must remain read-only")

for token in (
    "Read-ProjectProductVersion", "function Convert-ToStrictSemVerText", "numeric prerelease identifier with a leading zero",
    "[string]::Equals($productVersion, $coreProductVersion, [StringComparison]::Ordinal)", "$expectedTag = 'v' + $productVersion",
    "RELEASE_TAG must exactly match the source product version", "productVersion = $productVersion", "QS3DRUNTIMECHECK",
): require(package, token, "package-v25.ps1")
for token in (
    "[string]::Equals($productVersion, $coreProductVersion, [StringComparison]::OrdinalIgnoreCase)",
    "[string]::Equals($env:RELEASE_TAG, $expectedTag, [StringComparison]::OrdinalIgnoreCase)",
):
    if token in package:
        errors.append("package-v25.ps1 must not case-fold exact product/tag identity: " + token)

for token in ("Assert-CleanRepository -Phase 'before package creation'", "& $packer", "Repository HEAD changed during release packaging", "Assert-CleanRepository -Phase 'after package creation'", "PACKAGE-METADATA gitCommit", "does not match the exact clean package source HEAD"):
    require(release_package, token, "package-v25-release.ps1")
for token in ("pluginSignatureStatus -NotePropertyValue 'Valid'", "pluginSignerThumbprint -NotePropertyValue $expectedSigner", "signedPayloadSignerThumbprint", "signedPluginAssemblyVersion"):
    require(finalize, token, "finalize-v25-signed-package.ps1")

version_pattern = re.compile(r"<Version>([^<]+)</Version>")
# Protected-main preview ordinals are committed on the V25/V26 plugin projects first.
# Core can remain on the prior ordinal until the release workspace synchronization phase;
# the packager still fail-closes on exact plugin/Core identity before packaging.
for label, text in (("plugin", plugin_project), ("core", core_project)):
    m = version_pattern.search(text)
    if not m: errors.append(label + " project is missing <Version>")
    else:
        value = m.group(1).strip()
        if not is_strict_semver(value): errors.append(label + " project <Version> is not strict SemVer: " + repr(value))

for name, workflow, package_boundary, publish_markers in (
    ("release-v25.yml", commercial, "package-v25-release.ps1", ("Create draft, verify uploaded bytes, then publish", "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri")),
    ("release-v25-cloud.yml", cloud, "package-v25.ps1", ("Publish GitHub prerelease",)),
):
    preflight_index = workflow.find("python scripts/preflight-all.py")
    package_index = workflow.find(package_boundary)
    publish_index = min([i for i in (workflow.find(marker) for marker in publish_markers) if i >= 0], default=-1)
    if min(preflight_index, package_index, publish_index) < 0 or not preflight_index < package_index < publish_index:
        errors.append(name + " must aggregate-preflight -> package -> publish using its canonical release path")

for token in (
    "$tagCreatedByThisRun = $true",
    "$releaseId = [long]$release.id",
    "Assert-RemoteReleaseTagTargetsWorkflowSha",
    "gh release download $env:RELEASE_TAG",
    "rollback-v25-draft-release.ps1",
):
    require(commercial, token, "release-v25.yml")

print("QS3D customer release preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: customer release identity stays strict/exact, committed plugin preview identity may precede Core workspace synchronization, runtime diagnostics use the complete host-major helper, commercial packaging is clean-source signed/provenance-bound, and both commercial/cloud workflows preflight before package and publish; commercial publication retains exact-tag ownership, exact-tag assertion, and rollback.")