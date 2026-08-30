#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(rel):
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"missing required V26 compatibility file: {rel}")
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(f"{label} missing required token: {token}")


def forbid(text, token, label):
    if token in text:
        errors.append(f"{label} contains forbidden token: {token}")


def preprocess_for_v26(text):
    """Return the active source view when BRICSCAD_V26 is defined."""
    active = True
    branches = []
    visible = []

    for line in text.splitlines(keepends=True):
        directive = line.strip()
        if directive.startswith("#if "):
            condition = directive[4:].strip()
            if condition == "BRICSCAD_V26":
                enabled = True
            elif condition == "!BRICSCAD_V26":
                enabled = False
            else:
                errors.append(f"shared Update Center contains unsupported V26 preprocessor condition: {condition}")
                enabled = False
            branches.append((active, enabled))
            active = active and enabled
            continue
        if directive == "#else":
            if not branches:
                errors.append("shared Update Center has #else without matching #if")
                continue
            parent_active, enabled = branches[-1]
            active = parent_active and not enabled
            continue
        if directive == "#endif":
            if not branches:
                errors.append("shared Update Center has #endif without matching #if")
                continue
            active, _ = branches.pop()
            continue
        if active:
            visible.append(line)

    if branches:
        errors.append("shared Update Center has unterminated preprocessor branch")
    return "".join(visible)


v25 = read("src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj")
v26 = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")
v26_solution = read("QS3D.V26.sln")
entry = read("src/QS3D.BricsCAD.V26/PluginEntry.cs")
update_commands = read("src/QS3D.BricsCAD.V26/Updates/UpdateCommands.cs")
update_center = read("src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs")
v26_update_center = preprocess_for_v26(update_center)
update_preferences = read("src/QS3D.BricsCAD.V25/Updates/UpdatePreferences.cs")
v25_release_client = read("src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs")
v26_release_client = read("src/QS3D.BricsCAD.V26/Updates/GitHubReleaseClient.cs")
v26_manifest_probe = read("src/QS3D.BricsCAD.V26/Updates/UpdateManifestProbe.cs")
v26_launcher = read("src/QS3D.BricsCAD.V26/Updates/SecureUpdateLauncher.cs")
workflow = read(".github/workflows/bricscad-v26.yml")
runtime = read("scripts/test-bricscad-v26-runtime.ps1")
runtime_probe = read("src/QS3D.BricsCAD.V25/RuntimeProbeCommands.cs")
runtime_diagnostics = read("src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs")
release_readiness = read("src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs")
qualification = read("docs/LOCAL-V26-QUALIFICATION.md")
architecture = read("docs/ARCHITECTURE.md")
ci_documentation = read("docs/CI.md")
manual_release = read("docs/MANUAL-BUILD-RELEASE-V26.md")
core = read("src/QS3D.Core/QS3D.Core.csproj")
build_props = read("Directory.Build.props")

for token in ("<TargetFramework>net48</TargetFramework>", "QS3D.BricsCAD.V25", "BRICSCAD_V25_DIR"):
    require(v25, token, "V25 project")

for token in ('<Project Sdk="Microsoft.NET.Sdk">', "<TargetFramework>net8.0-windows</TargetFramework>", "<UseWPF>true</UseWPF>", "<UseWindowsForms>true</UseWindowsForms>", "<GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>", "<Nullable>annotations</Nullable>", "<AssemblyName>QS3D.BricsCAD.V26</AssemblyName>", "<RootNamespace>QS3D.BricsCAD.V25</RootNamespace>", "BRICSCAD_V26_DIR", "..\\QS3D.BricsCAD.V25\\**\\*.cs", "..\\QS3D.BricsCAD.V25\\PluginEntry.cs", "..\\QS3D.BricsCAD.V25\\Updates\\**\\*.cs", "Updates\\SemanticReleaseVersion.cs", "Updates\\UpdateBootstrapper.cs", "Updates\\UpdateCenterWindow.cs", "Updates\\UpdateCoordinator.cs", "Updates\\UpdatePreferences.cs", "Updates\\UpdateSettingsCommands.cs", "<Reference Include=\"BrxMgd\">", "<Reference Include=\"TD_Mgd\">", "<Reference Include=\"TD_MgdBrep\">", "$(BRICSCAD_V26_DIR)\\TD_MgdBrep.dll", "<Private>false</Private>", "ValidateBricsCadV26References"):
    require(v26, token, "V26 project")
for token in ("Microsoft.NET.Sdk.WindowsDesktop", "BRICSCAD_V25_DIR", "<TargetFramework>net48</TargetFramework>", "<TargetFrameworks>", "QS3D-BricsCAD-V25.update.json"):
    forbid(v26, token, "V26 project")

for token in ('"QS3D.Core", "src\\QS3D.Core\\QS3D.Core.csproj"', '"QS3D.BricsCAD.V26", "src\\QS3D.BricsCAD.V26\\QS3D.BricsCAD.V26.csproj"', '"QS3D.Core.SmokeTests", "tests\\QS3D.Core.SmokeTests\\QS3D.Core.SmokeTests.csproj"', ".Debug|Any CPU.ActiveCfg = Debug|x64", ".Release|Any CPU.ActiveCfg = Release|x64"):
    require(v26_solution, token, "V26 solution")
