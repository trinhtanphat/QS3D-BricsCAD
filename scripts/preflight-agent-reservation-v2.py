#!/usr/bin/env python3
"""Focused regression for Reservation-v2 peer collision against exact current-base tree state."""

from __future__ import annotations

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-agent-lane-collision.py"
REPOSITORY = "trinhtanphat/QS3D-BricsCAD"
CURRENT_HEAD = "agent/gpt56sol-20260908-c05-reservation-peer-current-delta/issue-6100-reservation-peer-current-delta"
PEER_HEAD = "agent/gpt56sol-20260908-c05-v25-final-publish-main-stability/issue-6095-v25-final-publish-main-stability"
STALE_PATH = "scripts/publish-v26-release.ps1"
CURRENT_MAIN_SHA = "a" * 40
PEER_HEAD_SHA = "b" * 40


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


def assert_exact_peer_delta(gate):
    calls = []
    weird = "scripts/name with trailing space "

    def run_git_exact(args):
        calls.append(args)
        return f"{STALE_PATH}\x00{weird}\x00"

    gate._run_git_exact = run_git_exact
    paths = gate.effective_changed_paths(CURRENT_MAIN_SHA, PEER_HEAD_SHA)
    assert paths == [STALE_PATH, weird], paths
    assert calls == [[
        "diff",
        "--name-only",
        "-z",
        "--no-renames",
        "--diff-filter=ACDMRTUXB",
        CURRENT_MAIN_SHA,
        PEER_HEAD_SHA,
        "--",
    ]], calls


def assert_current_delta_is_nul_safe_and_includes_deletes(gate):
    calls = []
    deleted = "scripts/deleted file.ps1"

    def run_git_exact(args):
        calls.append(args)
        return deleted + "\x00"

    gate._run_git_exact = run_git_exact
    paths = gate.current_changed_paths("main")
    assert paths == [deleted], paths
    assert calls == [[
        "diff",
        "--name-only",
        "-z",
        "--no-renames",
        "--diff-filter=ACDMRTUXB",
        "origin/main...HEAD",
        "--",
    ]], calls


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
    assert_exact_peer_delta(gate)
    assert_current_delta_is_nul_safe_and_includes_deletes(gate)

    stale_conflicts = run_case(gate, [])
    assert stale_conflicts == [], "peer ancestry noise absent from exact current-base tree delta must not collide"

    real_conflicts = run_case(gate, [STALE_PATH])
    assert real_conflicts == [(6096, PEER_HEAD, [STALE_PATH])], real_conflicts
    assert_foreign_peer_fails_closed(gate)

    print("PASS: Reservation-v2 peer collision uses exact NUL-safe current-base tree delta")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
