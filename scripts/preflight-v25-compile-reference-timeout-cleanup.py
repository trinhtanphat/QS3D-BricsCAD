#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "acquire-v25-compile-references.ps1"
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def fail(message):
    print("ERROR:", message)
    return 1


def require(text, needle, label):
    if needle not in text:
        raise AssertionError(label + ": missing " + repr(needle))


def main():
    try:
        source = SCRIPT.read_text(encoding="utf-8")
        workflow = WORKFLOW.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        return fail("cannot read V25 compile-reference acquisition sources: " + str(exc))

    try:
        require(source, "function Stop-OwnedProcessTree", "owned cleanup helper")
        require(source, "taskkill.exe", "Windows tree cleanup command")
        require(source, "'/PID'", "cleanup must target one owned root PID")
        require(source, "'/T'", "cleanup must include descendants")
        require(source, "'/F'", "cleanup must force bounded termination")
        require(source, "$taskkill.WaitForExit($CleanupTimeoutMs)", "cleanup command timeout")
        require(source, "$taskkill.Kill()", "stuck cleanup command containment")
        require(source, "$Process.WaitForExit($CleanupTimeoutMs)", "root-process post-cleanup wait")
        require(source, "Stop-OwnedProcessTree -Process $process -CleanupTimeoutMs 10000", "timeout cleanup invocation")
        require(source, "owned process-tree cleanup failed", "fail-closed cleanup diagnostic")
        require(source, "owned process tree terminated", "successful timeout-cleanup diagnostic")
        require(source, "$process.WaitForExit(900000)", "15-minute MSI extraction budget")
        require(source, "$process.ExitCode -notin @(0, 3010)", "existing MSI success-code contract")
        require(workflow, ".\\scripts\\acquire-v25-compile-references.ps1", "shared core acquisition wiring")
        require(workflow, "timeout-minutes: 30", "outer workflow timeout containment")
    except AssertionError as exc:
        return fail(str(exc))

    if re.search(r"Get-Process\s+[^\r\n]*msiexec", source, flags=re.IGNORECASE):
        return fail("cleanup must not enumerate/global-kill unrelated msiexec processes")
    if re.search(r"Stop-Process\s+[^\r\n]*-Name\s+['\"]?msiexec", source, flags=re.IGNORECASE):
        return fail("cleanup must not kill msiexec globally by process name")
    if "try { $process.Kill() } catch { }" in source:
        return fail("legacy direct-root-only timeout kill must not return")

    helper_match = re.search(
        r"function\s+Stop-OwnedProcessTree\s*\{(?P<body>.*?)\n\}",
        source,
        flags=re.IGNORECASE | re.DOTALL,
    )
    if not helper_match:
        return fail("cannot isolate Stop-OwnedProcessTree helper")
    helper = helper_match.group("body")
    if "if ($Process.HasExited)" in helper and re.search(
        r"if\s*\(\$Process\.HasExited\)\s*\{\s*return\s*\}", helper, flags=re.IGNORECASE | re.DOTALL
    ):
        return fail("timeout cleanup must not silently skip a raced root exit because descendants become unverifiable")

    timeout_match = re.search(
        r"\$exited\s*=\s*\$process\.WaitForExit\(900000\).*?if\s*\(-not\s+\$exited\)\s*\{(?P<body>.*?)\n\s*\}",
        source,
        flags=re.IGNORECASE | re.DOTALL,
    )
    if not timeout_match:
        return fail("cannot isolate MSI timeout branch")
    timeout_body = timeout_match.group("body")
    cleanup_index = timeout_body.find("Stop-OwnedProcessTree")
    terminal_throw_index = timeout_body.rfind("throw ")
    if cleanup_index < 0 or terminal_throw_index < cleanup_index:
        return fail("timeout branch must attempt owned-tree cleanup before terminal failure")

    print("PASS: V25 compile-reference MSI timeout cleanup is bounded, PID-scoped, descendant-aware, and fail-closed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
