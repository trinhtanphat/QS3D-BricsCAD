from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs"
INSTALL = ROOT / "scripts/INSTALL-QS3D.cmd"
INSTALL_PS1 = ROOT / "scripts/install-v25-autoload.ps1"
PACKAGE = ROOT / "scripts/package-v25.ps1"
errors = []

for path in (COMMANDS, INSTALL, INSTALL_PS1, PACKAGE):
    if not path.is_file():
        errors.append("missing V25 NETLOAD/update UX contract file: " + str(path.relative_to(ROOT)))

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DUPDATE", CommandFlags.Modal)]',
        '[CommandMethod("QSUPDATE", CommandFlags.Modal)]',
        '[CommandMethod("QS3DVER", CommandFlags.Modal)]',
        '[CommandMethod("QSVER", CommandFlags.Modal)]',
        "Assembly.GetExecutingAssembly()",
        "assembly.Location",
        "UpdateCoordinator.Instance.LastResult",
        "Run QSUPDATE or QS3DUPDATE to check GitHub Releases.",
    ):
        if token not in text:
            errors.append("UpdateCommands.cs missing customer update/version contract: " + token)

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
        print(" - " + error)
    sys.exit(1)

print("PASS: V25 package keeps the integrity-checked Mark-of-the-Web install path, secure GitHub Update Center, short update aliases, and loaded-version/path diagnostics.")
