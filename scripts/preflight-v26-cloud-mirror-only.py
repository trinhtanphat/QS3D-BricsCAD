#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"


def fail(message: str) -> None:
    print(f"ERROR: V26 cloud mirror-only preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


if not WORKFLOW.is_file():
    fail(f"missing required workflow: {WORKFLOW.relative_to(ROOT)}")

workflow = WORKFLOW.read_text(encoding="utf-8")

# The cloud lane intentionally uses only the helper-owned pinned .20 mirror when
# the Actions cache is empty. Do not reintroduce canonical Google Storage or the
# owner bootstrap fallback here; admission security remains inside the helper.
for forbidden in (
    "BRICSCAD_V26_PUBLIC_MSI_URL",
    "BRICSCAD_V26_BOOTSTRAP_MSI_URL",
    "-FallbackUrl",
):
    if forbidden in workflow:
        fail(f"release-v26-cloud.yml must not contain mirror-only forbidden token: {forbidden}")

if workflow.count("-PrimaryUrl ''") != 2:
    fail("both V26 acquisition call sites must explicitly disable PrimaryUrl")
if workflow.count("-UsePinnedHttpMirror") != 2:
    fail("both V26 acquisition call sites must opt into the helper-owned pinned mirror")
if "http://" in workflow:
    fail("release-v26-cloud.yml must not embed the plaintext .20 URL; the helper owns it")

# The prime-cache call may be slow. Keep a separate child PowerShell monitor on
# the same Windows runner so GitHub Actions receives live staging-byte telemetry
# while the hardened helper remains the sole downloader/admission authority.
required_progress_tokens = (
    "$downloadStartedUtc = [DateTime]::UtcNow",
    "$monitorScriptPath = Join-Path $env:RUNNER_TEMP 'qs3d-v26-download-monitor.ps1'",
    "Start-Process -FilePath powershell.exe",
    "-NoNewWindow",
    ".qs3d-v26-msi-*.tmp",
    "[V26 installer progress]",
    "elapsed=",
    "bytes=",
    "Start-Sleep -Seconds 30",
    "Stop-Process -Id $monitor.Id -Force",
    "[V26 installer complete]",
)
for token in required_progress_tokens:
    if token not in workflow:
        fail(f"release-v26-cloud.yml is missing live installer telemetry token: {token}")

if workflow.count("Start-Process -FilePath powershell.exe") != 1:
    fail("installer download monitor must have exactly one process launch")
if workflow.count("Stop-Process -Id $monitor.Id -Force") != 1:
    fail("installer download monitor must have exactly one deterministic stop")

print("V26 cloud mirror-only/progress preflight passed.")
