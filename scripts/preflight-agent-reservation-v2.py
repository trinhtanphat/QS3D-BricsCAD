#!/usr/bin/env python3
"""Focused regression for Reservation-v2 peer/current collision against effective protected-base changes."""

from __future__ import annotations

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-agent-lane-collision.py"
REPOSITORY = "trinhtanphat/QS3D-BricsCAD"
CURRENT_HEAD = "agent/gpt56sol-20260908-c05-reservation-peer-current-delta/issue-6100-reservation-peer-current-delta"
PEER_HEAD = "agent/gpt56sol-20260908-c05-v25-final-publish-main-stability/issue-6095-v25-final-publish-main-stability"
STALE_PATH = "scripts/publish-v26-release.ps1"
PEER_ONLY_PATH = "scripts/publish-v25-release.ps1"
MAIN_ONLY_PATH = "scripts/main-only.ps1"
CURRENT_MAIN_SHA = "a" * 40
PEER_HEAD_SHA = "b" * 40
MERGE_BASE_SHA = "c" * 40
CURRENT_HEAD_SHA = "d" * 40


def load_target():
    spec = importlib.util.spec_from_file_location("agent_lane_collision_preflight", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load agent reservation collision preflight")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def issue(number: int, created_at: str) -> dict:
    return {
        "number": number,
        "created_at": created_at,
        "body": f"Lane-Key: issue-{number}\nReservation-Protocol: v2\n",
        "title": f"reservation {number}",
        "state": "open",
    }


def peer(repo: str = REPOSITORY) -> dict:
    return {
        "number": 6096,
        "created_at": "2026-09-07T23:44:46Z",
        "head": {
            "ref": PEER_HEAD,
            "sha": PEER_HEAD_SHA,
            "repo": {"full_name": repo},
        },
    }


def assert_effective_peer_delta(gate):
    exact_calls = []

    def run_git(args):
        assert args == ["merge-base", CURRENT_MAIN_SHA, PEER_HEAD_SHA], args
        return MERGE_BASE_SHA

    def run_git_exact(args):
        exact_calls.append(args)
        left, right = args[5], args[6]
        if (left, right) == (MERGE_BASE_SHA, PEER_HEAD_SHA):
            # STALE_PATH was introduced on the peer but later converged to main.
            return f"{STALE_PATH}\x00{PEER_ONLY_PATH}\x00"
        if (left, right) == (CURRENT_MAIN_SHA, PEER_HEAD_SHA):
            # MAIN_ONLY_PATH differs because main advanced, but the peer never introduced it.
            return f"{PEER_ONLY_PATH}\x00{MAIN_ONLY_PATH}\x00"
        raise AssertionError(args)

    gate._run_git = run_git
    gate._run_git_exact = run_git_exact
    paths = gate.effective_changed_paths(CURRENT_MAIN_SHA, PEER_HEAD_SHA)
    assert paths == [PEER_ONLY_PATH], paths
    expected_prefix = [
        "diff",
        "--name-only",
        "-z",
        "--no-renames",
        "--diff-filter=ACDMRTUXB",
    ]
    assert exact_calls == [
        expected_prefix + [MERGE_BASE_SHA, PEER_HEAD_SHA, "--"],
        expected_prefix + [CURRENT_MAIN_SHA, PEER_HEAD_SHA, "--"],
    ], exact_calls


def assert_current_delta_is_nul_safe_and_includes_deletes(gate):
    git_calls = []
    exact_calls = []
    deleted = "scripts/deleted file.ps1"

    def run_git(args):
        git_calls.append(args)
        if args == ["rev-parse", "origin/main^{commit}"]:
            return CURRENT_MAIN_SHA
        if args == ["rev-parse", "HEAD^{commit}"]:
            return CURRENT_HEAD_SHA
        if args == ["merge-base", CURRENT_MAIN_SHA, CURRENT_HEAD_SHA]:
            return MERGE_BASE_SHA
        raise AssertionError(args)

    def run_git_exact(args):
        exact_calls.append(args)
        left, right = args[5], args[6]
        if (left, right) in {
            (MERGE_BASE_SHA, CURRENT_HEAD_SHA),
            (CURRENT_MAIN_SHA, CURRENT_HEAD_SHA),
        }:
            return deleted + "\x00"
        raise AssertionError(args)

    gate._run_git = run_git
    gate._run_git_exact = run_git_exact
    paths = gate.current_changed_paths("main")
    assert paths == [deleted], paths
    assert git_calls == [
        ["rev-parse", "origin/main^{commit}"],
        ["rev-parse", "HEAD^{commit}"],
        ["merge-base", CURRENT_MAIN_SHA, CURRENT_HEAD_SHA],
    ], git_calls
    expected_prefix = [
        "diff",
        "--name-only",
        "-z",
        "--no-renames",
        "--diff-filter=ACDMRTUXB",
    ]
    assert exact_calls == [
        expected_prefix + [MERGE_BASE_SHA, CURRENT_HEAD_SHA, "--"],
        expected_prefix + [CURRENT_MAIN_SHA, CURRENT_HEAD_SHA, "--"],
    ], exact_calls


def run_case(gate, peer_paths: list[str]):
    current = issue(6100, "2026-09-08T01:21:37Z")
    older = issue(6095, "2026-09-07T23:44:40Z")

    def run_git(args):
        assert args == ["rev-parse", "origin/main^{commit}"], args
        return CURRENT_MAIN_SHA

    gate._run_git = run_git
    gate.ensure_peer_commit = lambda peer_sha: (
        None if peer_sha == PEER_HEAD_SHA else (_ for _ in ()).throw(AssertionError(peer_sha))
    )
    observed = []

    def effective(base_sha, peer_sha):
        observed.append((base_sha, peer_sha))
        return peer_paths

    gate.effective_changed_paths = effective
    conflicts = gate.canonical_open_pr_path_conflicts(
        current,
        CURRENT_HEAD,
        [STALE_PATH],
        [peer()],
        [current, older],
        "https://api.github.test",
        REPOSITORY,
        "token",
        6101,
    )
    assert observed == [(CURRENT_MAIN_SHA, PEER_HEAD_SHA)], observed
    return conflicts


def assert_foreign_peer_fails_closed(gate):
    current = issue(6100, "2026-09-08T01:21:37Z")
    older = issue(6095, "2026-09-07T23:44:40Z")
    gate._run_git = lambda args: CURRENT_MAIN_SHA if args == ["rev-parse", "origin/main^{commit}"] else ""
    try:
        gate.canonical_open_pr_path_conflicts(
            current,
            CURRENT_HEAD,
            [STALE_PATH],
            [peer("someone/fork")],
            [current, older],
            "https://api.github.test",
            REPOSITORY,
            "token",
            6101,
        )
    except RuntimeError as exc:
        assert "not in repository" in str(exc), exc
        return
    raise AssertionError("locked foreign peer must fail closed")


def main() -> int:
    gate = load_target()
    assert_effective_peer_delta(gate)
    assert_current_delta_is_nul_safe_and_includes_deletes(gate)

    stale_conflicts = run_case(gate, [])
    assert stale_conflicts == [], "converged peer ancestry noise and main-only advancement must not collide"

    real_conflicts = run_case(gate, [STALE_PATH])
    assert real_conflicts == [(6096, PEER_HEAD, [STALE_PATH])], real_conflicts
    assert_foreign_peer_fails_closed(gate)

    print("PASS: Reservation-v2 peer/current collision uses effective protected-base deltas")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
