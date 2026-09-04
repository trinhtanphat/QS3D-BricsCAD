#!/usr/bin/env python3
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeDiagnosticsCommands.cs"
PLUGIN_ENTRIES = [
    ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs",
    ROOT / "src" / "QS3D.BricsCAD.V26" / "PluginEntry.cs",
]
UPDATE_COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates" / "UpdateCoordinator.cs"
UPDATE_CENTER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates" / "UpdateCenterWindow.cs"
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"
PACKAGE_V25 = ROOT / "scripts" / "package-v25.ps1"
PACKAGE_V26 = ROOT / "scripts" / "package-v26.ps1"
BUILD_PROPS = ROOT / "Directory.Build.props"
RUNTIME_SOURCE_IDENTITY = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeSourceIdentityGuard.cs"
EXACT_SOURCE_PROBES = [
    ROOT / "src" / "QS3D.BricsCAD.V25" / "LevelZRuntimeProbeCommands.cs",
    ROOT / "src" / "QS3D.BricsCAD.V25" / "LevelZLifecycleRuntimeProbeCommands.cs",
    ROOT / "src" / "QS3D.BricsCAD.V25" / "CurvedStructuralRuntimeProbeCommands.cs",
]
PROJECTS = [
    ROOT / "src" / "QS3D.BricsCAD.V25" / "QS3D.BricsCAD.V25.csproj",
    ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj",
    ROOT / "src" / "QS3D.Core" / "QS3D.Core.csproj",
]
PLUGIN_PROJECTS = PROJECTS[:2]
CORE_PROJECT = PROJECTS[2]


def fail(message):
    print("ERROR:", message)
    return 1


def single_text(root, name, path):
    values = [(node.text or "").strip() for node in root.iter(name) if (node.text or "").strip()]
    if len(values) != 1:
        raise ValueError(f"{path} must declare exactly one {name}; found {len(values)}")
    return values[0]


def read_identity(path):
    root = ET.parse(path).getroot()
    return {
        "Version": single_text(root, "Version", path),
        "AssemblyVersion": single_text(root, "AssemblyVersion", path),
        "FileVersion": single_text(root, "FileVersion", path),
        "InformationalVersion": single_text(root, "InformationalVersion", path),
    }


