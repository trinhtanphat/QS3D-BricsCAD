#!/usr/bin/env python3
"""Regression test for Shared CI changed-path tokenization, rename and execution bounds."""

from __future__ import annotations

import importlib.util
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
CLASSIFIER = ROOT / "scripts" / "ci-validation-scope.py"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def load_classifier():
    spec = importlib.util.spec_from_file_location("qs3d_ci_validation_scope", CLASSIFIER)
    if spec is None or spec.loader is None:
        fail("could not load production validation-scope classifier")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


SCOPE = load_classifier()


def run_git(repo: Path, *args: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        timeout=15,
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


def expect_scope_error(action, expected: str) -> None:
    try:
        action()
    except SCOPE.ScopeError as exc:
        if expected not in str(exc):
            fail(f"expected ScopeError containing {expected!r}, got {str(exc)!r}")
        return
    fail(f"expected ScopeError containing {expected!r}")


def prove_lossless_records() -> None:
    expected = [
        "scripts/line\nbreak.py",
        "tests/tab\tname.cs",
        'src/quote"name.cs',
        "scripts/back\\slash.py",
        "scripts/unicode-đường-dẫn-ß.py",
        "tests/name with trailing space ",
        "docs/line\nbreak.md",
    ]
    raw = b"\0".join(path.encode("utf-8") for path in expected) + b"\0"
    actual = SCOPE.parse_nul_paths(raw)
    if actual != expected:
        fail(f"NUL-delimited path records did not preserve exact identity: {actual!r}")

    source, build = SCOPE.classify_paths(actual)
    if (source, build) != (True, True):
        fail("unusual watched path records must retain source/build validation")

    if SCOPE.classify_path("docs/line\nbreak.md") != (False, False):
        fail("unusual docs-only path must remain lightweight")
    if SCOPE.classify_path("scripts\\literal-root-name.py") != (False, False):
        fail("a literal backslash in a root filename must not be rewritten into a watched directory separator")

    malformed = (
        (b"scripts/not-terminated.py", "unterminated output"),
        (b"scripts/one.py\0\0scripts/two.py\0", "empty record"),
        (b"scripts/bad-\xff.py\0", "invalid UTF-8"),
    )
    for payload, label in malformed:
        try:
            SCOPE.parse_nul_paths(payload)
        except SCOPE.ScopeError:
            continue
        fail(f"production parser accepted {label}")

    historical = '"scripts/line\\nbreak.py"\n'
    historical_tokens = split_paths(historical)
    if historical_tokens != ['"scripts/line\\nbreak.py"']:
        fail("historical C-quoted control is invalid")
    if SCOPE.classify_paths(historical_tokens) != (False, False):
        fail("historical line-tokenized control must demonstrate why quoted records cannot be classified safely")


def prove_bounded_process_contract() -> None:
    with tempfile.TemporaryDirectory(prefix="qs3d-ci-scope-bounds-") as temp_dir:
        cwd = Path(temp_dir)

        success = SCOPE.run_bounded_process(
            [
                sys.executable,
                "-S",
                "-c",
                "import sys; sys.stdout.buffer.write(b'scripts/ok.py\\0'); "
                "sys.stderr.buffer.write(b'note'); sys.stdout.flush(); sys.stderr.flush()",
            ],
            cwd=cwd,
            timeout_seconds=2.0,
            max_stdout_bytes=1024,
            max_stderr_bytes=1024,
        )
        if success.returncode != 0 or success.stdout != b"scripts/ok.py\0" or success.stderr != b"note":
            fail(f"bounded process success contract changed: {success!r}")

        nonzero = SCOPE.run_bounded_process(
            [
                sys.executable,
                "-S",
                "-c",
                "import sys; sys.stderr.write('bounded diagnostic'); raise SystemExit(7)",
            ],
            cwd=cwd,
            timeout_seconds=2.0,
            max_stdout_bytes=1024,
            max_stderr_bytes=1024,
        )
        if nonzero.returncode != 7 or nonzero.stderr != b"bounded diagnostic":
            fail(f"bounded nonzero diagnostic contract changed: {nonzero!r}")

        expect_scope_error(
            lambda: SCOPE.run_bounded_process(
                [sys.executable, "-S", "-c", "import sys; sys.stdout.buffer.write(b'x' * 8192); sys.stdout.flush()"],
                cwd=cwd,
                timeout_seconds=2.0,
                max_stdout_bytes=128,
                max_stderr_bytes=1024,
            ),
            "Git changed-path output exceeded 128-byte limit",
        )

        expect_scope_error(
            lambda: SCOPE.run_bounded_process(
                [sys.executable, "-S", "-c", "import sys; sys.stderr.buffer.write(b'e' * 8192); sys.stderr.flush()"],
                cwd=cwd,
                timeout_seconds=2.0,
                max_stdout_bytes=1024,
                max_stderr_bytes=128,
            ),
            "Git diagnostic output exceeded 128-byte limit",
        )

        expect_scope_error(
            lambda: SCOPE.run_bounded_process(
                [sys.executable, "-S", "-c", "import time; time.sleep(5)"],
                cwd=cwd,
                timeout_seconds=0.1,
                max_stdout_bytes=1024,
                max_stderr_bytes=1024,
            ),
            "timed out after 0.1 seconds",
        )

        missing = cwd / "definitely-missing-qs3d-executable"
        expect_scope_error(
            lambda: SCOPE.run_bounded_process(
                [str(missing)],
                cwd=cwd,
                timeout_seconds=1.0,
                max_stdout_bytes=1024,
                max_stderr_bytes=1024,
            ),
            "could not launch changed-path command",
        )


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
        if SCOPE.classify_paths(rename_detected) != (False, False):
            fail("rename-detection control must demonstrate the historical docs-only misclassification")

        rename_safe = SCOPE.changed_paths(baseline, root=repo)
        expected = ["docs/preflight-rename-fixture.py", "scripts/preflight-rename-fixture.py"]
        if sorted(rename_safe) != sorted(expected):
            fail(f"rename-safe NUL diff must expose both source and destination paths: {rename_safe!r}")
        if SCOPE.classify_paths(rename_safe) != (True, True):
            fail("watched-path rename must retain source/build validation")

        docs_baseline = run_git(repo, "rev-parse", "HEAD").strip()
        run_git(repo, "mv", "docs/preflight-rename-fixture.py", "docs/renamed-fixture.py")
        commit_all(repo, "rename docs path")
        docs_only = SCOPE.changed_paths(docs_baseline, root=repo)
        if SCOPE.classify_paths(docs_only) != (False, False):
            fail(f"docs-only rename must remain lightweight: {docs_only!r}")


def prove_workflow_contract() -> None:
    text = WORKFLOW.read_text(encoding="utf-8")
    required = (
        "python scripts/ci-validation-scope.py --all --github-output $env:GITHUB_OUTPUT",
        'python scripts/ci-validation-scope.py --base "origin/$baseBranch" --head HEAD --github-output $env:GITHUB_OUTPUT',
        'git fetch --no-tags origin "+refs/heads/$baseBranch`:refs/remotes/origin/$baseBranch"',
        "$classificationExitCode = 0",
        "$classificationExitCode = $LASTEXITCODE",
        "if ($classificationExitCode -ne 0) { throw 'Validation scope classification failed.' }",
    )
    for snippet in required:
        if snippet not in text:
            fail(f"Shared CI NUL-safe changed-path contract is missing: {snippet}")

    forbidden = (
        "git -c core.quotePath=false diff --no-renames --name-only",
        "Git returned a C-quoted changed path",
        "$path = $path.Replace('\\', '/')",
        "foreach ($rawPath in $changed)",
        "if ($LASTEXITCODE -ne 0) { throw 'Validation scope classification failed.' }",
    )
    for snippet in forbidden:
        if snippet in text:
            fail(f"Shared CI still contains line/C-quote or stale-exit changed-path handling: {snippet}")

    source = CLASSIFIER.read_text(encoding="utf-8")
    production_required = (
        "GIT_DIFF_TIMEOUT_SECONDS = 30.0",
        "MAX_CHANGED_PATH_BYTES = 4 * 1024 * 1024",
        "MAX_GIT_DIAGNOSTIC_BYTES = 64 * 1024",
        "run_bounded_process(",
        "stdout=subprocess.PIPE",
        "stderr=subprocess.PIPE",
    )
    for snippet in production_required:
        if snippet not in source:
            fail(f"validation-scope execution bound is missing: {snippet}")
    if "subprocess.run(" in source:
        fail("production validation-scope classifier must not return to unbounded subprocess.run capture")


def main() -> int:
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
        actual = SCOPE.classify_path(path)
        if actual != expected:
            fail(f"classification control failed for {path!r}: expected {expected}, got {actual}")

    prove_lossless_records()
    prove_bounded_process_contract()
    prove_rename_behavior()
    prove_workflow_contract()

    print("PASS: Shared CI changed-path classification is lossless, rename-safe and execution-bounded")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