for token in ("QS3D.BricsCAD.V25.csproj", "BRICSCAD_V25_DIR"):
    forbid(v26_solution, token, "V26 solution")

for token in ("public sealed class PluginEntry : IExtensionApplication", "using QS3D.BricsCAD.V25.Updates;", "RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity();", "DocumentLifecycleCoordinator.Start();", "RibbonInitializationCoordinator.Start();", "TeardownHostServices();", "QuantityContextMenuCoordinator.Start();", 'ReportOptionalStartupFailure("Quantity context menu", ex);', "UpdateBootstrapper.Start();", 'ReportOptionalStartupFailure("Update service", ex);', "TryCleanup(UpdateBootstrapper.Stop);", "TryCleanup(StartCenterPaletteCoordinator.Dispose);", "TryCleanup(PaletteCoordinator.Dispose);", "TryCleanup(UpdateRibbonAugmenter.Reset);", "private static void TryCleanup(Action cleanup)"):
    require(entry, token, "V26 PluginEntry")
for token in ("PaletteCoordinator.EnsureCreated();", "RibbonBootstrapper.TryInitialize();", "ReferenceWallRibbonAugmenter.TryInitialize();", "RibbonBootstrapIconAugmenter.TryInitialize();"):
    forbid(entry, token, "V26 PluginEntry synchronous NETLOAD bootstrap")

for token in ("QS3DUPDATE", "UpdateCenterWindowHost.Show()", "QS3DUPDATE V26 error"):
    require(update_commands, token, "V26 update command")
for token in ("one-click update is intentionally disabled", "Do not install a V25 update package"):
    forbid(update_commands, token, "V26 update command")

for token in ("#if BRICSCAD_V26", "var hasPreviewDownload = false;", "#if !BRICSCAD_V26", "var progress = new Progress<UpdateDownloadProgress>(ApplyDownloadProgress);", "new VerifiedReleaseDownloader().DownloadAsync(release, progress)"):
    require(update_center, token, "shared Update Center V26 preview isolation")
for token in ("private bool _previewScheduled;", "private string? _previewScheduledDetail;", "private async System.Threading.Tasks.Task DownloadPreviewAsync"):
    require(update_center, token, "V25 preview scheduling implementation")
for token in ("_previewScheduled", "_previewScheduledDetail", "DownloadPreviewAsync"):
    forbid(v26_update_center, token, "V26 preprocessed Update Center")
for token in ("#if BRICSCAD_V26", '@"Software\\QS3D\\BricsCAD-V26\\Updates"', '@"Software\\QS3D\\BricsCAD-V25\\Updates"'):
    require(update_preferences, token, "shared host-major update preferences")

for text, label, manifest_asset in ((v25_release_client, "V25 release client", "QS3D-BricsCAD-V25.update.json"), (v26_release_client, "V26 release client", "QS3D-BricsCAD-V26.update.json")):
    require(text, manifest_asset, label)
    require(text, "if (manifestUri == null) continue;", label)
    require(text, "UpdateManifestAssetName", label)
require(v26_release_client, "QS3D-BricsCAD-V26-Updater", "V26 release client")
for token in ("QS3D-BricsCAD-V25.update.json", "QS3D-BricsCAD-V25.zip", "QS3D-BricsCAD-V25-Updater"):
    forbid(v26_release_client, token, "V26 release client")

for text, label in ((v26_release_client, "V26 release client"), (v26_manifest_probe, "V26 manifest probe")):
    for token in ("using System.Net.Http;", "HttpClient", "HttpClientHandler", "HttpCompletionOption.ResponseHeadersRead", "CancellationTokenSource", "Timeout.InfiniteTimeSpan"):
        require(text, token, label)
    for token in ("WebRequest.CreateHttp", "HttpWebRequest"):
        forbid(text, token, label)

for token in ('private const string Target = "BricsCAD V26 x64";', 'request.Headers.UserAgent.ParseAdd("QS3D-BricsCAD-V26-Updater")', '"QS3D-BricsCAD-V26.zip"', "GitHubReleaseClient.UpdateManifestAssetName", "schemaVersion 2"):
    require(v26_manifest_probe, token, "V26 manifest probe")
for token in ("BricsCAD V25 x64", "QS3D-BricsCAD-V25.zip", "QS3D-BricsCAD-V25.update.json"):
    forbid(v26_manifest_probe, token, "V26 manifest probe")

for token in ('UpdateMutexPrefix = "Global\\\\QS3D-BricsCAD-V26-Update-"', 'Path.Combine(installDirectory, "update-v26.ps1")', "TryVerifyAuthenticode", "WinVerifyTrust", "TryAcquireCrossProcessReservation", "WorkerReadyTimeoutMilliseconds", "-AllowedPackageHost @('github.com')", "-ExpectedSignerThumbprint $expectedSigner"):
    require(v26_launcher, token, "V26 secure update launcher")
