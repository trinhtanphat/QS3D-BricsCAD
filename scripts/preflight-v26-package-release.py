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
qualification_workflow = read(".github/workflows/bricscad-v26.yml")
v26_project = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")
v25_release_client = read("src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs")
v26_release_client = read("src/QS3D.BricsCAD.V26/Updates/GitHubReleaseClient.cs")
v26_manifest_probe = read("src/QS3D.BricsCAD.V26/Updates/UpdateManifestProbe.cs")
v26_launcher = read("src/QS3D.BricsCAD.V26/Updates/SecureUpdateLauncher.cs")
v26_update_command = read("src/QS3D.BricsCAD.V26/Updates/UpdateCommands.cs")
v26_entry = read("src/QS3D.BricsCAD.V26/PluginEntry.cs")
build_props = read("Directory.Build.props")

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

for text, label, asset in (
    (v25_release_client, "V25 release client", "QS3D-BricsCAD-V25.update.json"),
    (v26_release_client, "V26 release client", "QS3D-BricsCAD-V26.update.json"),
):
    require(text, asset, label)
    require(text, "if (manifestUri == null) continue;", label)
    require(text, "UpdateManifestAssetName", label)
for token in ("QS3D-BricsCAD-V25.update.json", "QS3D-BricsCAD-V25-Updater"):
    forbid(v26_release_client, token, "V26 release client")

require(build_props, "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", "Directory.Build.props")
for text, label in (
    (v26_release_client, "V26 release client"),
    (v26_manifest_probe, "V26 manifest probe"),
):
    for token in (
        "using System.Net.Http;",
        "HttpClient",
        "HttpClientHandler",
        "HttpCompletionOption.ResponseHeadersRead",
        "CancellationTokenSource",
        "Timeout.InfiniteTimeSpan",
    ):
        require(text, token, label)
    for token in ("WebRequest.CreateHttp", "HttpWebRequest"):
        forbid(text, token, label)

for token in (
    'private const string Target = "BricsCAD V26 x64";',
    'request.Headers.UserAgent.ParseAdd("QS3D-BricsCAD-V26-Updater")',
    '"QS3D-BricsCAD-V26.zip"',
    "GitHubReleaseClient.UpdateManifestAssetName",
    "schemaVersion 2",
):
    require(v26_manifest_probe, token, "V26 manifest probe")
for token in ("BricsCAD V25 x64", "QS3D-BricsCAD-V25.zip", "QS3D-BricsCAD-V25.update.json"):
    forbid(v26_manifest_probe, token, "V26 manifest probe")

for token in (
    "Global\\\\QS3D-BricsCAD-V26-Update-",
    'Path.Combine(installDirectory, "update-v26.ps1")',
    "TryVerifyAuthenticode",
    "WinVerifyTrust",
    "TryAcquireCrossProcessReservation",
    "WorkerReadyTimeoutMilliseconds",
    "-AllowedPackageHost @('github.com')",
    "-ExpectedSignerThumbprint $expectedSigner",
):
    require(v26_launcher, token, "V26 secure update launcher")
for token in ("QS3D-BricsCAD-V25-Update-", "update-v25.ps1"):
    forbid(v26_launcher, token, "V26 secure update launcher")

for token in ("QS3DUPDATE", "UpdateCenterWindowHost.Show()", "QS3DUPDATE V26 error"):
    require(v26_update_command, token, "V26 update command")
for token in ("UpdateBootstrapper.Start();", "UpdateBootstrapper.Stop();"):
    require(v26_entry, token, "V26 PluginEntry")

# V26 qualification must execute the same aggregate source/release guards used by
# the release lane before native build/runtime evidence can be accepted.
for token in (
    "permissions:\n  contents: read",
    "persist-credentials: false",
    "python scripts/preflight-all.py",
    "python scripts/preflight-bricscad-v26.py",
    "python scripts/preflight-v26-package-release.py",
    "dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64",
    "test-bricscad-v26-runtime.ps1",
):
    require(qualification_workflow, token, "V26 qualification workflow")
if "contents: write" in qualification_workflow:
    errors.append("V26 qualification workflow must remain read-only")

