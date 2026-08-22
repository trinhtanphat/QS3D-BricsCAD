#!/usr/bin/env python3
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
V25_LAUNCHER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates" / "SecureUpdateLauncher.cs"
V26_LAUNCHER = ROOT / "src" / "QS3D.BricsCAD.V26" / "Updates" / "SecureUpdateLauncher.cs"
INSTALLER_TEMPLATE = ROOT / "scripts" / "install-v25-autoload.ps1"
UPDATER_TEMPLATE = ROOT / "scripts" / "update-v25.ps1"
V25_PACKAGE = ROOT / "scripts" / "package-v25.ps1"
V26_PACKAGE = ROOT / "scripts" / "package-v26.ps1"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing DemandLoad preservation surface: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"{label} is missing required token: {token}")


def require_order(text: str, before: str, after: str, label: str) -> None:
    before_at = text.find(before)
    after_at = text.find(after)
    if before_at < 0 or after_at < 0 or before_at >= after_at:
        raise AssertionError(f"{label} requires '{before}' before '{after}'")


def check_launcher(text: str, host_major: str) -> None:
    label = f"V{host_major} secure updater launcher"
    registry_root = f'private const string DemandLoadRegistryRoot = @"Software\\Bricsys\\BricsCAD\\V{host_major}x64";'
    for token in (
        "using Microsoft.Win32;",
        registry_root,
        "private const int MaximumRegistrySubKeys = 64;",
        "TryResolveRegisteredLoadMode(pluginPath, out var loadMode, out error)",
        "Registry.CurrentUser.OpenSubKey(DemandLoadRegistryRoot, writable: false)",
        "Array.Sort(languageNames, StringComparer.OrdinalIgnoreCase);",
        'languageName + @"\\Applications\\QS3D"',
        'appKey.GetValueKind("Loader") != RegistryValueKind.String',
        'appKey.GetValueKind("LoadCtrls") != RegistryValueKind.DWord',
        "RegistryValueOptions.DoNotExpandEnvironmentNames",
        "!Path.IsPathRooted(loader)",
        "Path.GetFullPath(loader), canonicalPluginPath, StringComparison.OrdinalIgnoreCase",
        "loadCtrls != 2 && loadCtrls != 4",
        "registeredLoadCtrls.Value != loadCtrls",
        'registeredLoadCtrls.Value == 2 ? "OnStartup" : "OnCommand"',
        "string loadMode,",
        'script.AppendLine("$loadMode = " + PsLiteral(loadMode));',
        "-InstallDirectory $install -LoadMode $loadMode -AllowedPackageHost @('github.com')",
    ):
        require(text, token, label)

    require_order(
        text,
        "TryResolveRegisteredLoadMode(pluginPath, out var loadMode, out error)",
        "Interlocked.CompareExchange(ref _scheduled, 1, 0)",
        f"{label} must freeze the verified mode before scheduling",
    )
    require_order(
        text,
        'script.AppendLine("$loadMode = " + PsLiteral(loadMode));',
        "-LoadMode $loadMode",
        f"{label} must freeze the mode before invoking the updater",
    )


def main() -> int:
    check_launcher(read(V25_LAUNCHER), "25")
    check_launcher(read(V26_LAUNCHER), "26")

    for path, label in (
        (INSTALLER_TEMPLATE, "new/manual V25 installer default"),
        (UPDATER_TEMPLATE, "manual V25 updater default"),
    ):
        text = read(path)
        require(text, "[ValidateSet('OnCommand', 'OnStartup')]", label)
        require(text, "[string]$LoadMode = 'OnCommand'", label)

    for path, label in (
        (V25_PACKAGE, "V25 package metadata default"),
        (V26_PACKAGE, "V26 package metadata default"),
    ):
        require(read(path), "defaultLoadMode = 'OnCommand'", label)

    print(
        "PASS: V25/V26 one-click updates preserve an unambiguous current-DLL DemandLoad mode; "
        "stale, malformed, missing, or mixed registrations fail closed, while new/manual installs remain OnCommand by default."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
