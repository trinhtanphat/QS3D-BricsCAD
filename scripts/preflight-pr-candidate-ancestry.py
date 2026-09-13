#!/usr/bin/env python3
"""Reject merge-contaminated same-repository agent PR candidate ancestry.

`preflight-all.py` auto-discovers this guard on every source-validation run. Static
self-tests execute everywhere; on pull_request events the guard binds to the event's
exact base/head SHAs and inspects the real candidate commits, never GitHub's synthetic
merge ref. Integration branches intentionally retain their batch/merge semantics.
"""

from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
DEPENDABOT_LOGIN = "dependabot[bot]"


def fail(message: str) -> int:
    print(f"ERROR: {message}", file=sys.stderr)
    return 1


def run_git(*args: str, cwd: Path = ROOT, check: bool = True) -> subprocess.CompletedProcess[str]:
    completed = subprocess.run(
        ["git", *args],
        cwd=cwd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="strict",
        check=False,
    )
    if check and completed.returncode != 0:
        detail = (completed.stderr or completed.stdout).strip()
        raise RuntimeError(f"git {' '.join(args)} failed: {detail}")
    return completed


def merge_commits(base_sha: str, head_sha: str, *, cwd: Path = ROOT) -> list[str]:
    completed = run_git("rev-list", "--min-parents=2", f"{base_sha}..{head_sha}", cwd=cwd)
    return [line.strip() for line in completed.stdout.splitlines() if line.strip()]


def commit_file(repo: Path, name: str, content: str, message: str) -> str:
    (repo / name).write_text(content, encoding="utf-8")
    run_git("add", "--", name, cwd=repo)
    run_git("commit", "-m", message, cwd=repo)
    return run_git("rev-parse", "HEAD", cwd=repo).stdout.strip()


def self_test() -> None:
    with tempfile.TemporaryDirectory(prefix="qs3d-pr-ancestry-") as temporary:
        repo = Path(temporary)
        run_git("init", "-q", cwd=repo)
        run_git("config", "user.name", "QS3D CI", cwd=repo)
        run_git("config", "user.email", "qs3d-ci@example.invalid", cwd=repo)

        base = commit_file(repo, "base.txt", "base\n", "base")

        run_git("checkout", "-q", "-b", "linear", cwd=repo)
        commit_file(repo, "linear-a.txt", "a\n", "linear a")
        linear_head = commit_file(repo, "linear-b.txt", "b\n", "linear b")
        if merge_commits(base, linear_head, cwd=repo):
            raise RuntimeError("linear multi-commit candidate was incorrectly classified as merge-contaminated")

        run_git("checkout", "-q", "-b", "topic", base, cwd=repo)
        commit_file(repo, "topic.txt", "topic\n", "topic work")
        run_git("checkout", "-q", "-b", "main-advance", base, cwd=repo)
        commit_file(repo, "main.txt", "main\n", "main advance")
        run_git("checkout", "-q", "topic", cwd=repo)
        run_git("merge", "--no-ff", "main-advance", "-m", "merge main into topic", cwd=repo)
        merged_head = run_git("rev-parse", "HEAD", cwd=repo).stdout.strip()
        merges = merge_commits(base, merged_head, cwd=repo)
        if merges != [merged_head]:
            raise RuntimeError(
                "merge-contaminated candidate was not deterministically detected; "
                f"expected {[merged_head]!r}, got {merges!r}"
            )


def require_object(value: Any, label: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise RuntimeError(f"{label} must be a JSON object")
    return value


def require_string(value: Any, label: str) -> str:
    if not isinstance(value, str) or not value or value != value.strip():
        raise RuntimeError(f"{label} must be a non-empty canonical string")
    return value


def require_sha(value: Any, label: str) -> str:
    text = require_string(value, label)
    if len(text) != 40 or any(ch not in "0123456789abcdefABCDEF" for ch in text):
        raise RuntimeError(f"{label} must be a 40-hex commit SHA")
    return text.lower()


def read_event() -> dict[str, Any]:
    event_path = os.environ.get("GITHUB_EVENT_PATH", "").strip()
    if not event_path:
        raise RuntimeError("GITHUB_EVENT_PATH is required on pull_request runs")
    path = Path(event_path)
    if not path.is_file():
        raise RuntimeError(f"GitHub event payload not found: {path}")
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise RuntimeError(f"could not read GitHub event payload: {exc}") from exc
    return require_object(value, "GitHub event payload")


def ensure_commit(sha: str, label: str) -> None:
    completed = run_git("cat-file", "-e", f"{sha}^{{commit}}", check=False)
    if completed.returncode != 0:
        raise RuntimeError(f"exact {label} commit {sha} is not present in the checkout")


def validate_live_candidate(event: dict[str, Any]) -> str:
    pr = require_object(event.get("pull_request"), "pull_request")
    user = require_object(pr.get("user"), "pull_request.user")
    author = require_string(user.get("login"), "pull_request.user.login")
    if author == DEPENDABOT_LOGIN:
        return "Dependabot standing exception applies"

    head = require_object(pr.get("head"), "pull_request.head")
    base = require_object(pr.get("base"), "pull_request.base")
    head_repo = require_object(head.get("repo"), "pull_request.head.repo")
    base_repo = require_object(base.get("repo"), "pull_request.base.repo")
    head_repo_name = require_string(head_repo.get("full_name"), "pull_request.head.repo.full_name")
    base_repo_name = require_string(base_repo.get("full_name"), "pull_request.base.repo.full_name")
    if head_repo_name.casefold() != base_repo_name.casefold():
        return "external/fork PR; agent-topic ancestry policy not applied"

    head_ref = require_string(head.get("ref"), "pull_request.head.ref")
    if head_ref.startswith("integration/"):
        return "integration branch; batch merge semantics retained"
    if not head_ref.startswith("agent/"):
        return "non-agent internal branch; agent-topic ancestry policy not applied"

    head_sha = require_sha(head.get("sha"), "pull_request.head.sha")
    base_sha = require_sha(base.get("sha"), "pull_request.base.sha")
    actual_head = run_git("rev-parse", "HEAD").stdout.strip().lower()
    if actual_head != head_sha:
        raise RuntimeError(f"checked-out HEAD mismatch: expected event head {head_sha}, got {actual_head}")

    ensure_commit(head_sha, "head")
    ensure_commit(base_sha, "base")
    common = run_git("merge-base", base_sha, head_sha, check=False)
    if common.returncode != 0 or not common.stdout.strip():
        raise RuntimeError(f"candidate {head_sha} has no merge base with event base {base_sha}")

    merges = merge_commits(base_sha, head_sha)
    if merges:
        raise RuntimeError(
            "agent PR candidate contains merge commit(s) in exact base..head range: "
            + ", ".join(merges)
            + ". Rebuild/replay the carrier linearly from current protected main instead of merging branches into it."
        )
    return f"agent candidate {head_ref}@{head_sha} has linear exact base..head ancestry"


def main() -> int:
    try:
        self_test()
        if os.environ.get("GITHUB_EVENT_NAME") != "pull_request":
            print("PR candidate ancestry guard PASS: deterministic self-tests passed; live PR check not applicable.")
            return 0
        result = validate_live_candidate(read_event())
    except RuntimeError as exc:
        return fail(str(exc))

    print("PR candidate ancestry guard PASS: " + result)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
