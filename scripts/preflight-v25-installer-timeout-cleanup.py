#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "install-bricscad-v25.ps1"


def fail(message: str) -> None:
    print(f"::error::{message}")
    raise SystemExit(1)


try:
    text = TARGET.read_text(encoding="utf-8")
except (OSError, UnicodeError) as exc:
    fail(f"cannot read installer helper as strict UTF-8: {exc}")

required = (
    "[ValidateRange(60, 7200)][int]$InstallTimeoutSeconds = 1800",
    "[ValidateRange(1, 60)][int]$CleanupTimeoutSeconds = 10",
    "function Stop-OwnedInstallerTree",
    "if ($InstallerProcess.HasExited)",
    "Start-Process -FilePath \"taskkill.exe\"",
    "'/PID'",
    "$InstallerProcess.Id.ToString([Globalization.CultureInfo]::InvariantCulture)",
    "'/T'",
    "'/F'",
    "$cleanup.WaitForExit($TimeoutSeconds * 1000)",
    "$InstallerProcess.WaitForExit([Math]::Min($TimeoutSeconds, 5) * 1000)",
    "Start-Process -FilePath \"msiexec.exe\"",
    "$process.WaitForExit($InstallTimeoutSeconds * 1000)",
    "Stop-OwnedInstallerTree -InstallerProcess $process -TimeoutSeconds $CleanupTimeoutSeconds",
    "$process.ExitCode -ne 0 -and $process.ExitCode -ne 3010",
    "$process.Dispose()",
)
for token in required:
    if token not in text:
        fail(f"installer timeout/cleanup contract is missing: {token}")

for forbidden in (
    "Start-Process -FilePath \"msiexec.exe\" -ArgumentList ([string]::Join(' ', $arguments)) -Wait",
    "taskkill /IM msiexec",
    "taskkill.exe /IM",
    "Stop-Process -Name msiexec",
    "Get-Process msiexec | Stop-Process",
):
    if forbidden.lower() in text.lower():
        fail(f"installer helper must not use unbounded wait or broad msiexec termination: {forbidden}")

launch = text.find('Start-Process -FilePath "msiexec.exe"')
wait = text.find('$process.WaitForExit($InstallTimeoutSeconds * 1000)', launch)
cleanup = text.find('Stop-OwnedInstallerTree -InstallerProcess $process -TimeoutSeconds $CleanupTimeoutSeconds', wait)
exit_check = text.find('$process.ExitCode -ne 0 -and $process.ExitCode -ne 3010', cleanup)
dispose = text.find('$process.Dispose()', exit_check)
if min(launch, wait, cleanup, exit_check, dispose) < 0 or not (launch < wait < cleanup < exit_check < dispose):
    fail("installer helper must launch, wait with a bound, clean the owned tree on timeout, validate exit code, then dispose")

cleanup_start = text.find("function Stop-OwnedInstallerTree")
cleanup_end = text.find("$MsiPath = [IO.Path]::GetFullPath", cleanup_start)
if cleanup_start < 0 or cleanup_end < 0:
    fail("cannot locate bounded cleanup helper")
cleanup_body = text[cleanup_start:cleanup_end]
if not re.search(r"taskkill\.exe[\s\S]*'/PID'[\s\S]*\$InstallerProcess\.Id[\s\S]*'/T'[\s\S]*'/F'", cleanup_body):
    fail("cleanup must target only the launched installer root PID and its descendants")
if "Timed out while cleaning up BricsCAD installer process tree rooted at PID" not in cleanup_body:
    fail("cleanup timeout must produce an explicit rooted-PID diagnostic")
if "Failed to clean up BricsCAD installer process tree rooted at PID" not in cleanup_body:
    fail("cleanup command failure must produce an explicit rooted-PID diagnostic")
if "remained active after bounded tree cleanup" not in cleanup_body:
    fail("cleanup must fail closed when the installer root survives")

print("PASS V25 installer helper has bounded execution and owned-process-tree cleanup")
