#!/usr/bin/env python3
"""Reject merge-contaminated same-repository agent PR candidate ancestry."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
CI_WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
HYBRID_WORKFLOW = ROOT / ".github" / "workflows" / "hybrid-pr-coordinator.yml"
SHA_RE = re.compile(r"^[0-9a-f]{40}$")


def run_git(*args: str, cwd: Path = ROOT) -> str:
    completed = subprocess.run(
        ["git", *args],
        cwd=cwd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="strict",
        check=False,
    )
    if completed.returncode != 0:
        detail = (completed.stderr or completed.stdout).strip()
        raise RuntimeError(f"git {' '.join(args)} failed: {detail}")
    return completed.stdout.strip()


def require_sha(raw: object, label: str) -> str:
    value = str(raw or "").strip().lower()
    if not SHA_RE.fullmatch(value):
        raise ValueError(f"{label} must be an exact 40-hex commit SHA")
    return value


def merge_commits(base_sha: str, head_sha: str, *, cwd: Path = ROOT) -> list[str]:
    base = require_sha(base_sha, "base SHA")
    head = require_sha(head_sha, "head SHA")
    output = run_git("rev-list", "--min-parents=2", f"{base}..{head}", cwd=cwd)
    if not output:
        return []
    merges = [line.strip().lower() for line in output.splitlines() if line.strip()]
    for merge in merges:
        require_sha(merge, "merge commit")
    if len(set(merges)) != len(merges):
        raise RuntimeError("git rev-list returned duplicate merge commit identities")
    return merges


def commit_file(repo: Path, name: str, content: str, message: str) -> str:
    (repo / name).write_text(content, encoding="utf-8")
    run_git("add", "--", name, cwd=repo)
    run_git("commit", "-m", message, cwd=repo)
    return require_sha(run_git("rev-parse", "HEAD", cwd=repo), "created commit")


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
            raise RuntimeError("linear multi-commit candidate was incorrectly rejected")

        run_git("checkout", "-q", "-b", "topic", base, cwd=repo)
        commit_file(repo, "topic.txt", "topic\n", "topic work")
        run_git("checkout", "-q", "-b", "main-advance", base, cwd=repo)
        commit_file(repo, "main.txt", "main\n", "main advance")
        run_git("checkout", "-q", "topic", cwd=repo)
        run_git("merge", "--no-ff", "main-advance", "-m", "merge main into topic", cwd=repo)
        merged_head = require_sha(run_git("rev-parse", "HEAD", cwd=repo), "merge head")
        merges = merge_commits(base, merged_head, cwd=repo)
        if merges != [merged_head]:
            raise RuntimeError(
                "merge-contaminated candidate was not deterministically detected; "
                f"expected {[merged_head]!r}, got {merges!r}"
            )


def validate_workflow_contracts() -> None:
    ci_source = CI_WORKFLOW.read_text(encoding="utf-8")
    required_ci_tokens = (
        "- name: Agent PR candidate ancestry gate",
        "github.event_name == 'pull_request'",
        "startsWith(github.event.pull_request.head.ref, 'agent/')",
        "github.event.pull_request.head.repo.full_name == github.repository",
        "github.actor != 'dependabot[bot]'",
        "python scripts/preflight-pr-candidate-ancestry.py --verify-runtime",
    )
    for token in required_ci_tokens:
        if token not in ci_source:
            raise RuntimeError(f"Shared CI ancestry hook is missing required token {token!r}")

    hybrid_source = HYBRID_WORKFLOW.read_text(encoding="utf-8")
    if "update_method=merge" in hybrid_source:
        raise RuntimeError(
            "Hybrid PR coordinator still contains update_method=merge; "
            "the coordinator can reintroduce merge-contaminated agent ancestry"
        )
    if hybrid_source.count("update_method=rebase") < 2:
        raise RuntimeError("Hybrid PR coordinator no longer contains both rebase-first refresh paths")
    if hybrid_source.count("clean replay/rebuild") < 2:
        raise RuntimeError("Hybrid PR coordinator must explain clean replay/rebuild on declined rebases")


def event_object(raw: object, label: str) -> dict:
    if not isinstance(raw, dict):
        raise ValueError(f"event {label} object is missing")
    return raw


def event_string(raw: object, label: str) -> str:
    value = str(raw or "").strip()
    if not value:
        raise ValueError(f"event {label} is missing")
    return value


def validate_runtime() -> None:
    if os.environ.get("GITHUB_EVENT_NAME") != "pull_request":
        raise ValueError("runtime ancestry verification requires a pull_request event")

    event_path = os.environ.get("GITHUB_EVENT_PATH", "").strip()
    if not event_path:
        raise ValueError("GITHUB_EVENT_PATH is required")
    try:
        event = json.loads(Path(event_path).read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise ValueError(f"pull_request event payload is unreadable: {exc}") from exc
    if not isinstance(event, dict):
        raise ValueError("pull_request event payload root must be an object")

    pr = event_object(event.get("pull_request"), "pull_request")
    head = event_object(pr.get("head"), "pull_request.head")
    base = event_object(pr.get("base"), "pull_request.base")
    head_repo = event_object(head.get("repo"), "pull_request.head.repo")
    base_repo = event_object(base.get("repo"), "pull_request.base.repo")
    author = event_object(pr.get("user"), "pull_request.user")

    head_ref = event_string(head.get("ref"), "pull_request.head.ref")
    head_repo_name = event_string(head_repo.get("full_name"), "pull_request.head.repo.full_name")
    base_repo_name = event_string(base_repo.get("full_name"), "pull_request.base.repo.full_name")
    author_login = event_string(author.get("login"), "pull_request.user.login")

    if author_login == "dependabot[bot]":
        print("PASS: Dependabot PR is outside agent candidate ancestry admission.")
        return
    if head_repo_name != base_repo_name:
        print("PASS: fork PR is outside same-repository agent candidate ancestry admission.")
        return
    if not head_ref.startswith("agent/"):
        print("PASS: non-agent PR is outside agent candidate ancestry admission.")
        return

    base_sha = require_sha(base.get("sha"), "event pull_request.base.sha")
    head_sha = require_sha(head.get("sha"), "event pull_request.head.sha")
    actual_head = require_sha(run_git("rev-parse", "HEAD^{commit}"), "checked-out HEAD")
    if actual_head != head_sha:
        raise ValueError(
            f"checked-out HEAD identity drifted: event head is {head_sha}, checkout is {actual_head}"
        )

    resolved_base = require_sha(run_git("rev-parse", f"{base_sha}^{{commit}}"), "resolved event base")
    resolved_head = require_sha(run_git("rev-parse", f"{head_sha}^{{commit}}"), "resolved event head")
    if resolved_base != base_sha or resolved_head != head_sha:
        raise ValueError("event base/head commit identity drifted during exact ancestry resolution")

    merge_base = require_sha(run_git("merge-base", base_sha, head_sha), "merge-base")
    if merge_base != base_sha:
        raise ValueError(
            "agent PR exact event base is not an ancestor of event head; "
            "rebuild/replay the carrier directly from current protected main"
        )

    merges = merge_commits(base_sha, head_sha)
    if merges:
        raise ValueError(
            "agent PR candidate contains merge commit(s) in exact base..head range: "
            + ", ".join(merges[:8])
            + "; rebuild/replay the carrier instead of merging protected main into the topic branch"
        )

    print(f"PASS: agent PR candidate {head_ref}@{head_sha} has linear exact base..head ancestry.")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--verify-runtime", action="store_true")
    args = parser.parse_args()
    try:
        self_test()
        validate_workflow_contracts()
        if args.verify_runtime:
            validate_runtime()
    except (OSError, RuntimeError, ValueError, subprocess.SubprocessError, UnicodeError) as exc:
        print("ERROR: agent PR candidate ancestry preflight failed closed:", exc)
        return 1

    if not args.verify_runtime:
        print("PASS: agent PR ancestry deterministic regression and workflow contracts are valid.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
