#!/usr/bin/env python3
"""Fail closed when an active same-repository agent topic contains merge commits."""

from __future__ import annotations

import json
import os
import re
import subprocess
import tempfile
from pathlib import Path

AGENT_PREFIX = "agent/"
HEX40_RE = re.compile(r"^[0-9a-f]{40}$")


def run_git(args: list[str], cwd: Path | None = None) -> str:
    completed = subprocess.run(
        ["git", *args],
        cwd=str(cwd) if cwd is not None else None,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="strict",
    )
    if completed.returncode != 0:
        detail = (completed.stderr or completed.stdout).strip()
        raise RuntimeError(f"git {' '.join(args)} failed: {detail}")
    return completed.stdout.strip()


def merge_commits_between(base_ref: str, head_ref: str = "HEAD", cwd: Path | None = None) -> list[str]:
    base = run_git(["rev-parse", f"{base_ref}^{{commit}}"], cwd).lower()
    head = run_git(["rev-parse", f"{head_ref}^{{commit}}"], cwd).lower()
    if not HEX40_RE.fullmatch(base) or not HEX40_RE.fullmatch(head):
        raise RuntimeError("topic ancestry check requires exact 40-hex base/head commit identities")

    merge_base = run_git(["merge-base", base, head], cwd).lower()
    if not HEX40_RE.fullmatch(merge_base):
        raise RuntimeError("topic ancestry check could not resolve an exact merge-base")

    raw = run_git(["rev-list", "--min-parents=2", "--format=%H", f"{merge_base}..{head}"], cwd)
    commits: list[str] = []
    for line in raw.splitlines():
        value = line.strip().removeprefix("commit ").lower()
        if not value:
            continue
        if not HEX40_RE.fullmatch(value):
            raise RuntimeError(f"topic ancestry check returned invalid merge commit identity: {line!r}")
        if value not in commits:
            commits.append(value)
    return commits


def assert_linear_topic_history(base_ref: str, head_ref: str = "HEAD", cwd: Path | None = None) -> None:
    merges = merge_commits_between(base_ref, head_ref, cwd)
    if merges:
        detail = ", ".join(merges[:8])
        raise ValueError(
            "agent topic ancestry contains merge commit(s) after its protected-base merge-base: "
            + detail
            + "; rebuild/rebase a clean linear carrier from current protected main instead of merging main into the topic"
        )


def commit_file(repo: Path, name: str, text: str, message: str) -> str:
    (repo / name).write_text(text, encoding="utf-8")
    run_git(["add", "--", name], repo)
    run_git(["commit", "-m", message], repo)
    return run_git(["rev-parse", "HEAD"], repo).lower()


def hermetic_regression() -> None:
    with tempfile.TemporaryDirectory(prefix="qs3d-topic-linear-") as temp:
        repo = Path(temp)
        run_git(["init"], repo)
        run_git(["config", "user.name", "QS3D C05 Preflight"], repo)
        run_git(["config", "user.email", "c05-preflight@example.invalid"], repo)
        run_git(["checkout", "-b", "main"], repo)
        commit_file(repo, "base.txt", "base\n", "base")
        base = run_git(["rev-parse", "HEAD"], repo)

        run_git(["checkout", "-b", "linear"], repo)
        commit_file(repo, "topic-a.txt", "a\n", "topic a")
        commit_file(repo, "topic-b.txt", "b\n", "topic b")
        assert_linear_topic_history(base, "HEAD", repo)

        run_git(["checkout", "main"], repo)
        commit_file(repo, "main-advance.txt", "main advance\n", "main advance")
        run_git(["checkout", "linear"], repo)
        run_git(["merge", "--no-ff", "main", "-m", "merge main into topic"], repo)
        contaminated = merge_commits_between(base, "HEAD", repo)
        if len(contaminated) != 1:
            raise RuntimeError(
                f"hermetic topic ancestry regression expected exactly one introduced merge commit, got {contaminated}"
            )
        try:
            assert_linear_topic_history(base, "HEAD", repo)
        except ValueError:
            pass
        else:
            raise RuntimeError("hermetic topic ancestry regression failed to reject merge-contaminated history")


def scoped_event() -> tuple[bool, str]:
    event_name = os.environ.get("GITHUB_EVENT_NAME", "").strip()
    repository = os.environ.get("GITHUB_REPOSITORY", "").strip()
    event_path = os.environ.get("GITHUB_EVENT_PATH", "").strip()
    if event_name not in {"pull_request", "push"} or not repository or not event_path:
        return False, ""

    try:
        event = json.loads(Path(event_path).read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise RuntimeError(f"cannot read GitHub event payload for topic ancestry admission: {exc}") from exc
    if not isinstance(event, dict):
        raise RuntimeError("GitHub event payload root is not an object")

    if event_name == "pull_request":
        pr = event.get("pull_request")
        if not isinstance(pr, dict):
            raise RuntimeError("pull_request event is missing pull_request object")
        head = pr.get("head") or {}
        head_ref = str(head.get("ref") or "")
        head_repo = str((head.get("repo") or {}).get("full_name") or "")
        if head_repo != repository or not head_ref.startswith(AGENT_PREFIX):
            return False, ""
        base_ref = str((pr.get("base") or {}).get("ref") or os.environ.get("GITHUB_BASE_REF") or "").strip()
        if not base_ref:
            raise RuntimeError("agent pull_request event is missing its protected base ref")
        return True, base_ref

    ref = str(event.get("ref") or "")
    head_ref = str(os.environ.get("GITHUB_REF_NAME") or ref.removeprefix("refs/heads/"))
    if not head_ref.startswith(AGENT_PREFIX):
        return False, ""
    return True, "main"


def main() -> int:
    try:
        hermetic_regression()
        scoped, base_ref = scoped_event()
        if not scoped:
            print("PASS: topic linear-history regression is green; event is outside same-repository agent scope.")
            return 0
        assert_linear_topic_history(f"origin/{base_ref}")
    except (OSError, RuntimeError, ValueError, subprocess.SubprocessError) as exc:
        print("ERROR: agent topic linear-history preflight failed closed:", exc)
        return 1

    print(f"PASS: active agent topic is linear relative to protected base '{base_ref}'.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
