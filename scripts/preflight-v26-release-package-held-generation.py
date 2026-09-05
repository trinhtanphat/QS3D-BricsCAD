#!/usr/bin/env python3
"""Require V26 assembly identity semantics to consume admitted held file generations."""

from __future__ import annotations

import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "assert-v26-release-package-identity.ps1"
PROBE_DIR = ROOT / "scripts" / "V26ReleaseIdentityProbe"
PROBE_PROJECT = PROBE_DIR / "V26ReleaseIdentityProbe.csproj"
PROBE_SOURCE = PROBE_DIR / "Program.cs"
MARKER = "QS3D_ASSEMBLY_VERSION:"
EXPECTED_PROBE_VERSION = "1.0.0.0"


def static_failures() -> list[str]:
    source = SCRIPT.read_text(encoding="utf-8")
    probe = PROBE_SOURCE.read_text(encoding="utf-8")
    failures: list[str] = []

    required_existing = (
        "function Open-LockedStableFile",
        "function Get-HeldStreamingSha256",
        "[IO.FileShare]::Read",
        "$pluginHeld = Open-LockedStableFile",
        "$coreHeld = Open-LockedStableFile",
        "PluginSha256 = $pluginHeld.Sha256",
        "CoreSha256 = $coreHeld.Sha256",
    )
    for token in required_existing:
        if token not in source:
            failures.append(f"held-generation admission regressed; missing: {token}")

    forbidden = (
        "GetAssemblyName($pluginHeld.Path)",
        "GetAssemblyName($coreHeld.Path)",
        "GetAssemblyName($Held.Path)",
        "ReflectionOnlyLoad(",
        "$snapshotPath",
        "$Held.Stream.CopyTo($process.StandardInput.BaseStream)",
    )
    for token in forbidden:
        if token in source:
            failures.append(f"assembly semantics reintroduced an unsafe/unbounded generation path: {token}")

    required_source = (
        "function Initialize-AssemblyVersionProbe",
        "function Get-HeldAssemblyVersion",
        "$Held.Stream.Position = 0",
        "$deadline = [Diagnostics.Stopwatch]::StartNew()",
        "$copyTask = $Held.Stream.CopyToAsync($process.StandardInput.BaseStream)",
        "$copyTask.Wait(",
        "$copyTask.GetAwaiter().GetResult()",
        "$process.StandardInput.Close()",
        "$process.WaitForExit(",
        "RedirectStandardInput = $true",
        "RedirectStandardOutput = $true",
        "RedirectStandardError = $true",
        "$pluginVersion = Get-HeldAssemblyVersion -Held $pluginHeld",
        "$coreVersion = Get-HeldAssemblyVersion -Held $coreHeld",
    )
    for token in required_source:
        if token not in source:
            failures.append(f"held-stream probe contract is incomplete; missing: {token}")

    required_probe = (
        "Console.OpenStandardInput()",
        "MaxAssemblyBytes = 256L * 1024L * 1024L",
        "new PEReader(",
        "PEStreamOptions.LeaveOpen",
        "peReader.HasMetadata",
        "peReader.GetMetadataReader()",
        "metadata.IsAssembly",
        "metadata.GetAssemblyDefinition().Version",
        "QS3D_ASSEMBLY_VERSION:",
    )
    for token in required_probe:
        if token not in probe:
            failures.append(f"metadata-only probe contract is incomplete; missing: {token}")

    if "Assembly.Load" in probe or "AssemblyName.GetAssemblyName" in probe:
        failures.append("metadata probe must parse PE metadata without loading or pathname-opening the candidate assembly")

    dispose_marker = source.find("$heldFiles[$index].Stream.Dispose()")
    version_match = source.find("if ($pluginVersion -ne $packageVersion -or $coreVersion -ne $packageVersion)")
    if dispose_marker < 0 or version_match < 0 or dispose_marker < version_match:
        failures.append("held input streams must remain live through cross-assembly version equality checks")

    if "continue-on-error" in source.lower():
        failures.append("release package identity must not hide held-generation failures")

    return failures


def github_annotation_escape(value: str) -> str:
    return value.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def phase_notice(phase: str) -> None:
    print(f"::notice title=V26 held-generation phase::{github_annotation_escape(phase)}")


def run_probe_regression() -> list[str]:
    failures: list[str] = []
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        return ["dotnet SDK/runtime is required to exercise the V26 metadata probe"]
    phase_notice("dotnet-resolved")

    env = os.environ.copy()
    env.update(
        {
            "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
            "DOTNET_NOLOGO": "1",
            "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
        }
    )

    build = subprocess.run(
        [
            dotnet,
            "build",
            str(PROBE_PROJECT),
            "--configuration",
            "Release",
            "--nologo",
            "--verbosity",
            "quiet",
            "-p:RestoreIgnoreFailedSources=true",
        ],
        cwd=ROOT,
        env=env,
        capture_output=True,
        text=True,
        timeout=90,
        check=False,
    )
    if build.returncode != 0:
        detail = (build.stderr or build.stdout).strip()[-2000:]
        return [f"V26 metadata probe build failed: {detail}"]
    phase_notice("probe-build-pass")

    probe_dll = PROBE_DIR / "bin" / "Release" / "net8.0" / "V26ReleaseIdentityProbe.dll"
    if not probe_dll.is_file():
        return [f"V26 metadata probe build produced no expected assembly: {probe_dll}"]

    probe_bytes = probe_dll.read_bytes()
    good = subprocess.run(
        [dotnet, str(probe_dll)],
        input=probe_bytes,
        capture_output=True,
        timeout=20,
        check=False,
    )
    expected = f"{MARKER}{EXPECTED_PROBE_VERSION}"
    good_stdout = good.stdout.decode("utf-8", errors="replace").strip()
    if good.returncode != 0 or good_stdout != expected:
        detail = good.stderr.decode("utf-8", errors="replace").strip()[-1000:]
        failures.append(
            f"V26 metadata probe did not parse its own exact stdin generation: rc={good.returncode} stdout={good_stdout!r} stderr={detail!r}"
        )
    else:
        phase_notice("self-parse-pass")

    malformed = subprocess.run(
        [dotnet, str(probe_dll)],
        input=b"not-a-managed-pe",
        capture_output=True,
        timeout=20,
        check=False,
    )
    malformed_stdout = malformed.stdout.decode("utf-8", errors="replace")
    if malformed.returncode == 0 or MARKER in malformed_stdout:
        failures.append("V26 metadata probe did not fail closed on malformed stdin bytes")
    else:
        phase_notice("malformed-rejection-pass")

    return failures


def main() -> int:
    phase_notice("guard-entered")
    failures = static_failures()
    if not failures:
        phase_notice("static-contract-pass")
        failures.extend(run_probe_regression())

    if failures:
        for failure in failures:
            print(f"::error title=V26 held-generation preflight::{github_annotation_escape(failure)}")
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    phase_notice("guard-pass")
    print("PASS: V26 package semantics use a bounded metadata-only probe over exact held-stream bytes")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
