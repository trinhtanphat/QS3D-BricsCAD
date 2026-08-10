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

for path in (runtime, package, finalize, plugin_project, core_project):
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
        "QS3D plugin/Core product versions differ",
        "$expectedTag = 'v' + $productVersion",
        "RELEASE_TAG must exactly match the source product version",
        "productVersion = $productVersion",
        "QS3DRUNTIMECHECK",
    ):
        if needle not in text:
            errors.append("package-v25.ps1 missing full-version/customer contract: " + needle)

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
            versions[label] = match.group(1).strip()
if len(versions) == 2 and versions["plugin"].lower() != versions["core"].lower():
    errors.append("plugin/Core <Version> values differ: " + repr(versions))

print("QS3D customer release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: customer diagnostics are registered, V25/x64 is checked in-product, "
    "plugin/Core product SemVer is aligned, RELEASE_TAG is exact-version-bound, "
    "and finalized signed metadata records the verified publisher."
)
