#!/usr/bin/env python3
"""Fail closed when cloud V25 release/dispatch can package stale source.

Manual release preparation may derive preview identity from the requested tag only
inside the checked-out workspace. That intentional dirty state must remain bounded
to the V25/V26/Core project identity files while release provenance remains bound
to the unchanged admitted protected-main commit SHA. Automatic dispatch, batch
counting, dependency identity, and pathname handling remain independently guarded.
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
    return bool(re.search(r"(?im)^(?!\s*#)[^\r\n]*git\s+diff\s+--name-only\b", source))


def _run_git(repo: pathlib.Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args], cwd=repo, check=check, stdout=subprocess.PIPE,
        stderr=subprocess.PIPE, text=True, encoding="utf-8", errors="strict",
    )


def _assert_pathname_safe_git_primitive(failures: list[str]) -> None:
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
            if any(line.strip().replace("\\", "/").startswith("src/") for line in legacy_lines):
                failures.append("quoted-path fixture did not reproduce legacy line-parser bypass")
            nul_paths = _run_git(repo, "diff", "--name-only", "-z", commit_range, "--").stdout.split("\0")
            if "src/café.cs" not in nul_paths:
                failures.append("NUL-delimited Git pathname fixture lost Unicode release path")
            safe = _run_git(repo, "diff", "--quiet", "--no-ext-diff", commit_range, "--", "src/", check=False)
            if safe.returncode != 1:
                failures.append(f"pathname-safe git diff must report release drift; got {safe.returncode}")
    except (OSError, subprocess.SubprocessError, UnicodeError) as exc:
        failures.append(f"could not execute pathname-safe Git fixture: {type(exc).__name__}: {exc}")


def _assert_batch_gate_preserves_exact_paths(failures: list[str]) -> None:
    try:
        spec = importlib.util.spec_from_file_location("qs3d_v25_release_batch_gate", BATCH_GATE)
        if spec is None or spec.loader is None:
            failures.append("could not load v25-release-batch-gate.py")
            return
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        if module.is_release_relevant(r"src\not-release.txt"):
            failures.append("batch gate rewrote literal backslash into synthetic src/ path")
        if module.is_release_relevant(r"external\QS3D-Platform"):
            failures.append("batch gate rewrote literal backslash into Platform gitlink path")
        if not module.is_release_relevant("src/actual-release.cs"):
            failures.append("batch gate stopped recognizing canonical src/ paths")
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
                failures.append("batch gate trimmed leading-space pathname")
            if "src/not-release.txt" in decoded:
                failures.append("batch gate synthesized release path from leading-space pathname")
            relevant = sorted(path for path in decoded if module.is_release_relevant(path))
            if relevant != ["src/café.cs"]:
                failures.append("batch gate classified hostile pathnames incorrectly: " + repr(relevant))
    except (OSError, subprocess.SubprocessError, UnicodeError, RuntimeError) as exc:
        failures.append(f"could not execute batch-gate pathname fixture: {type(exc).__name__}: {exc}")


def main() -> int:
    source = PREPARE.read_text(encoding="utf-8")
    dispatch = DISPATCH.read_text(encoding="utf-8")
    batch_gate = BATCH_GATE.read_text(encoding="utf-8")
    release_policy = RELEASE_POLICY.read_text(encoding="utf-8")
    failures: list[str] = []

    workspace_signals = (
        "$workspaceVersionPaths = @(",
        "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
        "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
        "src/QS3D.Core/QS3D.Core.csproj",
        "function Set-WorkspaceProductVersion",
        "Set-WorkspaceProductVersion -ReleaseTagValue $tag",
        "$expectedProductVersion = $tag.Substring(1)",
        "Release workspace HEAD must remain the protected-main source commit",
        "$finalStatus.Count -ne 0 -and $finalStatus.Count -ne $workspaceVersionPaths.Count",
        "Workspace version synchronization must either be a no-op or produce exactly three bounded project modifications.",
        "Workspace ProductVersion is already synchronized",
        "if ($finalStatus.Count -eq $workspaceVersionPaths.Count)",
        "Unexpected release-preparation workspace change",
        "No commit, push, branch-protection bypass, or protected-main mutation was performed by release preparation.",
    )
    missing_workspace = [token for token in workspace_signals if token not in source]
    if missing_workspace:
        failures.append("manual release lost bounded workspace/source identity signals: " + ", ".join(missing_workspace))

    for stale in (
        "function Get-CommittedProductVersion",
        "$committedProductVersion = Get-CommittedProductVersion",
        "Merge the version update to protected main before publishing.",
    ):
        if stale in source:
            failures.append("manual release reintroduced stale committed-version gate: " + stale)

    if "sync-preview-release-version.ps1" in source:
        failures.append("manual release reintroduced legacy external workspace rewrite helper")

    if _line_parsed_diff(source):
        failures.append("release preparation still parses line-oriented git diff --name-only")
    if _line_parsed_diff(dispatch):
        failures.append("main release dispatcher still parses line-oriented git diff --name-only")

    prepare_pathspec_tokens = (
        "$releaseRelevantPathspecs", "'src/'", "'tests/'", "'scripts/'",
        "'external/QS3D-Platform'", "'.gitmodules'", "'Directory.Build.props'",
        "'QS3D.sln'", "'QS3D.V26.sln'", "'.github/workflows/release-v25-cloud.yml'",
        "'.github/workflows/dispatch-v25-cloud-after-main-integration.yml'",
    )
    missing_pathspecs = [token for token in prepare_pathspec_tokens if token not in source]
    if missing_pathspecs:
        failures.append("release drift admission is missing Git pathspecs: " + ", ".join(missing_pathspecs))

    quiet_diff_pattern = re.compile(r"(?is)&\s*git\s+diff\s+--quiet\s+--no-ext-diff\s+\$range\s+--\s+@releaseRelevantPathspecs")
    if not quiet_diff_pattern.search(source):
        failures.append("release preparation must use pathname-safe --quiet pathspec comparison")
    exit_code_pattern = re.compile(r"(?is)\$diffExit\s*=\s*\$LASTEXITCODE.*?\$diffExit\s+-eq\s+0.*?return\s+\$false.*?\$diffExit\s+-eq\s+1.*?return\s+\$true.*?throw")
    if not exit_code_pattern.search(source):
        failures.append("release preparation does not fail-close git diff clean/drift/error exit codes")

    dispatch_signals = (
        '- "external/QS3D-Platform"', '- ".gitmodules"', "release_relevant_pathspecs=(",
        "'external/QS3D-Platform'", "'.gitmodules'",
        'git diff --quiet --no-ext-diff "${source_sha}..${current_main}" -- "${release_relevant_pathspecs[@]}"',
        "release_drift_status=$?", "release_drift_status == 1", "release_drift_status != 0",
    )
    missing_dispatch = [token for token in dispatch_signals if token not in dispatch]
    if missing_dispatch:
        failures.append("automatic dispatcher is missing pathname-safe dependency drift signals: " + ", ".join(missing_dispatch))

    batch_signals = (
        '"external/QS3D-Platform"', '".gitmodules"', "RELEASE_RELEVANT_EXACT_PATHS",
        '"--name-only", "-z"', '.split("\\0")', "run_git_exact",
    )
    missing_batch = [token for token in batch_signals if token not in batch_gate]
    if missing_batch:
        failures.append("release batch gate does not preserve exact paths; missing: " + ", ".join(missing_batch))

    for policy_token in ("`external/QS3D-Platform`", "`.gitmodules`"):
        if policy_token not in release_policy:
            failures.append("release policy missing dependency identity path: " + policy_token)

    _assert_pathname_safe_git_primitive(failures)
    _assert_batch_gate_preserves_exact_paths(failures)

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V25 manual release accepts an already-synchronized identity or permits only bounded workspace preview identity changes while preserving exact protected-main source provenance and pathname-safe drift admission")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
