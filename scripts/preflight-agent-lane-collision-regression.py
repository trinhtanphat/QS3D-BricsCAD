#!/usr/bin/env python3
"""Hermetic regression for the agent Lane-Key collision preflight."""

from __future__ import annotations

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-agent-lane-collision.py"


def load_target():
    spec = importlib.util.spec_from_file_location("agent_lane_collision_preflight", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load Lane-Key collision preflight")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def expect_raises(fn, expected_fragment):
    try:
        fn()
    except Exception as exc:  # regression intentionally checks controlled validation failures
        if expected_fragment not in str(exc):
            raise AssertionError(f"expected {expected_fragment!r} in {exc!r}") from exc
        return
    raise AssertionError(f"expected failure containing {expected_fragment!r}")


def main():
    gate = load_target()

    assert gate.normalize_lane_key("#2305") == "issue-2305"
    assert gate.normalize_lane_key("Issue: 2305") == "issue-2305"
    assert gate.normalize_lane_key("BATCH-UI-AUG17") == "batch-ui-aug17"
    expect_raises(lambda: gate.normalize_lane_key("x"), "3-81")
    expect_raises(lambda: gate.normalize_lane_key("bad/key"), "3-81")

    assert gate.extract_lane_key("Lane-Key: issue-2305\nIssue: #999") == "issue-2305"
    assert gate.extract_lane_key("Issue: #2305\n") == "issue-2305"
    assert gate.extract_lane_key("Fixes #2305") == "issue-2305"
    assert gate.extract_lane_key("Fixes #2305 and closes #2305") == "issue-2305"
    assert gate.extract_lane_key("No task metadata") is None
    expect_raises(
        lambda: gate.extract_lane_key("Lane-Key: issue-2305\nLane-Key: issue-2306"),
        "conflicting explicit lane keys",
    )
    expect_raises(
        lambda: gate.extract_lane_key("Fixes #2305\nCloses #2306"),
        "conflicting closing reference lane keys",
    )

    assert gate.requires_lane_lock(
        "agent/chatgpt/task-2305", "trinhtanphat/QS3D-BricsCAD", "trinhtanphat/QS3D-BricsCAD", "trinhtanphat"
    )
    assert gate.requires_lane_lock(
        "integration/batch-a", "trinhtanphat/QS3D-BricsCAD", "trinhtanphat/QS3D-BricsCAD", "trinhtanphat"
    )
    assert not gate.requires_lane_lock(
        "agent/dependabot/test", "trinhtanphat/QS3D-BricsCAD", "trinhtanphat/QS3D-BricsCAD", "dependabot[bot]"
    )
    assert not gate.requires_lane_lock(
        "feature/human", "trinhtanphat/QS3D-BricsCAD", "trinhtanphat/QS3D-BricsCAD", "trinhtanphat"
    )
    assert not gate.requires_lane_lock(
        "agent/fork/test", "fork/QS3D-BricsCAD", "trinhtanphat/QS3D-BricsCAD", "someone"
    )

    peers = [
        {"number": 10, "body": "Lane-Key: issue-2305", "head": {"ref": "agent/a/task"}},
        {"number": 11, "body": "Issue: #2306", "head": {"ref": "agent/b/task"}},
        {"number": 12, "body": "Fixes #2305", "head": {"ref": "agent/c/task"}},
        {"number": 13, "body": "Lane-Key: issue-9999\nLane-Key: issue-8888", "head": {"ref": "agent/bad"}},
    ]
    assert gate.find_duplicate_carriers(99, "issue-2305", peers) == [
        (10, "agent/a/task"),
        (12, "agent/c/task"),
    ]
    assert gate.find_duplicate_carriers(10, "issue-2305", peers) == [(12, "agent/c/task")]
    assert gate.find_duplicate_carriers(99, "issue-7777", peers) == []

    event = {
        "number": 99,
        "sender": {"login": "trinhtanphat"},
        "pull_request": {
            "number": 99,
            "body": "Lane-Key: issue-2305\nCanonical carrier: agent/chatgpt/task-2305",
            "head": {
                "ref": "agent/chatgpt/task-2305",
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
        },
    }
    key, conflicts = gate.validate_pull_request_event(event, "trinhtanphat/QS3D-BricsCAD", peers)
    assert key == "issue-2305"
    assert conflicts == [(10, "agent/a/task"), (12, "agent/c/task")]

    missing = {
        **event,
        "pull_request": {**event["pull_request"], "body": "no lane metadata"},
    }
    expect_raises(
        lambda: gate.validate_pull_request_event(missing, "trinhtanphat/QS3D-BricsCAD", []),
        "requires a Lane-Key",
    )

    fork_event = {
        **event,
        "pull_request": {
            **event["pull_request"],
            "body": None,
            "head": {"ref": "agent/fork/task", "repo": {"full_name": "fork/QS3D-BricsCAD"}},
        },
    }
    assert gate.validate_pull_request_event(fork_event, "trinhtanphat/QS3D-BricsCAD", peers) == (None, [])

    print("PASS: agent Lane-Key collision preflight regression")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
