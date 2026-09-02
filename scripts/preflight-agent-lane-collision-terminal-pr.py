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

    if not target.pull_request_is_terminal(5284, []):
        fail("closed/non-open PR must be identified as terminal")

    open_event = event_for(5286)
    open_pr = open_event["pull_request"]
    if target.pull_request_is_terminal(5286, [open_pr]):
        fail("currently open PR must not be identified as terminal")

    open_result = target.validate_pull_request_event(open_event, repository, [open_pr])
    if not isinstance(open_result, tuple) or len(open_result) != 2:
        fail("validate_pull_request_event must preserve the lane/conflicts 2-tuple API")
    lane_key, conflicts = open_result
    if lane_key != "issue-5286" or conflicts:
        fail("open agent PR must retain branch-derived Lane-Key validation")

    print("PASS: terminal PR detection is explicit while open PR Lane-Key validation remains API-compatible")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
