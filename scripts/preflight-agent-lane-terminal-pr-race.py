#!/usr/bin/env python3
"""Regression: queued PR validation must ignore a carrier that is already closed."""

from __future__ import annotations

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-agent-lane-collision.py"


def load_target():
    spec = importlib.util.spec_from_file_location("agent_lane_collision_preflight_terminal_race", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load agent reservation collision preflight")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    gate = load_target()
    repository = "trinhtanphat/QS3D-BricsCAD"
    head_ref = "agent/session-terminal-race/issue-4997-mcp-capability-lanes"
    event = {
        "action": "edited",
        "number": 5021,
        "sender": {"login": "trinhtanphat"},
        "pull_request": {
            "number": 5021,
            "state": "open",
            "body": "Lane-Key: issue-4997",
            "head": {
                "ref": head_ref,
                "repo": {"full_name": repository},
            },
        },
    }

    # The event snapshot can say open even though the queued run starts only after
    # the PR has been closed. Current open-PR state is authoritative for mergeability.
    lane_key, conflicts = gate.validate_pull_request_event(event, repository, [])
    assert lane_key is None, (
        "terminal queued PR must be ignored once it is absent from the current open-PR set; "
        f"got lane_key={lane_key!r}"
    )
    assert conflicts == []

    # Reopened/currently-open carrier must immediately return to normal fail-closed validation.
    current_pr = {
        "number": 5021,
        "body": "Lane-Key: issue-4997",
        "head": {"ref": head_ref, "repo": {"full_name": repository}},
    }
    lane_key, conflicts = gate.validate_pull_request_event(event, repository, [current_pr])
    assert lane_key == "issue-4997"
    assert conflicts == []

    print("PASS: terminal queued PR reservation race regression")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
