#!/usr/bin/env python3
"""Fail closed when the cloud V25 release can package uncommitted version bytes.

The published prerelease/tag names an immutable commit. Release preparation must
therefore not rewrite ProductVersion inputs only in the runner worktree and then
publish against the unchanged commit SHA.
"""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"


def main() -> int:
    source = PREPARE.read_text(encoding="utf-8")
    failures: list[str] = []

    workspace_sync = re.compile(
        r"(?im)^\s*&\s*\(Join-Path\s+\$PSScriptRoot\s+['\"]sync-preview-release-version\.ps1['\"]\)"
    )
    if workspace_sync.search(source):
        failures.append(
            "prepare-v25-cloud-release.ps1 still executes workspace-only preview "
            "version synchronization before returning an unchanged commit SHA"
        )

    committed_binding_signals = (
        "Committed preview ProductVersion",
        "$expectedReleaseTag",
        "$committedProductVersion",
    )
    missing = [token for token in committed_binding_signals if token not in source]
    if missing:
        failures.append(
            "release preparation does not prove the requested tag matches the "
            "ProductVersion committed at the selected release base; missing: "
            + ", ".join(missing)
        )

    final_clean_pattern = re.compile(
        r"(?is)\$finalStatus\s*=\s*@\(Get-ReleaseStatusEntries\).*?"
        r"foreach\s*\(\$entry\s+in\s+\$finalStatus\).*?throw"
    )
    if not final_clean_pattern.search(source):
        failures.append(
            "release preparation does not re-assert a clean index/worktree after "
            "committed version binding and before returning the release SHA"
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V25 cloud preview release is bound to clean committed source identity")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
