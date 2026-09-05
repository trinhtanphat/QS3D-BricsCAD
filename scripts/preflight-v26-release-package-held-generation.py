#!/usr/bin/env python3
"""Require V26 assembly identity semantics to stay bound to admitted held generations."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "assert-v26-release-package-identity.ps1"


def main() -> int:
    source = SCRIPT.read_text(encoding="utf-8")
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

    forbidden_path_reopens = (
        "GetAssemblyName($pluginHeld.Path)",
        "GetAssemblyName($coreHeld.Path)",
    )
    for token in forbidden_path_reopens:
        if token in source:
            failures.append(
                f"managed assembly semantics still reopen admitted input by pathname: {token}"
            )

    snapshot_helper = source.find("function Get-HeldAssemblyVersion")
    held_stream_copy = source.find("$Held.Stream.Read(")
    create_new = source.find("[IO.FileMode]::CreateNew")
    read_share = source.find("[IO.FileShare]::Read", create_new if create_new >= 0 else 0)
    assembly_semantics = source.find("[Reflection.AssemblyName]::GetAssemblyName($snapshotPath)")
    dispose_snapshot = source.find("$snapshotStream.Dispose()")
    delete_snapshot = source.find("Remove-Item -LiteralPath $snapshotPath")

    if min(snapshot_helper, held_stream_copy, create_new, read_share, assembly_semantics) < 0:
        failures.append(
            "held assembly semantic snapshot contract is incomplete: expected stream-copy, exclusive generation creation, read lock, and snapshot-only GetAssemblyName"
        )
    elif not (snapshot_helper < create_new < held_stream_copy < assembly_semantics):
        failures.append(
            "assembly semantics must consume a snapshot copied from the held input stream, not a pathname generation"
        )

    if assembly_semantics >= 0 and dispose_snapshot >= 0 and dispose_snapshot < assembly_semantics:
        failures.append("semantic snapshot lock must remain held through GetAssemblyName")

    if delete_snapshot >= 0 and dispose_snapshot >= 0 and delete_snapshot < dispose_snapshot:
        failures.append("semantic snapshot must not be deleted before its lock is disposed")

    if "continue-on-error" in source.lower():
        failures.append("release package identity must not hide held-generation failures")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V26 package assembly semantics consume a locked snapshot copied from each admitted held generation")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
