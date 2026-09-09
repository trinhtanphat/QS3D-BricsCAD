#!/usr/bin/env python3
from pathlib import Path
import os
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
HARNESS = ROOT / "tests" / "QS3D.LocalQualification.MultiRegion"
CONTRACT = HARNESS / "test-runner-contract.ps1"
RUNNER = HARNESS / "Run-Local005NativeMultiRegion.ps1"
PASS_MARKER = "PASS LOCAL-005 runner contract"


def require_file(path: Path) -> None:
    if not path.is_file():
        raise RuntimeError(f"LOCAL-005 host-free contract input is missing: {path.relative_to(ROOT)}")


def require_fail_closed_cleanup_source() -> None:
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        "$zeroHosts = $false",
        "$zeroHosts = $true",
        "if ($zeroHosts) {",
        "PROFILE:SKIPPED_WHILE_HOST_ACTIVE",
        "PRIVATE_ROOT:SKIPPED_WHILE_HOST_ACTIVE",
    )
    missing = [token for token in required if token not in text]
    if missing:
        raise RuntimeError("LOCAL-005 fail-closed host cleanup contract is incomplete: " + ", ".join(missing))


require_file(CONTRACT)
require_file(RUNNER)
require_fail_closed_cleanup_source()

if os.name != "nt":
    # Shared CI executes this guard on windows-latest and therefore runs the
    # committed PowerShell contract below. Keep non-Windows aggregate preflight
    # usable while still pinning the critical fail-closed cleanup source shape.
    print("PASS LOCAL-005 native runner source contract (PowerShell execution requires Windows)")
    raise SystemExit(0)

pwsh = shutil.which("pwsh")
if not pwsh:
    raise RuntimeError("pwsh is required to execute the LOCAL-005 host-free runner contract on Windows")

try:
    completed = subprocess.run(
        [pwsh, "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(CONTRACT)],
        cwd=ROOT,
        stdin=subprocess.DEVNULL,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        encoding="utf-8",
        errors="replace",
        timeout=60,
        check=False,
    )
except subprocess.TimeoutExpired as exc:
    raise RuntimeError("LOCAL-005 host-free runner contract timed out") from exc

if completed.stdout:
    sys.stdout.write(completed.stdout)
if completed.returncode != 0:
    raise RuntimeError(f"LOCAL-005 host-free runner contract failed with exit {completed.returncode}")
if PASS_MARKER not in completed.stdout:
    raise RuntimeError("LOCAL-005 host-free runner contract returned success without its PASS marker")

print("PASS LOCAL-005 native runner PowerShell contract executed")
