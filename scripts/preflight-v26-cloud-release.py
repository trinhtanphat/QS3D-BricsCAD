#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"
HELPER = ROOT / "scripts" / "acquire-v26-compile-references.ps1"
MANUAL_WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"


def fail(message: str) -> None:
    print(f"ERROR: V26 cloud preview release preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require_text(path: Path) -> str:
    if not path.is_file():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require_all(text: str, path: Path, needles: tuple[str, ...]) -> None:
    for needle in needles:
        if needle not in text:
            fail(f"{path.relative_to(ROOT)} is missing required contract token: {needle}")


workflow = require_text(WORKFLOW)
helper = require_text(HELPER)
manual = require_text(MANUAL_WORKFLOW)

require_all(
    workflow,
    WORKFLOW,
    (
        "name: QS3D Cloud V26 Preview Build & Release",
        "workflow_dispatch:",
        "source_sha:",
        "release_tag:",
        "confirm_release:",
        "windows-latest",
        "BRICSCAD_V26_PINNED_MSI_SHA256",
        "BricsCAD-V26.2.07-x64.msi",
        "bricscad-v26.2.07-x64-en-us-",
        "actions/cache/restore@",
        "actions/cache/save@",
        "scripts\\acquire-v26-compile-references.ps1",
        "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
        "scripts\\package-v26.ps1",
        "scripts\\write-v26-package-checksum.ps1",
        "scripts\\new-v26-candidate-provenance.ps1",
        "scripts\\assert-v26-candidate-identity.ps1",
        "scripts\\publish-v26-release.ps1",
        "V26_RELEASE_REQUEST_PRERELEASE: 'true'",
        "V26_RELEASE_REQUEST_SIGN_PACKAGE: 'false'",
        "RELEASE_RUN_RUNTIME: 'false'",
    ),
)

# The committed workflow must not embed the owner's short-lived signed capability.
for forbidden in ("GoogleAccessId=", "Signature=", "Expires="):
    if forbidden in workflow:
        fail(f"committed workflow must not embed expiring signed installer query material: {forbidden}")

require_all(
    helper,
    HELPER,
    (
        "BricsCAD V26",
        "26.2.07",
        "ExpectedSha256",
        "Get-AuthenticodeSignature",
        "Bricsys",
        "ProductVersion",
        "BrxMgd.dll",
        "TD_Mgd.dll",
        "TD_MgdBrep.dll",
        "msiexec.exe",
    ),
)

require_all(
    manual,
    MANUAL_WORKFLOW,
    (
        "scripts\\assert-v26-candidate-identity.ps1",
        "scripts\\publish-v26-release.ps1",
    ),
)

print("V26 cloud preview release preflight passed.")
