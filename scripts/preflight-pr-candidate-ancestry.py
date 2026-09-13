#!/usr/bin/env python3
"""Hermetic regression for same-repository agent PR candidate ancestry admission."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
LANE_GUARD = ROOT / "scripts" / "preflight-agent-lane-collision.py"


def run_git(*args: str, cwd: Path) -> str:
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


def commit_file(repo: Path, name: str, content: str, message: str) -> str:
    (repo / name).write_text(content, encoding="utf-8")
    run_git("add", "--", name, cwd=repo)
    run_git("commit", "-m", message, cwd=repo)
    return run_git("rev-parse", "HEAD", cwd=repo)


def load_lane_guard():
    spec = importlib.util.spec_from_file_location("qs3d_lane_guard", LANE_GUARD)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load reservation gate module")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    lane = load_lane_guard()
    if not hasattr(lane, "agent_candidate_merge_commits"):
        raise SystemExit(
            "ERROR: unconditional reservation gate does not expose agent_candidate_merge_commits; "
            "docs/metadata-only PRs can still bypass ancestry admission"
        )
    if not hasattr(lane, "validate_agent_pr_candidate_ancestry"):
        raise SystemExit(
            "ERROR: unconditional reservation gate does not expose validate_agent_pr_candidate_ancestry"
        )

    with tempfile.TemporaryDirectory(prefix="qs3d-pr-ancestry-") as temporary:
        repo = Path(temporary)
        run_git("init", "-q", cwd=repo)
        run_git("config", "user.name", "QS3D CI", cwd=repo)
        run_git("config", "user.email", "qs3d-ci@example.invalid", cwd=repo)

        base = commit_file(repo, "base.txt", "base\n", "base")
        run_git("checkout", "-q", "-b", "linear", cwd=repo)
        commit_file(repo, "linear-a.txt", "a\n", "linear a")
        linear_head = commit_file(repo, "linear-b.txt", "b\n", "linear b")
        if lane.agent_candidate_merge_commits(base, linear_head, cwd=repo):
            raise RuntimeError("linear multi-commit candidate was incorrectly rejected")

        run_git("checkout", "-q", "-b", "topic", base, cwd=repo)
        commit_file(repo, "topic.txt", "topic\n", "topic work")
        run_git("checkout", "-q", "-b", "main-advance", base, cwd=repo)
        commit_file(repo, "main.txt", "main\n", "main advance")
        run_git("checkout", "-q", "topic", cwd=repo)
        run_git("merge", "--no-ff", "main-advance", "-m", "merge main into topic", cwd=repo)
        merged_head = run_git("rev-parse", "HEAD", cwd=repo)
        merges = lane.agent_candidate_merge_commits(base, merged_head, cwd=repo)
        if merges != [merged_head]:
            raise RuntimeError(
                "merge-contaminated candidate was not deterministically detected; "
                f"expected {[merged_head]!r}, got {merges!r}"
            )

    source = LANE_GUARD.read_text(encoding="utf-8")
    required = (
        "validate_agent_pr_candidate_ancestry(event, repository)",
        "agent PR candidate contains merge commit(s) in exact base..head range",
    )
    for token in required:
        if token not in source:
            raise SystemExit(f"ERROR: unconditional reservation gate ancestry integration missing {token!r}")

    print("PASS: agent PR ancestry admission is unconditional and hermetically detects merge contamination")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