for token in ("QS3D-BricsCAD-V25-Update-", "update-v25.ps1"):
    forbid(v26_launcher, token, "V26 secure update launcher")

for token in ("workflow_dispatch:", "github.event_name == 'workflow_dispatch'", "runs-on: [self-hosted, windows, x64, bricscad-v26]", "BRICSCAD_V26_DIR", "TD_MgdBrep.dll", "dotnet-version: \"8.0.x\"", "preflight-bricscad-v26.py", "QS3D.BricsCAD.V26.csproj", "test-bricscad-v26-runtime.ps1"):
    require(workflow, token, "V26 workflow")
for forbidden in ("\n  push:", "\n  pull_request:", "\n  schedule:", "\n  workflow_run:"):
    forbid(workflow, forbidden, "V26 workflow")

for token in ("FileMajorPart -ne 26", "QS3D.BricsCAD.V26.dll", "BrxMgd.dll", "TD_Mgd.dll", "QS3DRUNTIMEPROBE", "ribbon_ready", "palette_visible", "QS3D_RUNTIME_RESULT", "Assert-Qs3dV26DotNetRoot", "Assert-Qs3dV26InstalledDesktopRuntime", '[Environment]::GetEnvironmentVariable("DOTNET_ROOT", "Process")', '[Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)', 'Join-Path $root "host\\fxr"', 'Join-Path $root "shared\\Microsoft.NETCore.App"', 'Join-Path $installedRoot "shared\\Microsoft.WindowsDesktop.App"', 'Join-Path $_.FullName "hostfxr.dll"', 'Join-Path $_.FullName "coreclr.dll"', 'Join-Path $_.FullName "WindowsBase.dll"', 'Join-Path $_.FullName "System.Windows.Forms.dll"', "DOTNET_ROOT alone is insufficient"):
    require(runtime, token, "V26 runtime gate")
if "QS3D.BricsCAD.V25.dll" in runtime:
    errors.append("V26 runtime gate must reject/circumvent V25 adapter binaries, not load them")

require(runtime_probe, "QS3D BricsCAD runtime must be 64-bit.", "shared runtime probe")
for token in ("QS3D BricsCAD V25 runtime must be 64-bit.", "QS3D BricsCAD V26 runtime must be 64-bit."):
    forbid(runtime_probe, token, "shared runtime probe")

for token in ("#if BRICSCAD_V26", "private const int ExpectedRuntimeMajor = 26;", 'private const string ExpectedRuntimeLabel = "V26";', "private const int ExpectedRuntimeMajor = 25;", 'private const string ExpectedRuntimeLabel = "V25";', "var expectedRuntime = NativeRuntimeAssembliesMatch(brxAssembly, tdAssembly);", "private static bool NativeRuntimeAssembliesMatch", "if (Major(brxAssembly) != ExpectedRuntimeMajor || Major(tdAssembly) != ExpectedRuntimeMajor)", "expectedRuntime && x64Runtime && packageVersionMatches", '"NOT " + ExpectedRuntimeLabel', "diskVersionMatches", "diskFingerprintMatches"):
    require(runtime_diagnostics, token, "shared runtime diagnostics")
for token in ("var v25Runtime = Major(brxAssembly) == 25", '"V25 scenario suite', '(v25Runtime ? "V25" : "NOT V25")'):
    forbid(runtime_diagnostics, token, "shared runtime diagnostics")

for token in ("#if BRICSCAD_V26", 'private const string ExpectedRuntimeLabel = "V26";', 'private const string ExpectedRuntimeLabel = "V25";', 'ExpectedRuntimeLabel + " runtime/private-DWG gate'):
    require(release_readiness, token, "shared release readiness")
forbid(release_readiness, "V25 runtime/private-DWG gate vẫn là bước riêng.", "shared release readiness")

for token in ("LOCAL_ONLY", "DO_NOT_RETRY_REMOTE", "net8.0-windows", "BRICSCAD_V26_DIR", "QS3D.BricsCAD.V26.dll", "TD_MgdBrep.dll", "BricsCAD V26", ".NET 8"):
    require(qualification, token, "V26 local qualification")
for text, label in ((architecture, "architecture"), (ci_documentation, "CI documentation"), (manual_release, "manual V26 release documentation")):
    for token in ("BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"):
        require(text, token, label)

require(core, "<TargetFramework>netstandard2.0</TargetFramework>", "Core project")
require(build_props, "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", "Directory.Build.props")

print("QS3D BricsCAD V26 source compatibility preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: V25 remains net48; V26 uses the current Microsoft.NET.Sdk on net8.0-windows with an explicit Windows Desktop runtime configuration, a dedicated solution, the complete BrxMgd/TD_Mgd/TD_MgdBrep host reference set, V26-only runtime/update assets, HttpClient-only updater networking, manifest-channel-isolated release discovery, and helper-based host-major-aware shared runtime diagnostics with stale-binary identity checks.")
