#!/usr/bin/env python3
"""Regression test for Shared CI changed-path tokenization and rename safety."""

from __future__ import annotations

import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def classify_control(path: str) -> tuple[bool, bool]:
    normalized = path.replace("\\", "/")
    build = bool(
        re.match(r"^(src|tests|scripts)/", normalized)
        or re.match(r"^samples/generated/", normalized)
        or re.match(r"^\.github/workflows/", normalized)
        or normalized in {".gitmodules", "Directory.Build.props", "QS3D.sln", "QS3D.V26.sln"}
    )
    source = build or normalized in {
        "CI_POLICY.md",
        "AGENTS.md",
        "README.md",
        "docs/MAIN-WRITE-AUTHORIZATION.md",
        "docs/AGENT-WORK-REGISTRATION.md",
        "docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md",
        "docs/AGENT-STATUS-MARKER-SEMANTICS.md",
        "docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md",
    }
    return source, build


def classify_paths(paths: list[str]) -> tuple[bool, bool]:
    source = False
    build = False
    for path in paths:
        path_source, path_build = classify_control(path)
        source = source or path_source
        build = build or path_build
    return source, build


def run_git(repo: Path, *args: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    if completed.returncode != 0:
        fail(
            "git command failed: "
            + " ".join(args)
            + f" (exit={completed.returncode}, stderr={completed.stderr.strip()!r})"
        )
    return completed.stdout


def commit_all(repo: Path, message: str) -> None:
    run_git(repo, "add", "--all")
    run_git(
        repo,
        "-c",
        "user.name=QS3D CI Regression",
        "-c",
        "user.email=ci-regression@example.invalid",
        "commit",
        "-m",
        message,
    )


def split_paths(output: str) -> list[str]:
    return [line for line in output.splitlines() if line]


def prove_rename_behavior() -> None:
    with tempfile.TemporaryDirectory(prefix="qs3d-ci-path-rename-") as temp_dir:
        repo = Path(temp_dir)
        run_git(repo, "init", "--quiet")
        (repo / "scripts").mkdir()
        (repo / "docs").mkdir()
        watched = repo / "scripts" / "preflight-rename-fixture.py"
        watched.write_text("print('fixture')\n", encoding="utf-8")
        commit_all(repo, "baseline")
        baseline = run_git(repo, "rev-parse", "HEAD").strip()

        run_git(repo, "mv", "scripts/preflight-rename-fixture.py", "docs/preflight-rename-fixture.py")
        commit_all(repo, "rename watched path into docs")

        rename_detected = split_paths(
            run_git(repo, "-c", "core.quotePath=false", "diff", "--name-only", f"{baseline}...HEAD", "--")
        )
        if rename_detected != ["docs/preflight-rename-fixture.py"]:
            fail(
                "hermetic control no longer demonstrates Git rename destination collapse: "
                + repr(rename_detected)
            )
        if classify_paths(rename_detected) != (False, False):
            fail("rename-detection control must demonstrate the historical docs-only misclassification")

        rename_safe = split_paths(
            run_git(
                repo,
                "-c",
                "core.quotePath=false",
                "diff",
                "--no-renames",
                "--name-only",
                f"{baseline}...HEAD",
                "--",
            )
        )
        expected = ["docs/preflight-rename-fixture.py", "scripts/preflight-rename-fixture.py"]
        if sorted(rename_safe) != sorted(expected):
            fail(f"rename-safe diff must expose both source and destination paths: {rename_safe!r}")
        if classify_paths(rename_safe) != (True, True):
            fail("watched-path rename must retain source/build validation")

        docs_baseline = run_git(repo, "rev-parse", "HEAD").strip()
        run_git(repo, "mv", "docs/preflight-rename-fixture.py", "docs/renamed-fixture.py")
        commit_all(repo, "rename docs path")
        docs_only = split_paths(
            run_git(
                repo,
                "-c",
                "core.quotePath=false",
                "diff",
                "--no-renames",
                "--name-only",
                f"{docs_baseline}...HEAD",
                "--",
            )
        )
        if classify_paths(docs_only) != (False, False):
            fail(f"docs-only rename must remain lightweight: {docs_only!r}")


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")

    required = (
        "git -c core.quotePath=false diff --no-renames --name-only",
        "$path = [string]$rawPath",
        "[string]::IsNullOrEmpty($path)",
        "Git returned a C-quoted changed path; refusing ambiguous validation classification",
        "$path = $path.Replace('\\', '/')",
    )
    for snippet in required:
        if snippet not in text:
            fail(f"Shared CI changed-path safety contract is missing: {snippet}")

    if "git -c core.quotePath=false diff --name-only" in text:
        fail("Shared CI must disable rename detection before name-only validation-scope classification")
    if "([string]$rawPath).Trim()" in text or "$path = $path.Trim()" in text:
        fail("Shared CI must not trim Git path tokens before validation-scope classification")

    quote_guard = text.index("Git returned a C-quoted changed path; refusing ambiguous validation classification")
    build_classifier = text.index("$path -match '^(src|tests|scripts)/'")
    if quote_guard > build_classifier:
        fail("C-quoted path rejection must occur before source/build path classification")

    controls = {
        "src/đường-dẫn/fixture.cs": (True, True),
        "scripts/preflight-unicode-ß.py": (True, True),
        "tests/name with trailing space ": (True, True),
        ".github/workflows/ci.yml": (True, True),
        ".gitmodules": (True, True),
        "Directory.Build.props": (True, True),
        "CI_POLICY.md": (True, False),
        "docs/ARCHITECTURE.md": (False, False),
    }
    for path, expected in controls.items():
        actual = classify_control(path)
        if actual != expected:
            fail(f"classification control failed for {path!r}: expected {expected}, got {actual}")

    quoted_controls = (
        '"src/control\\nname.cs"',
        '"scripts/quote\\"name.py"',
    )
    for path in quoted_controls:
        if not (len(path) >= 2 and path[0] == '"' and path[-1] == '"'):
            fail(f"quoted-path negative control is invalid: {path!r}")

    prove_rename_behavior()

    print("PASS: Shared CI preserves path identity, rejects ambiguous tokens, and retains watched rename scope")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