# V26 commercial publication is deliberately two-stage. The self-hosted/native
# qualification job must never receive repository write authority; only the
# GitHub-hosted publication job may receive contents:write after the candidate
# crossed an artifact boundary and was independently reverified.
for token in (
    "workflow_dispatch:",
    "github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE'",
    "permissions:\n  contents: read",
    "build_sign:",
    "runs-on: [self-hosted, windows, x64, bricscad-v26]",
    "All discovered feature source guards",
    "preflight-bricscad-v26.py",
    "preflight-v26-package-release.py",
    "package-v26.ps1",
    "sign-v26.ps1",
    "verify-v26-signatures.ps1",
    "finalize-v26-signed-package.ps1",
    "test-bricscad-v26-runtime.ps1",
    "new-v26-update-manifest.ps1",
    "Create V26 package checksum and provenance",
    "QS3D-BricsCAD-V26.provenance.json",
    "Upload V26 qualified candidate",
    "release:",
    "needs: build_sign",
    "runs-on: windows-latest",
    "contents: write",
    "actions/download-artifact@3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c",
    "Verify V26 candidate after job boundary",
    "V26 ZIP checksum mismatch after job boundary",
    "V26 candidate provenance does not exactly bind tag, product, source, signing state and ZIP digest",
    "verify-v26-signatures.ps1",
    "gh release create",
    "--draft",
    "git ls-remote --tags origin",
    "gh release download",
    "Draft V26 release asset SHA-256 mismatch",
    "gh release edit",
    "--draft=false",
    "Stable V26 release requires run_runtime=true",
    "Stable V26 release requires sign_package=true",
):
    require(workflow, token, "V26 release workflow")

build_index = workflow.find("  build_sign:")
release_index = workflow.find("  release:", build_index + 1)
if build_index < 0 or release_index < 0 or build_index >= release_index:
    errors.append("V26 release workflow must order build_sign before release")
else:
    build_section = workflow[build_index:release_index]
    release_section = workflow[release_index:]
    if "contents: write" in build_section:
        errors.append("V26 self-hosted build/sign/runtime job must not receive contents:write")
    if "permissions:\n      contents: read" not in build_section:
        errors.append("V26 self-hosted build/sign/runtime job must explicitly use contents:read")
    if "permissions:\n      contents: write" not in release_section:
        errors.append("V26 publication job must explicitly scope contents:write")
    if "runs-on: [self-hosted" in release_section:
        errors.append("V26 write-enabled publication job must not run on the self-hosted BricsCAD runner")

upload_candidate = workflow.find("Upload V26 qualified candidate")
download_candidate = workflow.find("actions/download-artifact@", release_index)
verify_boundary = workflow.find("Verify V26 candidate after job boundary", download_candidate)
draft_create = workflow.find("gh release create", verify_boundary)
remote_tag_check = workflow.find("git ls-remote --tags origin", draft_create)
remote_download = workflow.find("gh release download", remote_tag_check)
remote_hash = workflow.find("Draft V26 release asset SHA-256 mismatch", remote_download)
publish = workflow.find("gh release edit", remote_hash)
if min(upload_candidate, download_candidate, verify_boundary, draft_create, remote_tag_check, remote_download, remote_hash, publish) < 0 or not (
    upload_candidate < download_candidate < verify_boundary < draft_create < remote_tag_check < remote_download < remote_hash < publish
):
    errors.append("V26 publication order must be qualified artifact -> download -> boundary verify -> draft -> tag/SHA check -> remote download/hash verify -> publish")

for token in ("QS3D-BricsCAD-V25", "BRICSCAD_V25_DIR", "bricscad-v25", "QS3D.BricsCAD.V25.dll"):
    forbid(workflow, token, "V26 release workflow")
for trigger in ("\n  push:", "\n  pull_request:", "\n  schedule:", "\n  workflow_run:"):
    forbid(workflow, trigger, "V26 release workflow")

for token in (
    "<TargetFramework>net8.0-windows</TargetFramework>",
    "<AssemblyName>QS3D.BricsCAD.V26</AssemblyName>",
    "Updates\\SemanticReleaseVersion.cs",
    "Updates\\UpdateBootstrapper.cs",
    "Updates\\UpdateCenterWindow.cs",
    "Updates\\UpdateCoordinator.cs",
):
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

print("PASS: V26 packaging preserves hardened major isolation; qualification is aggregate-guarded and read-only; self-hosted build/sign/runtime is separated from write-enabled publication by a reverified artifact boundary.")