def main():
    try:
        identities = {path: read_identity(path) for path in PROJECTS}
    except (OSError, ET.ParseError, ValueError) as exc:
        return fail(str(exc))

    # Protected-main preview ordinals are committed on the V25/V26 plugin projects
    # before Core is synchronized in the release workspace. Runtime-facing V25/V26
    # identities must still advance together exactly.
    plugin_product_versions = {identities[path]["Version"] for path in PLUGIN_PROJECTS}
    if len(plugin_product_versions) != 1:
        return fail("V25 and V26 product versions must stay identical: " + repr(sorted(plugin_product_versions)))

    assembly_versions = {identity["AssemblyVersion"] for identity in identities.values()}
    if len(assembly_versions) != 1:
        return fail("V25, V26 and Core assembly versions must stay identical: " + repr(sorted(assembly_versions)))

    for path, identity in identities.items():
        if identity["InformationalVersion"] != identity["Version"]:
            return fail(f"{path} InformationalVersion must equal Version exactly")

        preview = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)-preview\.(\d+)", identity["Version"])
        if preview:
            expected_file = ".".join(preview.groups()[:3]) + "." + preview.group(4)
            if identity["FileVersion"] != expected_file:
                return fail(
                    f"{path} FileVersion must identify preview build {identity['Version']}; "
                    f"expected {expected_file}, got {identity['FileVersion']}"
                )

    build_props = BUILD_PROPS.read_text(encoding="utf-8")
    if "<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>" not in build_props:
        return fail("Directory.Build.props must prevent SDK-added git metadata from changing declared product identity")

    source_identity = RUNTIME_SOURCE_IDENTITY.read_text(encoding="utf-8")
    required_source_identity_tokens = [
        'Path.ChangeExtension(assemblyPath, ".pdb")',
        "File.ReadAllBytes(pdbPath)",
        "https://raw.githubusercontent.com/trinhtanphat/QS3D-BricsCAD/",
        "sourceLinkPrefix",
        "StringComparison.OrdinalIgnoreCase",
    ]
    for token in required_source_identity_tokens:
        if token not in source_identity:
            return fail(f"runtime exact-source identity lost PDB SourceLink guard: {token}")

    for probe_path in EXACT_SOURCE_PROBES:
        probe = probe_path.read_text(encoding="utf-8")
        if "RuntimeSourceIdentityGuard.RequireExactSourceLink(assembly, sourceSha, label);" not in probe:
            return fail(f"{probe_path} must bind runtime evidence to PDB SourceLink")
        if 'EndsWith("+" + sourceSha' in probe or "AssemblyInformationalVersionAttribute" in probe:
            return fail(f"{probe_path} must not overload public ProductVersion with exact-source identity")

    runtime = RUNTIME.read_text(encoding="utf-8")
    required_runtime_tokens = [
        '[CommandMethod("QS3DVERSION"',
        "ProductVersionText(pluginAssembly)",
        "AssemblyInformationalVersionAttribute",
        "FileVersionInfo.GetVersionInfo(path)",
        "pluginAssembly.ManifestModule.ModuleVersionId",
        "Process.GetCurrentProcess()",
        "ProductVersionsEqual(metadata.ProductVersion, pluginProductVersion)",
        "AssemblyVersionsEqual(metadata.AssemblyVersion, pluginAssemblyVersion)",
        "CaptureLoadedBinaryIdentity()",
        "SHA256.Create()",
        "FileShare.ReadWrite | FileShare.Delete",
        "diskFingerprintMatches",
        "Loaded-at-start SHA256",
        "STALE PROCESS",
        ".NET does not hot-reload an already NETLOADed QS3D assembly",
        "diskVersionMatches",
        "Math.Max(0, version.Build)",
        "Math.Max(0, version.Revision)",
    ]
    for token in required_runtime_tokens:
        if token not in runtime:
            return fail(f"runtime version diagnostics lost required guard: {token}")

    stale_assembly_only_comparison = "NormalizeVersion(metadata.AssemblyVersion), NormalizeVersion(pluginVersion)"
    if stale_assembly_only_comparison in runtime:
        return fail("runtime diagnostics regressed to assembly-only package version comparison")
    if "version.ToString(4)" in runtime:
        return fail("runtime diagnostics must not call Version.ToString(4) on a version that may omit revision")

    ui_startup_tokens = (
        "RibbonInitializationCoordinator.Start();",
        "PaletteCoordinator.EnsureCreated();",
    )
    runtime_startup_tokens = ui_startup_tokens + (
        "DocumentLifecycleCoordinator.Start();",
        "RibbonBootstrapper.TryInitialize();",
        "UpdateBootstrapper.Start();",
    )
    for plugin_entry_path in PLUGIN_ENTRIES:
        plugin_entry = plugin_entry_path.read_text(encoding="utf-8")
        capture = plugin_entry.find("RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity();")
        ui_positions = [plugin_entry.find(token) for token in ui_startup_tokens]
        ui_positions = [position for position in ui_positions if position >= 0]
        startup_positions = [plugin_entry.find(token) for token in runtime_startup_tokens]
        startup_positions = [position for position in startup_positions if position >= 0]
        if capture < 0 or not ui_positions or not startup_positions or capture > min(startup_positions):
            return fail(f"{plugin_entry_path} must capture the loaded QS3D binary fingerprint before recognized UI/runtime startup")

    update_coordinator = UPDATE_COORDINATOR.read_text(encoding="utf-8")
    if "AssemblyInformationalVersionAttribute" not in update_coordinator or "SemanticReleaseVersion.FromRunningVersion(informational" not in update_coordinator:
        return fail("UpdateCoordinator must derive the running product version from AssemblyInformationalVersion")

    update_center = UPDATE_CENTER.read_text(encoding="utf-8")
    if 'result.CurrentVersion?.Original ?? "unknown"' not in update_center:
        return fail("Update Center must display the semantic running product version, not AssemblyVersion")

    installer = INSTALLER.read_text(encoding="utf-8")
    if "Get-RunningBricsCADProcessDetails" not in installer or "Close all BricsCAD processes before installing or upgrading QS3D" not in installer:
        return fail("V25 installer must refuse install/upgrade while any BricsCAD process is running")

    for package_path in (PACKAGE_V25, PACKAGE_V26):
        package = package_path.read_text(encoding="utf-8")
        if "productVersion" not in package or "version = $assemblyVersion.ToString()" not in package:
            return fail(f"{package_path} must persist product and assembly identities separately")

    plugin_version = next(iter(plugin_product_versions))
    core_version = identities[CORE_PROJECT]["Version"]
    assembly = next(iter(assembly_versions))
    print("PASS: runtime product-version identity is guarded")
    print("V25/V26 product version:", plugin_version)
    print("Core product version:", core_version)
    print("Assembly version:", assembly)
    print("V25/V26 QS3DVERSION/QS3DRUNTIMECHECK detect stale same-path DLL replacement by semantic version and startup SHA-256 fingerprint.")
    print("Update Center displays the semantic product version from AssemblyInformationalVersion.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
