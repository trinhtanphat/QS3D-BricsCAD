#!/usr/bin/env python3
"""Fail closed when cloud V25 release/dispatch can package stale source.

The published prerelease/tag names an immutable commit. Release preparation must
therefore not rewrite ProductVersion inputs only in the runner worktree and then
publish against the unchanged commit SHA. Release preparation, automatic main
dispatch, batch counting, and policy documentation must classify the pinned
Platform dependency identity consistently. Batch path enumeration must preserve
exact NUL-delimited pathnames instead of trimming, separator rewriting, or
decoding quoted line output.
"""

from __future__ import annotations

import importlib.util
import os
import pathlib
import re
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
BATCH_GATE = ROOT / "scripts" / "v25-release-batch-gate.py"
RELEASE_POLICY = ROOT / "docs" / "RELEASE_POLICY.md"


def _line_parsed_diff(source: str) -> bool:
    return bool(
        re.search(
            r"(?im)^(?!\s*#)[^\r\n]*git\s+diff\s+--name-only\b",
            source,
        )
    )


def _run_git(repo: pathlib.Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=repo,
        check=check,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="strict",
    )


def _assert_pathname_safe_git_primitive(failures: list[str]) -> None:
    """Prove quoted line output bypasses prefix parsing while NUL/pathspec output does not."""

    try:
        with tempfile.TemporaryDirectory(prefix="qs3d-release-path-") as temp:
            repo = pathlib.Path(temp)
            _run_git(repo, "init", "-q")
            _run_git(repo, "config", "user.name", "QS3D C05 preflight")
            _run_git(repo, "config", "user.email", "c05-preflight@example.invalid")
            _run_git(repo, "config", "core.quotePath", "true")

            (repo / "README.md").write_text("base\n", encoding="utf-8")
            _run_git(repo, "add", "--", "README.md")
            _run_git(repo, "commit", "-q", "-m", "base")
            base = _run_git(repo, "rev-parse", "HEAD").stdout.strip()

            source_dir = repo / "src"
            source_dir.mkdir()
            (source_dir / "café.cs").write_text("// drift\n", encoding="utf-8")
            _run_git(repo, "add", "--", "src/")
            _run_git(repo, "commit", "-q", "-m", "release-relevant unicode path")
            head = _run_git(repo, "rev-parse", "HEAD").stdout.strip()
            commit_range = f"{base}..{head}"

            legacy_lines = _run_git(repo, "diff", "--name-only", commit_range, "--").stdout.splitlines()
            legacy_prefix_match = any(
                line.strip().replace("\\", "/").startswith("src/") for line in legacy_lines
            )
            if legacy_prefix_match:
                failures.append(
                    "quoted-path regression fixture did not reproduce the legacy prefix "
                    "classification bypass with core.quotePath=true"
                )

            nul_paths = _run_git(repo, "diff", "--name-only", "-z", commit_range, "--").stdout.split("\0")
            if "src/café.cs" not in nul_paths:
                failures.append(
                    "NUL-delimited Git pathname fixture did not preserve the release-relevant Unicode path"
                )

            safe = _run_git(
                repo,
                "diff",
                "--quiet",
                "--no-ext-diff",
                commit_range,
                "--",
                "src/",
                check=False,
            )
            if safe.returncode != 1:
                failures.append(
                    "pathname-safe git diff pathspec fixture must return exit 1 for "
                    f"release-relevant Unicode drift; got {safe.returncode}"
                )
    except (OSError, subprocess.SubprocessError, UnicodeError) as exc:
        failures.append(
            "could not execute deterministic pathname-safe Git regression fixture: "
            f"{type(exc).__name__}: {exc}"
        )


