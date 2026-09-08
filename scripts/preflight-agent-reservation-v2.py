#!/usr/bin/env python3
"""Focused regression for Reservation-v2 peer path collision against current protected main."""

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


def peer() -> dict:
    return {
        "number": 6096,
        "created_at": "2026-09-07T23:44:46Z",
        "head": {
            "ref": PEER_HEAD,
            "sha": PEER_HEAD_SHA,
            "repo": {"full_name": REPOSITORY},
        },
    }


def run_case(gate, path_is_effective: bool):
    current = issue(6100, "2026-09-08T01:21:37Z")
    older = issue(6095, "2026-09-07T23:44:40Z")

    gate.fetch_pr_files = lambda *_args, **_kwargs: [STALE_PATH]
    gate.fetch_branch_head_sha = lambda *_args, **_kwargs: CURRENT_MAIN_SHA
    gate._run_git = lambda _args: (_ for _ in ()).throw(
        AssertionError("peer collision must not bind protected-main identity from a stale local ref")
    )
    observed = []

    def path_changed(api_url, repository, current_main_sha, peer_head_sha, path, token):
        observed.append((current_main_sha, peer_head_sha, path))
        return path_is_effective

    gate.path_changed_between_commits = path_changed
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
    assert observed == [(CURRENT_MAIN_SHA, PEER_HEAD_SHA, STALE_PATH)], observed
    return conflicts


def main() -> int:
    gate = load_target()

    stale_conflicts = run_case(gate, path_is_effective=False)
    assert stale_conflicts == [], (
        "peer PR-file ancestry noise must not collide when the peer head has the same "
        "path identity as current protected main"
    )

    real_conflicts = run_case(gate, path_is_effective=True)
    assert real_conflicts == [(6096, PEER_HEAD, [STALE_PATH])], real_conflicts

    print("PASS: Reservation-v2 peer path collision uses effective current-main delta")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
