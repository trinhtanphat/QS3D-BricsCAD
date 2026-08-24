from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "test-v26-package-install-lifecycle.ps1"
PACKAGE = ROOT / "scripts" / "package-v26.ps1"
GENERATOR = ROOT / "scripts" / "new-v26-script-from-v25.ps1"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"
V26_DOC = ROOT / "docs" / "LOCAL-V26-QUALIFICATION.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors: list[str] = []


def require(path: Path, tokens: list[str]) -> None:
    if not path.is_file():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")
        return
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            errors.append(f"{path.relative_to(ROOT)} missing contract token: {token}")


require(RUNNER, [
    "ExpectedSourceSha",
    "ConfirmDisposableInstall",
    "status --porcelain",
    "completely clean working tree",
    "BRICSCAD_V26_DIR",
    "dotnet build",
    "package-v26.ps1",
    "PACKAGE-METADATA.json",
    "SHA256SUMS.txt",
    "BricsCAD V26 x64",
    "net8.0-windows",
    "QS3D.BricsCAD.V26.runtimeconfig.json",
    "Microsoft.WindowsDesktop.App",
    "install-v26-autoload.ps1",
    "uninstall-v26-autoload.ps1",
    "registrationCreated",
    "registrationV26Only",
    "registrationIdentityValid",
    "installedPayloadValid",
    "installedPayloadHashesMatch",
    "runtimeConfigInstalled",
    "uninstallRemovedRegistration",
    "uninstallRemovedFiles",
    "unrelatedV25RegistrationPreserved",
    "unrelatedSentinelPreserved",
    "cleanupComplete",
    "v26-package-install-lifecycle.json",
])
require(PACKAGE, [
    "QS3D.BricsCAD.V26.runtimeconfig.json",
    "install-v26-autoload.ps1",
    "uninstall-v26-autoload.ps1",
    "QS3D-BricsCAD-V26.zip",
])
require(GENERATOR, [
    "install-v25-autoload.ps1",
    "QS3D.BricsCAD.V26.runtimeconfig.json",
    "Generated V26 installer payload anchor changed",
])
require(V26_PROJECT, [
    "<TargetFramework>net8.0-windows</TargetFramework>",
    "<GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>",
])
require(V26_DOC, ["V26 clean-machine package install/uninstall", "test-v26-package-install-lifecycle.ps1"])
require(INBOX, ["V26 clean-machine package install/uninstall", "PENDING_LOCAL"])

runner_text = RUNNER.read_text(encoding="utf-8") if RUNNER.is_file() else ""
for forbidden in ("Start-Process bricscad", "NETLOAD", "LOCAL_PASS", "private DWG", "$IsWindows"):
    if forbidden in runner_text:
        errors.append(f"runner must not claim/perform licensed runtime boundary or require PowerShell 7-only host detection: {forbidden}")

# This runner validates the exact generated V26 package and installation state. It
# must not silently weaken the production installer/uninstaller or fabricate local
# runtime evidence merely to make the source gate green.
if "-Force" in runner_text:
    errors.append("qualification runner must not bypass installer/uninstaller ownership guards with -Force")

if errors:
    print("V26 package install lifecycle preflight: FAIL")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print("V26 package install lifecycle preflight: PASS")
