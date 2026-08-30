# Reservation-v2 scope includes this shared UX compatibility guard for #4675.
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs"
RUNTIME_DIAGNOSTICS = ROOT / "src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs"
UPDATE_CENTER = ROOT / "src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"
INSTALL = ROOT / "scripts/INSTALL-QS3D.cmd"
INSTALL_PS1 = ROOT / "scripts/install-v25-autoload.ps1"
PACKAGE = ROOT / "scripts/package-v25.ps1"
errors = []


def console_safe(value: object) -> str:
    text = str(value)
    encoding = sys.stdout.encoding or "utf-8"
    return text.encode(encoding, errors="backslashreplace").decode(encoding)


for path in (COMMANDS, RUNTIME_DIAGNOSTICS, UPDATE_CENTER, INSTALL, INSTALL_PS1, PACKAGE):
    if not path.is_file():
        errors.append("missing V25 NETLOAD/update UX contract file: " + str(path.relative_to(ROOT)))

version_registration = '[CommandMethod("QS3DVERSION", CommandFlags.Modal)]'

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DUPDATE", CommandFlags.Modal)]',
        '[CommandMethod("QSUPDATE", CommandFlags.Modal)]',
        '[CommandMethod("QS3DVER", CommandFlags.Modal)]',
        '[CommandMethod("QSVER", CommandFlags.Modal)]',
        'WriteVersionCore("QS3DVER")',
        'WriteVersionCore("QSVER")',
        "typeof(global::QS3D.BricsCAD.V25.RuntimeDiagnosticsCommands).Assembly",
        "ProductVersionText(assembly)",
        "AssemblyInformationalVersionAttribute",
        "assembly.Location",
        "Version source: loaded QS3D assembly (not updater cache or GitHub metadata).",
        "Run QS3DUPDATE to check GitHub Releases.",
    ):
        if token not in text:
            errors.append("UpdateCommands.cs missing customer update/version contract: " + token)
    if version_registration in text:
        errors.append("UpdateCommands.cs must not re-register canonical QS3DVERSION")
    for stale_token in (
        "Assembly.GetExecutingAssembly()",
        "UpdateCoordinator.Instance.LastResult",
        "result.CurrentVersion",
    ):
        if stale_token in text:
            errors.append("UpdateCommands.cs version aliases must use the loaded QS3D assembly, not updater/cache identity: " + stale_token)

if RUNTIME_DIAGNOSTICS.is_file():
    text = RUNTIME_DIAGNOSTICS.read_text(encoding="utf-8")
    if text.count(version_registration) != 1:
        errors.append("RuntimeDiagnosticsCommands.cs must own exactly one canonical QS3DVERSION registration")
    for token in (
        "public void VersionCheck()",
        "WriteVersionSummary();",
        '[CommandMethod("QS3DRUNTIMECHECK", CommandFlags.Modal)]',
        "Run QS3DRUNTIMECHECK for full runtime/package verification.",
    ):
        if token not in text:
            errors.append("RuntimeDiagnosticsCommands.cs missing canonical concise QS3DVERSION contract: " + token)
    if "public void VersionCheck()\n        {\n            RuntimeCheck();" in text:
        errors.append("RuntimeDiagnosticsCommands.cs must keep full runtime/package verification behind QS3DRUNTIMECHECK")

if UPDATE_CENTER.is_file():
    text = UPDATE_CENTER.read_text(encoding="utf-8")
    for token in (
        "using System.Reflection;",
        'Title = "QS3D Update Center — " + currentDisplay;',
        '_title.Text = "Cập nhật QS3D " + currentDisplay;',
        "ApplyVersionHighlights(currentDisplay, latest);",
        'var assembly = Assembly.GetExecutingAssembly();',
        'var loadedPath = string.IsNullOrWhiteSpace(assembly.Location)',
        'var buildIdentity = GetBuildIdentity(currentOriginal);',
        '_runtimeIdentity.Text = string.IsNullOrWhiteSpace(buildIdentity)',
        '? "DLL đang chạy: " + loadedPath',
        ': "Build: " + buildIdentity + "    •    DLL đang chạy: " + loadedPath;',
        '_runtimeIdentity.ToolTip = "Product version đầy đủ: " + currentOriginal + "\\n" + loadedPath;',
    ):
        if token not in text:
            errors.append("UpdateCenterWindow.cs missing visible runtime-version identity: " + token)

if INSTALL.is_file():
    text = INSTALL.read_text(encoding="utf-8")
    for token in (
        "-ExecutionPolicy RemoteSigned",
        "Get-AuthenticodeSignature",
        "Unblock-File -LiteralPath $p -ErrorAction Stop",
        "install-v25-autoload.ps1",
    ):
        if token not in text:
            errors.append("INSTALL-QS3D.cmd missing secure bootstrap contract: " + token)
    lowered = text.lower()
    if "executionpolicy bypass" in lowered or "-executionpolicy bypass" in lowered:
        errors.append("INSTALL-QS3D.cmd must not bypass PowerShell execution policy")

if INSTALL_PS1.is_file():
    text = INSTALL_PS1.read_text(encoding="utf-8")
    for token in (
        "Assert-PackageIntegrity -Directory $package",
        "Assert-PackageIdentity -Directory $package",
        "Unblock-File -LiteralPath $destination -ErrorAction Stop",
        "BricsCAD V25",
    ):
        if token not in text:
            errors.append("install-v25-autoload.ps1 missing integrity/MOTW install contract: " + token)

if PACKAGE.is_file():
    text = PACKAGE.read_text(encoding="utf-8")
    for token in (
        "INSTALL-QS3D.cmd",
        "Recommended install (avoids .NET 0x80131515 / Mark-of-the-Web NETLOAD failures)",
        "QS3DUPDATE",
        "update-v25.ps1",
    ):
        if token not in text:
            errors.append("package-v25.ps1 missing customer package/update guidance: " + token)

if errors:
    print("V25 NETLOAD/update UX preflight FAILED:")
    for error in errors:
        print(" - " + console_safe(error))
    sys.exit(1)

print("PASS: V25 package keeps one canonical loaded-binary QS3DVERSION diagnostic command, the integrity-checked Mark-of-the-Web install path, secure GitHub Update Center, visible UI version/loaded-DLL identity, and short update/version aliases.")
