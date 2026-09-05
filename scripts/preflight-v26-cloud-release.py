#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"
HELPER = ROOT / "scripts" / "acquire-v26-compile-references.ps1"
PROVENANCE_HELPER = ROOT / "scripts" / "new-v26-candidate-provenance.ps1"
CANDIDATE_HELPER = ROOT / "scripts" / "assert-v26-candidate-identity.ps1"
MANUAL_WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"
PINNED_HTTP_MIRROR = "http://103.9.157.20/BricsCAD-V26.2.07-1-en_US(x64).msi"
PINNED_HTTP_MIRROR_SWITCH = "-UsePinnedHttpMirror"


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
provenance_helper = require_text(PROVENANCE_HELPER)
candidate_helper = require_text(CANDIDATE_HELPER)
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
        "src\\QS3D.BricsCAD.V26\\QS3D.BricsCAD.V26.csproj",
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

# The V26 cloud lane may use one owner-approved plaintext mirror only after the
# canonical HTTPS source fails. The exact URL is owned inside the hardened helper;
# workflow source only opts into that fixed mirror so the repository-wide immutable
# Actions guard continues to reject all plaintext HTTP in workflow YAML.
require_all(workflow, WORKFLOW, (PINNED_HTTP_MIRROR_SWITCH,))
if workflow.count(PINNED_HTTP_MIRROR_SWITCH) != 2:
    fail("release-v26-cloud.yml must enable the pinned HTTP mirror at both installer acquisition call sites")
if "http://" in workflow or "-MirrorUrl" in workflow:
    fail("release-v26-cloud.yml must not embed or accept a plaintext HTTP mirror URL; the helper owns the exact mirror")
require_all(
    helper,
    HELPER,
    (
        "[switch]$UsePinnedHttpMirror",
        "Assert-PinnedV26HttpMirrorUrl",
        f"$expectedMirror = '{PINNED_HTTP_MIRROR}'",
        "if ($UsePinnedHttpMirror)",
        "Name = 'pinned-http-mirror'",
    ),
)
if "[string]$MirrorUrl" in helper:
    fail("V26 helper must not accept an arbitrary HTTP mirror URL parameter")

# The shared manual-only CI guard rejects OR expressions in job-level guards.
# Keep cache priming optional at step level, but every job itself is hard manual-only.
if "installer-cache:\n    if: ${{ github.event_name == 'workflow_dispatch' }}" not in workflow:
    fail("installer-cache job must use the repository's simple hard manual-dispatch guard")
for job in ("qualify", "release"):
    marker = f"  {job}:\n    if: ${{{{ github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE' }}}}"
    if marker not in workflow:
        fail(f"{job} job must require manual dispatch plus explicit RELEASE confirmation")

# V25 owns the unscoped shared preview tags. Cloud V26 publication must therefore
# append a V26-only prerelease identifier while package ProductVersion remains the
# source version. Package semantic validation uses the base source tag; provenance,
# candidate identity, and publisher keep using the scoped cloud publication tag.
require_all(
    workflow,
    WORKFLOW,
    (
        "V26_PACKAGE_TAG",
        "$packageTag = 'v' + $v26Versions[0]",
        "$expectedCloudTag = $packageTag + '.v26'",
        "V26 cloud release tag/source version mismatch. Expected $expectedCloudTag with matching Core version.",
        '"V26_PACKAGE_TAG=$packageTag" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append',
        "-ReleaseTag $env:V26_PACKAGE_TAG | Out-Null",
        "-PackageReleaseTag $env:V26_PACKAGE_TAG",
        "-ExpectedPackageReleaseTag $env:V26_PACKAGE_TAG",
    ),
)

package_identity_step = re.search(
    r"(?s)- name: Validate V26 preview package identity.*?scripts\\assert-v26-release-package-identity\.ps1.*?(?=\n\s*- name:)",
    workflow,
)
if package_identity_step is None:
    fail("release-v26-cloud.yml is missing the bounded V26 package identity validation step")
if "-ReleaseTag $env:RELEASE_TAG" in package_identity_step.group(0):
    fail("V26 package identity must use V26_PACKAGE_TAG, not the scoped cloud publication tag")

require_all(
    provenance_helper,
    PROVENANCE_HELPER,
    (
        "[string]$PackageReleaseTag",
        "$effectivePackageTag",
        "[string]::IsNullOrWhiteSpace($PackageReleaseTag)",
        "('v' + $productVersion), $effectivePackageTag",
        "releaseTag = $ReleaseTag",
    ),
)
require_all(
    candidate_helper,
    CANDIDATE_HELPER,
    (
        "[string]$ExpectedPackageReleaseTag",
        "$effectivePackageTag",
        "[string]::IsNullOrWhiteSpace($ExpectedPackageReleaseTag)",
        "('v' + [string]$metadata.productVersion), $effectivePackageTag",
        "[string]$provenance.releaseTag, $ExpectedReleaseTag",
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

# Existing manual V26 lane remains the canonical signed/licensed runtime path and
# deliberately relies on the helpers' backward-compatible default package tag.
require_all(
    manual,
    MANUAL_WORKFLOW,
    (
        "scripts\\assert-v26-candidate-identity.ps1",
        "scripts\\publish-v26-release.ps1",
    ),
)
if "ExpectedPackageReleaseTag" in manual or "PackageReleaseTag" in manual:
    fail("manual V26 lane must remain on the default release/package tag contract")

print("V26 cloud preview release preflight passed.")
