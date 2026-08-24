#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "test-v26-package-update-lifecycle.ps1"
PACKAGER = ROOT / "scripts" / "package-v26.ps1"
UPDATER = ROOT / "scripts" / "update-v25.ps1"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
V26 = ROOT / "docs" / "LOCAL-V26-QUALIFICATION.md"


def require(path: Path, tokens: list[str]) -> str:
    if not path.is_file():
        raise AssertionError(f"missing required file: {path.relative_to(ROOT)}")
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            raise AssertionError(f"{path.relative_to(ROOT)} missing required token: {token}")
    return text


def main() -> int:
    runner = require(
        RUNNER,
        [
            "ExpectedSourceSha",
            "UpgradeManifestUri",
            "RollbackManifestUri",
            "ExpectedSignerThumbprint",
            "ConfirmDisposableInstall",
            "Assert-CleanExactSource",
            "Assert-HostIdentity",
            "package-v26.ps1",
            "install-v26-autoload.ps1",
            "update-v26.ps1",
            "rollbackRejected",
            "rollbackPreservedState",
            "cancelPreservedState",
            "unrelatedSentinelPreserved",
            "-AllowSameVersion -WhatIf",
            "v26-package-update-lifecycle.json",
            "QS3D_V26_PACKAGE_UPDATE_LIFECYCLE",
        ],
    )
    if "ExpectedSourceSha does not match exact Git HEAD." not in runner:
        raise AssertionError("runner must fail closed on exact-source mismatch")
    if "Close all BricsCAD processes" not in runner:
        raise AssertionError("runner must refuse package mutation while BricsCAD is running")
    if not re.search(r"Get-FileHash.+SHA256", runner):
        raise AssertionError("runner must verify installed payload hashes")
    if runner.count("Get-TreeDigest $installDir") < 3:
        raise AssertionError("runner must compare upgraded payload state across rollback and cancel paths")

    packager = require(
        PACKAGER,
        [
            "'update-v25.ps1' = 'update-v26.ps1'",
            "new-v26-script-from-v25.ps1",
            "Generated V26 release script leaked a V25 token",
            "SHA256SUMS.txt",
        ],
    )
    if "update-v26.ps1" not in packager:
        raise AssertionError("V26 package must contain the generated updater")

    require(
        UPDATER,
        [
            "ValidatePattern('^https://')",
            "ExpectedSignerThumbprint",
            "Enter-Qs3dUpdateMutex",
            "MaxPackageSizeMB",
            "MaxExpandedPackageSizeMB",
            "MaxArchiveEntries",
            "Compare-StrictSemVer",
            "Get-AuthenticodeSignature",
            "Assert-OfficialGitHubPackageSnapshot",
            "AllowSameVersion",
        ],
    )
    require(
        INBOX,
        [
            "test-v26-package-update-lifecycle.ps1",
            "PENDING_LOCAL",
        ],
    )
    require(
        V26,
        [
            "test-v26-package-update-lifecycle.ps1",
            "RollbackManifestUri",
            "ExpectedSignerThumbprint",
            "PENDING_LOCAL",
        ],
    )

    forbidden = ["LOCAL_PASS", "runtime PASS", "BricsCAD launched successfully"]
    for token in forbidden:
        if token in runner:
            raise AssertionError(f"runner must not fabricate licensed runtime evidence: {token}")

    print("V26 package update lifecycle source guard: PASS")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"V26 package update lifecycle source guard: FAIL: {exc}", file=sys.stderr)
        raise SystemExit(1)