def _assert_batch_gate_preserves_exact_paths(failures: list[str]) -> None:
    """Execute the real batch path decoder against hostile-but-valid pathnames."""

    try:
        spec = importlib.util.spec_from_file_location("qs3d_v25_release_batch_gate", BATCH_GATE)
        if spec is None or spec.loader is None:
            failures.append("could not load v25-release-batch-gate.py for deterministic pathname regression")
            return
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)

        # Git emits repository-relative path separators as '/', including on
        # Windows. A literal backslash can be part of a Unix filename and must
        # not be rewritten into a synthetic directory separator.
        if module.is_release_relevant(r"src\not-release.txt"):
            failures.append(
                "batch gate rewrote a literal backslash filename into a synthetic release-relevant src/ path"
            )
        if module.is_release_relevant(r"external\QS3D-Platform"):
            failures.append(
                "batch gate rewrote a literal backslash filename into the Platform gitlink path"
            )
        if not module.is_release_relevant("src/actual-release.cs"):
            failures.append("batch gate stopped recognizing canonical Git src/ paths")

        with tempfile.TemporaryDirectory(prefix="qs3d-release-batch-path-") as temp:
            repo = pathlib.Path(temp)
            _run_git(repo, "init", "-q")
            _run_git(repo, "config", "user.name", "QS3D C05 preflight")
            _run_git(repo, "config", "user.email", "c05-preflight@example.invalid")
            _run_git(repo, "config", "core.quotePath", "true")
            (repo / "README.md").write_text("base\n", encoding="utf-8")
            _run_git(repo, "add", "--", "README.md")
            _run_git(repo, "commit", "-q", "-m", "base")

            misleading = repo / " src"
            misleading.mkdir()
            (misleading / "not-release.txt").write_text("noise\n", encoding="utf-8")
            source_dir = repo / "src"
            source_dir.mkdir()
            (source_dir / "café.cs").write_text("// relevant\n", encoding="utf-8")
            _run_git(repo, "add", "--all")
            _run_git(repo, "commit", "-q", "-m", "mixed exact pathname fixture")
            head = _run_git(repo, "rev-parse", "HEAD").stdout.strip()

            previous_cwd = pathlib.Path.cwd()
            try:
                os.chdir(repo)
                decoded = module.changed_paths_for_first_parent_commit(head)
            finally:
                os.chdir(previous_cwd)

            if " src/not-release.txt" not in decoded:
                failures.append(
                    "batch gate trimmed the leading-space pathname instead of preserving exact NUL-delimited bytes"
                )
            if "src/not-release.txt" in decoded:
                failures.append(
                    "batch gate converted a non-release leading-space pathname into a release-relevant src/ path"
                )
            relevant = sorted(path for path in decoded if module.is_release_relevant(path))
            if relevant != ["src/café.cs"]:
                failures.append(
                    "batch gate classified hostile exact pathnames incorrectly; expected only src/café.cs, got "
                    + repr(relevant)
                )
    except (OSError, subprocess.SubprocessError, UnicodeError, RuntimeError) as exc:
        failures.append(
            "could not execute batch-gate exact-path regression fixture: "
            f"{type(exc).__name__}: {exc}"
        )


def main() -> int:
    source = PREPARE.read_text(encoding="utf-8")
    dispatch = DISPATCH.read_text(encoding="utf-8")
    batch_gate = BATCH_GATE.read_text(encoding="utf-8")
    release_policy = RELEASE_POLICY.read_text(encoding="utf-8")
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
        '- ".gitmodules"',
        "release_relevant_pathspecs=(",
        "'external/QS3D-Platform'",
        "'.gitmodules'",
        'git diff --quiet --no-ext-diff "${source_sha}..${current_main}" -- "${release_relevant_pathspecs[@]}"',
        "release_drift_status=$?",
        "release_drift_status == 1",
        "release_drift_status != 0",
    )
    missing_dispatch = [token for token in dispatch_signals if token not in dispatch]
    if missing_dispatch:
        failures.append(
            "automatic release dispatcher is missing pathname-safe/dependency drift "
            "admission signals: " + ", ".join(missing_dispatch)
        )

    batch_signals = (
        '"external/QS3D-Platform"',
        '".gitmodules"',
        "RELEASE_RELEVANT_EXACT_PATHS",
        '"--name-only", "-z"',
        '.split("\\0")',
        "run_git_exact",
    )
    missing_batch = [token for token in batch_signals if token not in batch_gate]
    if missing_batch:
        failures.append(
            "release batch gate does not count/decode release-relevant paths safely; missing: "
            + ", ".join(missing_batch)
        )

    for policy_token in ("`external/QS3D-Platform`", "`.gitmodules`"):
        if policy_token not in release_policy:
            failures.append(
                "release policy does not document dependency identity path as release-relevant: "
                + policy_token
            )

    _assert_pathname_safe_git_primitive(failures)
    _assert_batch_gate_preserves_exact_paths(failures)

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print(
        "PASS: V25 cloud preview release/dispatcher/batch policy are bound to clean committed "
        "source identity with exact pathname and dependency-identity admission"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())