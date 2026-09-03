#!/usr/bin/env python3
"""Fail closed when cloud V25 release/dispatch can package stale source.

The published prerelease/tag names an immutable commit. Release preparation must
therefore not rewrite ProductVersion inputs only in the runner worktree and then
publish against the unchanged commit SHA. Release preparation and its automatic
main dispatcher must classify drift with Git pathspec/exit-status semantics,
never by decoding line-oriented path output. The pinned Platform gitlink is part
of release identity because the cloud workflow materializes and builds it.
"""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"


def _line_parsed_diff(source: str) -> bool:
    return bool(
        re.search(
            r"(?im)^(?!\s*#)[^\r\n]*git\s+diff\s+--name-only\b",
            source,
        )
    )


def main() -> int:
    source = PREPARE.read_text(encoding="utf-8")
    dispatch = DISPATCH.read_text(encoding="utf-8")
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
    if _line_parsed_diff(source):
        failures.append(
            "release preparation still parses line-oriented git diff --name-only; "
            "hostile valid pathnames can bypass release admission"
        )
    if _line_parsed_diff(dispatch):
        failures.append(
            "main release dispatcher still parses line-oriented git diff --name-only; "
            "hostile valid pathnames can bypass supersession admission"
        )

    prepare_pathspec_tokens = (
        "$releaseRelevantPathspecs",
        "'src/'",
        "'tests/'",
        "'scripts/'",
        "'external/QS3D-Platform'",
        "'.gitmodules'",
        "'Directory.Build.props'",
        "'QS3D.sln'",
        "'QS3D.V26.sln'",
        "'.github/workflows/release-v25-cloud.yml'",
        "'.github/workflows/dispatch-v25-cloud-after-main-integration.yml'",
    )
    missing_pathspecs = [token for token in prepare_pathspec_tokens if token not in source]
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
            "release preparation must use Git's pathname-safe --quiet pathspec "
            "comparison instead of decoding diff path output in PowerShell"
        )

    exit_code_pattern = re.compile(
        r"(?is)\$diffExit\s*=\s*\$LASTEXITCODE.*?"
        r"\$diffExit\s+-eq\s+0.*?return\s+\$false.*?"
        r"\$diffExit\s+-eq\s+1.*?return\s+\$true.*?throw"
    )
    if not exit_code_pattern.search(source):
        failures.append(
            "release preparation does not distinguish git diff clean/drift/error "
            "exit codes fail-closed"
        )

    dispatch_signals = (
        '- "external/QS3D-Platform"',
        "release_relevant_pathspecs=(",
        "'external/QS3D-Platform'",
        'git diff --quiet --no-ext-diff "${source_sha}..${current_main}" -- "${release_relevant_pathspecs[@]}"',
        "release_drift_status=$?",
        "release_drift_status == 1",
        "release_drift_status != 0",
    )
    missing_dispatch = [token for token in dispatch_signals if token not in dispatch]
    if missing_dispatch:
        failures.append(
            "automatic release dispatcher is missing pathname-safe/Platform drift "
            "admission signals: " + ", ".join(missing_dispatch)
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print(
        "PASS: V25 cloud preview release/dispatcher are bound to clean committed "
        "source identity with pathname-safe and Platform-gitlink drift admission"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
