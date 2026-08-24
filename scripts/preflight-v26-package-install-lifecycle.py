from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "test-v26-package-install-lifecycle.ps1"
PACKAGE = ROOT / "scripts" / "package-v26.ps1"
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
    "status --porcelain --untracked-files=no",
    "package-v26.ps1",
    "PACKAGE-METADATA.json",
    "SHA256SUMS.txt",
    "BricsCAD V26 x64",
    "net8.0-windows",
    "install-v26-autoload.ps1",
    "uninstall-v26-autoload.ps1",
    "registrationCreated",
    "installedPayloadValid",
    "uninstallRemovedRegistration",
    "uninstallRemovedFiles",
    "unrelatedSentinelPreserved",
    "cleanupComplete",
    "v26-package-install-lifecycle.json",
])
require(PACKAGE, [
    "install-v26-autoload.ps1",
    "uninstall-v26-autoload.ps1",
    "QS3D-BricsCAD-V26.zip",
])
require(V26_DOC, ["clean-machine V26 package install", "test-v26-package-install-lifecycle.ps1"])
require(INBOX, ["V26 package install/uninstall lifecycle", "PENDING_LOCAL"])

text = RUNNER.read_text(encoding="utf-8") if RUNNER.is_file() else ""
for forbidden in ("Start-Process bricscad", "NETLOAD", "LOCAL_PASS", "private DWG"):
    if forbidden in text:
        errors.append(f"runner must not claim/perform licensed runtime boundary: {forbidden}")

if errors:
    print("V26 package install lifecycle preflight: FAIL")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print("V26 package install lifecycle preflight: PASS")
