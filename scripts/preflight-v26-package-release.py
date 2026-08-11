#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(rel):
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"missing V26 package/release file: {rel}")
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(f"{label} missing required token: {token}")


def forbid(text, token, label):
    if token in text:
        errors.append(f"{label} contains forbidden token: {token}")


def property_value(project_path, name):
    text = read(project_path)
    if not text:
        return ""
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append(f"{project_path} XML parse failed: {exc}")
        return ""
    values = []
    for group in root.findall("PropertyGroup"):
        node = group.find(name)
        if node is not None and node.text and node.text.strip():
            values.append(node.text.strip())
    if len(values) != 1:
        errors.append(f"{project_path} must declare exactly one {name}, found {len(values)}")
        return ""
    return values[0]


transformer = read("scripts/new-v26-script-from-v25.ps1")
package = read("scripts/package-v26.ps1")
sign = read("scripts/sign-v26.ps1")
verify = read("scripts/verify-v26-signatures.ps1")
finalize = read("scripts/finalize-v26-signed-package.ps1")
manifest = read("scripts/new-v26-update-manifest.ps1")
workflow = read(".github/workflows/release-v26.yml")
v26_project = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")

for token in (
    ".Replace('V25', 'V26').Replace('v25', 'v26')",
    "Generated V26 script still contains a V25/v25 token",
    "install-v25-autoload.ps1",
    "uninstall-v25-autoload.ps1",
    "update-v25.ps1",
    "finalize-v25-signed-package.ps1",
    "new-v25-update-manifest.ps1",
):
    require(transformer, token, "V26 script transformer")

# Independently model the exact major-token transform against today's hardened V25 templates.
template_expectations = {
    "scripts/install-v25-autoload.ps1": [
        "QS3D.BricsCAD.V26.dll", "BricsCAD V26 x64", "BricsCAD-V26", "^V26", "QS3D-BricsCAD-V26-Update-"
    ],
    "scripts/uninstall-v25-autoload.ps1": [
        "QS3D.BricsCAD.V26.dll", "BricsCAD V26 x64", "BricsCAD-V26", "^V26", "QS3D-BricsCAD-V26-Update-"
    ],
    "scripts/update-v25.ps1": [
        "QS3D.BricsCAD.V26.dll", "BricsCAD V26 x64", "BricsCAD-V26",
        "QS3D-BricsCAD-V26.update.json", "QS3D-BricsCAD-V26.zip",
        "install-v26-autoload.ps1", "QS3D-BricsCAD-V26-Update-"
    ],
    "scripts/finalize-v25-signed-package.ps1": [
        "QS3D.BricsCAD.V26.dll", "BricsCAD V26 x64", "QS3D-BricsCAD-V26.zip",
        "install-v26-autoload.ps1", "uninstall-v26-autoload.ps1", "update-v26.ps1"
    ],
    "scripts/new-v25-update-manifest.ps1": [
        "QS3D.BricsCAD.V26.dll", "BricsCAD V26 x64", "QS3D-BricsCAD-V26.zip",
        "QS3D-BricsCAD-V26.update.json", "install-v26-autoload.ps1",
        "uninstall-v26-autoload.ps1", "update-v26.ps1"
    ],
}
for rel, expected in template_expectations.items():
    source = read(rel)
    generated = source.replace("V25", "V26").replace("v25", "v26")
    if re.search(r"v25", generated, flags=re.IGNORECASE):
        errors.append(f"independent V26 transform leaked V25 token from {rel}")
    for token in expected:
        if token not in generated:
            errors.append(f"independent V26 transform from {rel} missing: {token}")

for token in (
    "src/QS3D.BricsCAD.V26/bin/x64/Release/net8.0-windows",
    "QS3D-BricsCAD-V26",
    "QS3D-BricsCAD-V26.zip",
    "QS3D.BricsCAD.V26.dll",
    "BricsCAD V26 x64",
    "framework = 'net8.0-windows'",
    "new-v26-script-from-v25.ps1",
    "install-v26-autoload.ps1",
    "uninstall-v26-autoload.ps1",
    "update-v26.ps1",
    "BrxMgd.dll",
    "TD_Mgd.dll",
    "SHA256SUMS.txt",
):
    require(package, token, "V26 packager")
for token in ("src/QS3D.BricsCAD.V25/bin/x64/Release/net48", "QS3D-BricsCAD-V25.zip", "BricsCAD V25 x64"):
    forbid(package, token, "V26 packager")

for text, label, template in (
    (sign, "V26 signer", "sign-v25.ps1"),
    (verify, "V26 signature verifier", "verify-v25-signatures.ps1"),
):
    require(text, "Set-StrictMode -Version Latest", label)
    require(text, template, label)

for text, label, template_name in (
    (finalize, "V26 finalizer", "finalize-v25-signed-package.ps1"),
    (manifest, "V26 manifest generator", "new-v25-update-manifest.ps1"),
):
    require(text, "new-v26-script-from-v25.ps1", label)
    require(text, template_name, label)
    require(text, "contains a V25 token", label)
    require(text, "QS3D-BricsCAD-V26", label)

for token in (
    "workflow_dispatch:",
    "github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE'",
    "runs-on: [self-hosted, windows, x64, bricscad-v26]",
    "BRICSCAD_V26_DIR",
    "FileMajorPart -ne 26",
    "Microsoft\\.WindowsDesktop\\.App 8\\.",
    "preflight-bricscad-v26.py",
    "preflight-v26-package-release.py",
    "package-v26.ps1",
    "sign-v26.ps1",
    "verify-v26-signatures.ps1",
    "finalize-v26-signed-package.ps1",
    "test-bricscad-v26-runtime.ps1",
    "new-v26-update-manifest.ps1",
    "QS3D-BricsCAD-V26.update.json",
    "QS3D-BricsCAD-V26.zip.sha256",
    "Stable V26 release requires run_runtime=true",
    "Stable V26 release requires sign_package=true",
    "draft = $true",
    "draft = $false",
):
    require(workflow, token, "V26 release workflow")
for token in ("QS3D-BricsCAD-V25", "BRICSCAD_V25_DIR", "bricscad-v25", "QS3D.BricsCAD.V25.dll"):
    forbid(workflow, token, "V26 release workflow")
for trigger in ("\n  push:", "\n  pull_request:", "\n  schedule:", "\n  workflow_run:"):
    forbid(workflow, trigger, "V26 release workflow")

for token in ("<TargetFramework>net8.0-windows</TargetFramework>", "<AssemblyName>QS3D.BricsCAD.V26</AssemblyName>"):
    require(v26_project, token, "V26 project")
plugin_version = property_value("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj", "Version")
plugin_info = property_value("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj", "InformationalVersion")
core_version = property_value("src/QS3D.Core/QS3D.Core.csproj", "Version")
core_info = property_value("src/QS3D.Core/QS3D.Core.csproj", "InformationalVersion")
if plugin_version and core_version and plugin_version != core_version:
    errors.append(f"V26/Core Version identity differs: {plugin_version} vs {core_version}")
if plugin_info and core_info and plugin_info != core_info:
    errors.append(f"V26/Core InformationalVersion identity differs: {plugin_info} vs {core_info}")

print("QS3D BricsCAD V26 package/release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: V26 packaging derives current hardened V25 transaction/security logic under a guarded major transform; package, signing, finalization, update assets and manual release stay V26-only while V25 remains untouched.")
