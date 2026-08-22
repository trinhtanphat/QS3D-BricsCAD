#!/usr/bin/env python3
"""Regression guard for publication-based V25 preview batch baselines."""

from __future__ import annotations

from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
GATE = ROOT / "scripts" / "v25-release-batch-gate.py"
DISPATCHER = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
SERIES = "v0.1.0-preview."


class RegressionError(RuntimeError):
    pass


def run(command: list[str], cwd: Path, *, check: bool = True) -> subprocess.CompletedProcess[str]:
    completed = subprocess.run(
        command,
        cwd=str(cwd),
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        encoding="utf-8",
        errors="replace",
    )
    if check and completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip() or f"exit {completed.returncode}"
        raise RegressionError(f"command failed ({' '.join(command)}): {detail}")
    return completed


def git(repo: Path, *args: str) -> str:
    return run(["git", *args], repo).stdout.strip()


def commit_file(repo: Path, relative: str, content: str, subject: str) -> str:
    path = repo / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")
    git(repo, "add", relative)
    git(repo, "commit", "-q", "-m", subject)
    return git(repo, "rev-parse", "HEAD")


def run_gate(repo: Path, source_sha: str, *extra: str) -> subprocess.CompletedProcess[str]:
    return run(
        [
            sys.executable,
            str(GATE),
            "--source-sha",
            source_sha,
            "--minimum-changes",
            "10",
            *extra,
        ],
        repo,
        check=False,
    )


def expect_contains(haystack: str, needle: str, context: str) -> None:
    if needle not in haystack:
        raise RegressionError(f"{context}: missing {needle!r}\n--- output ---\n{haystack}")


def validate_gate_behavior() -> None:
    with tempfile.TemporaryDirectory(prefix="qs3d-v25-published-baseline-") as temp_dir:
        repo = Path(temp_dir)
        git(repo, "init", "-q")
        git(repo, "config", "user.name", "QS3D regression")
        git(repo, "config", "user.email", "qs3d-regression@example.invalid")

        commit_file(repo, "README.md", "root\n", "root")
        commit_file(repo, "scripts/a.py", "A = 1\n", "relevant A")
        git(repo, "tag", f"{SERIES}1")
        commit_file(repo, "scripts/b.py", "B = 1\n", "relevant B")
        git(repo, "tag", f"{SERIES}99")
        source_sha = commit_file(repo, "scripts/c.py", "C = 1\n", "relevant C")

        explicit = run_gate(repo, source_sha, "--previous-published-tag", f"{SERIES}1")
        if explicit.returncode != 0:
            raise RegressionError(f"explicit published baseline failed: {explicit.stderr or explicit.stdout}")
        expect_contains(explicit.stdout, "Baseline mode: explicit-published-release", "explicit baseline")
        expect_contains(explicit.stdout, f"Previous preview: {SERIES}1", "explicit baseline")
        expect_contains(explicit.stdout, "Release-relevant main integrations: 2/10", "explicit baseline")

        no_published = run_gate(repo, source_sha, "--previous-published-tag", "")
        if no_published.returncode != 0:
            raise RegressionError(f"explicit empty published baseline failed: {no_published.stderr or no_published.stdout}")
        expect_contains(no_published.stdout, "Previous preview: (none)", "explicit empty baseline")
        expect_contains(no_published.stdout, "Release-relevant main integrations: 3/10", "explicit empty baseline")

        legacy = run_gate(repo, source_sha)
        if legacy.returncode != 0:
            raise RegressionError(f"legacy local-tag fallback failed: {legacy.stderr or legacy.stdout}")
        expect_contains(legacy.stdout, "Baseline mode: legacy-local-tag-discovery", "legacy baseline")
        expect_contains(legacy.stdout, f"Previous preview: {SERIES}99", "legacy baseline")
        expect_contains(legacy.stdout, "Release-relevant main integrations: 1/10", "legacy baseline")

        invalid = run_gate(repo, source_sha, "--previous-published-tag", f"{SERIES}0")
        if invalid.returncode != 2:
            raise RegressionError(f"non-canonical explicit baseline should exit 2, got {invalid.returncode}")
        expect_contains(invalid.stderr, "published preview baseline is non-canonical", "invalid baseline")

        missing = run_gate(repo, source_sha, "--previous-published-tag", f"{SERIES}2")
        if missing.returncode != 2:
            raise RegressionError(f"missing explicit baseline ref should exit 2, got {missing.returncode}")
        expect_contains(missing.stderr, "refs/tags/", "missing baseline")


def validate_dispatcher_contract() -> None:
    source = DISPATCHER.read_text(encoding="utf-8")
    required = (
        'gh api --paginate "repos/${GITHUB_REPOSITORY}/releases?per_page=100"',
        'if [[ "${release_draft}" != "false" || -z "${release_published_at}" ]]',
        'if [[ ! "${release_tag}" =~ ^v0\\.1\\.0-preview\\.([1-9][0-9]*)$ ]]',
        'if (( ordinal > published_preview_ordinal ))',
        '--previous-published-tag "${published_preview_tag}"',
        'consider_preview "${BASH_REMATCH[1]}" "Existing tag"',
    )
    for fragment in required:
        if fragment not in source:
            raise RegressionError(f"dispatcher publication-baseline contract missing source fragment: {fragment}")

    if 'consider_preview "${BASH_REMATCH[1]}" "Published"' in source:
        raise RegressionError("dispatcher still labels arbitrary local preview tags as published")

    release_query = source.index('releases?per_page=100')
    gate_call = source.index('--previous-published-tag "${published_preview_tag}"')
    if release_query >= gate_call:
        raise RegressionError("dispatcher must derive the published Release baseline before invoking the batch gate")


def main() -> int:
    validate_gate_behavior()
    validate_dispatcher_contract()
    print("PASS: V25 preview batch baseline is publication-based and regression-covered.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except RegressionError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
