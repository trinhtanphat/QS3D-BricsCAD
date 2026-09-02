#!/usr/bin/env python3
"""Hermetic regression for reservation-v2 terminal pull-request bypass."""

from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-agent-lane-collision.py"


def fail(message: str) -> None:
    print("ERROR: terminal PR reservation regression failed: " + message, file=sys.stderr)
    raise SystemExit(1)


def load_target():
    spec = spec_from_file_location("qs3d_agent_lane_collision", TARGET)
    if spec is None or spec.loader is None:
        fail("cannot load preflight-agent-lane-collision.py")
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def event_for(number: int) -> dict:
    branch = f"agent/regression-session/issue-{number}-terminal-pr"
    return {
        "number": number,
        "sender": {"login": "trinhtanphat"},
        "pull_request": {
            "number": number,
            "body": "",
            "head": {
                "ref": branch,
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
        },
    }


def main() -> int:
    target = load_target()
    repository = "trinhtanphat/QS3D-BricsCAD"

    terminal_result = target.validate_pull_request_event(event_for(5284), repository, [])
    if not isinstance(terminal_result, tuple) or len(terminal_result) != 3:
        fail("validate_pull_request_event must return lane/conflicts/terminal state")
    lane_key, conflicts, terminal = terminal_result
    if lane_key is not None or conflicts or terminal is not True:
        fail("closed/non-open PR must be identified as terminal without lane conflicts")

    open_event = event_for(5286)
    open_pr = open_event["pull_request"]
    open_result = target.validate_pull_request_event(open_event, repository, [open_pr])
    if not isinstance(open_result, tuple) or len(open_result) != 3:
        fail("open PR validation must also return explicit terminal state")
    lane_key, conflicts, terminal = open_result
    if lane_key != "issue-5286" or conflicts or terminal is not False:
        fail("open agent PR must retain branch-derived Lane-Key validation")

    print("PASS: terminal PR state is explicit while open PR Lane-Key validation remains active")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
