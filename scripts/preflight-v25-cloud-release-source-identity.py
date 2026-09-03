#!/usr/bin/env python3
"""Fail closed when the cloud V25 release can package uncommitted or stale source.

The published prerelease/tag names an immutable commit. Release preparation must
therefore not rewrite ProductVersion inputs only in the runner worktree and then
publish against the unchanged commit SHA. Drift admission must also be pathname-
safe: release relevance is decided by Git pathspec matching, never by parsing
line-oriented/quoted `git diff --name-only` output.
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

    # A pathname such as `src/evil\nname.cs` is legal in Git. Line-oriented
    # --name-only output can split or quote it; Trim()/prefix matching can then
    # misclassify a release-relevant commit as harmless. Admission must ask Git
    # directly whether owned pathspecs changed and branch on git-diff's exit code.
    line_parsed_diff = re.compile(
        r"(?im)^(?!\s*#)[^\r\n]*&\s*git\s+diff\s+--name-only\b"
    )
    if line_parsed_diff.search(source):
        failures.append(
            "release-relevant main drift still parses line-oriented `git diff "
            "--name-only`; hostile valid pathnames can bypass release admission"
        )

    pathspec_tokens = (
        "$releaseRelevantPathspecs",
        "'src/'",
        "'tests/'",
        "'scripts/'",
        "'.gitmodules'",
        "'Directory.Build.props'",
        "'QS3D.sln'",
        "'QS3D.V26.sln'",
        "'.github/workflows/release-v25-cloud.yml'",
        "'.github/workflows/dispatch-v25-cloud-after-main-integration.yml'",
    )
    missing_pathspecs = [token for token in pathspec_tokens if token not in source]
    if missing_pathspecs:
        failures.append(
            "pathname-safe release drift admission is missing owned Git pathspecs: "
            + ", ".join(missing_pathspecs)
        )

    quiet_diff_pattern = re.compile(
        r"(?is)&\s*git\s+diff\s+--quiet\s+--no-ext-diff\s+\$range\s+--\s+@releaseRelevantPathspecs"
    )
    if not quiet_diff_pattern.search(source):
        failures.append(
            "release drift admission must use Git's pathname-safe --quiet pathspec "
            "comparison instead of decoding diff path output in PowerShell"
        )

    exit_code_pattern = re.compile(
        r"(?is)\$diffExit\s*=\s*\$LASTEXITCODE.*?"
        r"\$diffExit\s+-eq\s+0.*?return\s+\$false.*?"
        r"\$diffExit\s+-eq\s+1.*?return\s+\$true.*?throw"
    )
    if not exit_code_pattern.search(source):
        failures.append(
            "release drift admission does not distinguish git diff clean/drift/error "
            "exit codes fail-closed"
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print(
        "PASS: V25 cloud preview release is bound to clean committed source identity "
        "with pathname-safe main-drift admission"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
