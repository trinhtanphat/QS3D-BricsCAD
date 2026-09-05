#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"
HELPER = ROOT / "scripts" / "acquire-v26-compile-references.ps1"
MANUAL_WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"
CI_POLICY_GUARD = ROOT / "scripts" / "preflight-ci-manual-only.py"


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
ci_policy_guard = require_text(CI_POLICY_GUARD)

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
        "-AdmittedScript '.\\scripts\\publish-v26-release.ps1'",
        "GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}",
        "V26_RELEASE_REQUEST_PRERELEASE: 'true'",
        "V26_RELEASE_REQUEST_SIGN_PACKAGE: 'false'",
        "RELEASE_RUN_RUNTIME: 'false'",
    ),
)

# Candidate admission owns the single V26 publisher invocation. A standalone second
# publisher step would duplicate a transaction after candidate verification.
if re.search(r"(?m)^\s*run:\s*\.\\scripts\\publish-v26-release\.ps1\s*$", workflow):
    fail("release-v26-cloud.yml must not invoke publish-v26-release.ps1 outside candidate admission")

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

# New release workflows must be explicitly admitted by the fail-closed CI policy,
# rather than relying on the generic manual-workflow branch.
require_all(
    ci_policy_guard,
    CI_POLICY_GUARD,
    (
        'RELEASE_WORKFLOWS = {',
        '"release-v26-cloud.yml"',
    ),
)

print("V26 cloud preview release preflight passed.")
