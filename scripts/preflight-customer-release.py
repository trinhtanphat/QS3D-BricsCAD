from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

runtime = ROOT / "src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs"
package = ROOT / "scripts/package-v25.ps1"
finalize = ROOT / "scripts/finalize-v25-signed-package.ps1"
plugin_project = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
core_project = ROOT / "src/QS3D.Core/QS3D.Core.csproj"
release_workflows = (
    ROOT / ".github/workflows/release-v25.yml",
    ROOT / ".github/workflows/release-v25-cloud.yml",
)

STRICT_SEMVER = re.compile(
    r"^(0|[1-9][0-9]*)\."
    r"(0|[1-9][0-9]*)\."
    r"(0|[1-9][0-9]*)"
    r"(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"
)


def is_strict_semver(value):
    match = STRICT_SEMVER.fullmatch(value)
    if not match:
        return False
    prerelease = match.group(4)
    if prerelease:
        for identifier in prerelease.split("."):
            if identifier.isdigit() and len(identifier) > 1 and identifier.startswith("0"):
                return False
    return True


def exact_release_identity(product_version, core_version, release_tag):
    return product_version == core_version and release_tag == "v" + product_version


for value in ("0.0.0", "1.2.3", "1.2.3-rc.1", "1.2.3-rc.1+build.4", "1.2.3+001"):
    if not is_strict_semver(value):
        errors.append("strict SemVer regression unexpectedly rejected valid value: " + value)
for value in (
    "01.2.3",
    "1.02.3",
    "1.2.03",
    "1.2.3-01",
    "1.2.3-rc.01",
    "1.2.3-",
    "1.2.3+",
    "1.2.3-rc..1",
    "1.2.3+build..1",
    "v1.2.3",
):
    if is_strict_semver(value):
        errors.append("strict SemVer regression unexpectedly accepted invalid value: " + value)

if not exact_release_identity("1.2.3-preview.2", "1.2.3-preview.2", "v1.2.3-preview.2"):
    errors.append("exact release identity regression must accept identical product/core/tag versions")
for product, core, tag in (
    ("1.2.3-preview.2", "1.2.3-PREVIEW.2", "v1.2.3-preview.2"),
    ("1.2.3-preview.2", "1.2.3-preview.2", "v1.2.3-PREVIEW.2"),
):
    if exact_release_identity(product, core, tag):
        errors.append("exact release identity regression must reject case-only version differences")

for path in (runtime, package, finalize, plugin_project, core_project) + release_workflows:
    if not path.is_file():
        errors.append("missing customer-release source: " + str(path.relative_to(ROOT)))

if runtime.is_file():
    text = runtime.read_text(encoding="utf-8")
    for needle in (
        '[CommandMethod("QS3DRUNTIMECHECK"',
        "Environment.Is64BitProcess",
        "Major(brxAssembly) == 25",
        "Major(tdAssembly) == 25",
        '"PACKAGE-METADATA.json"',
        'JsonString(text, "productVersion")',
        'JsonString(text, "signedPayloadSignerThumbprint")',
        "QS3DRELEASECHECK",
    ):
        if needle not in text:
            errors.append("RuntimeDiagnosticsCommands.cs missing runtime/customer guard: " + needle)

if package.is_file():
    text = package.read_text(encoding="utf-8")
    for needle in (
        "Read-ProjectProductVersion",
        "function Convert-ToStrictSemVerText",
        "is not strict SemVer",
        "numeric prerelease identifier with a leading zero",
        "Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $pluginProject)",
        "Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $coreProject)",
        "[string]::Equals($productVersion, $coreProductVersion, [StringComparison]::Ordinal)",
        "[string]::Equals($env:RELEASE_TAG.Trim(), $expectedTag, [StringComparison]::Ordinal)",
        "QS3D plugin/Core product versions differ",
        "$expectedTag = 'v' + $productVersion",
        "RELEASE_TAG must exactly match the source product version",
        "productVersion = $productVersion",
        "QS3DRUNTIMECHECK",
    ):
        if needle not in text:
            errors.append("package-v25.ps1 missing strict/exact-version customer contract: " + needle)
    if "[StringComparison]::OrdinalIgnoreCase" in text:
        errors.append("package-v25.ps1 must not use case-insensitive comparison for exact release identity")

if finalize.is_file():
    text = finalize.read_text(encoding="utf-8")
    for needle in (
        "pluginSignatureStatus -NotePropertyValue 'Valid'",
        "pluginSignerThumbprint -NotePropertyValue $expectedSigner",
        "signedPayloadSignerThumbprint",
        "signedPluginAssemblyVersion",
    ):
        if needle not in text:
            errors.append("finalize-v25-signed-package.ps1 missing finalized signing metadata: " + needle)

version_pattern = re.compile(r"<Version>([^<]+)</Version>")
versions = {}
for label, path in (("plugin", plugin_project), ("core", core_project)):
    if path.is_file():
        match = version_pattern.search(path.read_text(encoding="utf-8"))
        if not match:
            errors.append(label + " project is missing <Version>")
        else:
            version = match.group(1).strip()
            versions[label] = version
            if not is_strict_semver(version):
                errors.append(label + " project <Version> is not strict SemVer: " + repr(version))
if len(versions) == 2 and versions["plugin"] != versions["core"]:
    errors.append("plugin/Core <Version> values differ exactly: " + repr(versions))

for workflow in release_workflows:
    if not workflow.is_file():
        continue
    text = workflow.read_text(encoding="utf-8")
    preflight_index = text.find("python scripts/preflight-all.py")
    package_index = text.find("package-v25.ps1")
    publish_index = text.lower().find("publish github ")
    if preflight_index < 0:
        errors.append(workflow.name + " must run aggregate preflight before release packaging")
    if package_index < 0:
        errors.append(workflow.name + " must use package-v25.ps1 as the release package boundary")
    if publish_index < 0:
        errors.append(workflow.name + " is missing the GitHub release/prerelease publish step")
    if preflight_index >= 0 and package_index >= 0 and preflight_index >= package_index:
        errors.append(workflow.name + " must run aggregate preflight before package-v25.ps1")
    if package_index >= 0 and publish_index >= 0 and package_index >= publish_index:
        errors.append(workflow.name + " must run package-v25.ps1 before publishing the GitHub release/prerelease")

print("QS3D customer release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: customer diagnostics are registered, V25/x64 is checked in-product, "
    "plugin/Core product versions are strict SemVer and exact-case aligned, RELEASE_TAG is exact-case/version-bound, "
    "release workflows execute the strict package boundary before publication, "
    "and finalized signed metadata records the verified publisher."
)
