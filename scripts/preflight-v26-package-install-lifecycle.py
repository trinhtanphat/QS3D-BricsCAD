from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "test-v26-package-install-lifecycle.ps1"
PACKAGE = ROOT / "scripts" / "package-v26.ps1"
GENERATOR = ROOT / "scripts" / "new-v26-script-from-v25.ps1"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"
RUNBOOK = ROOT / "docs" / "LOCAL-V26-PACKAGE-INSTALL-LIFECYCLE.md"

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
    "^V26(?:x64)?(?:\\.|$)",
    "dotnet build",
    "package-v26.ps1",
    "PACKAGE-METADATA.json",
    "SHA256SUMS.txt",
    "BricsCAD V26 x64",
    "net8.0-windows",
    "QS3D.BricsCAD.V26.runtimeconfig.json",
    "Get-RuntimeFrameworkNames",
    "PSObject.Properties['runtimeOptions']",
    "PSObject.Properties['frameworks']",
    "PSObject.Properties['name']",
    "-is [System.Array]",
    "duplicate framework name",
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
require(RUNBOOK, [
    "PENDING_LOCAL",
    "test-v26-package-install-lifecycle.ps1",
    "V26x64",
    "QS3D.BricsCAD.V26.runtimeconfig.json",
    "unrelated V25",
    "LOCAL_PASS",
])

runner_text = RUNNER.read_text(encoding="utf-8") if RUNNER.is_file() else ""
for forbidden in ("Start-Process bricscad", "NETLOAD", "LOCAL_PASS", "private DWG", "$IsWindows"):
    if forbidden in runner_text:
        errors.append(f"runner must not claim/perform licensed runtime boundary or require PowerShell 7-only host detection: {forbidden}")

# Regression for #3878: generated net8.0-windows runtimeconfig uses the
# runtimeOptions.frameworks array. The old singular property access must never
# return because it fails under Set-StrictMode on the real generated payload.
if "runtimeOptions.framework.name" in runner_text:
    errors.append("runner must not read the obsolete singular runtimeOptions.framework property")
for token in (
    "$RuntimeConfig.PSObject.Properties['runtimeOptions']",
    "$runtimeOptionsProperty.Value.PSObject.Properties['frameworks']",
    "$framework.PSObject.Properties['name']",
    "runtimeOptions.frameworks must be an array",
    "runtimeOptions.frameworks is empty",
):
    if token not in runner_text:
        errors.append(f"runner missing fail-closed runtimeconfig array-shape regression token: {token}")

# The canonical calls intentionally omit the installer's/uninstaller's ownership-bypass
# switch. Remove-Item -Force is cleanup only and is not a package-identity bypass.
for forbidden_call in ("$installer -Force", "$uninstaller -Force"):
    if forbidden_call in runner_text:
        errors.append(f"qualification runner must not bypass package ownership guards: {forbidden_call}")

if errors:
    print("V26 package install lifecycle preflight: FAIL")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print("V26 package install lifecycle preflight: PASS")