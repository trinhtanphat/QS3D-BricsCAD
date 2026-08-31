#!/usr/bin/env python3
"""Hermetic regression for the agent reservation/Lane-Key collision preflight."""

from __future__ import annotations

import importlib.util
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-agent-lane-collision.py"


def load_target():
    spec = importlib.util.spec_from_file_location("agent_lane_collision_preflight", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load agent reservation collision preflight")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def expect_raises(fn, expected_fragment):
    try:
        fn()
    except Exception as exc:
        if expected_fragment not in str(exc):
            raise AssertionError(f"expected {expected_fragment!r} in {exc!r}") from exc
        return
    raise AssertionError(f"expected failure containing {expected_fragment!r}")


def issue(number, created_at, body, title="reservation", state="open"):
    return {
        "number": number,
        "created_at": created_at,
        "body": body,
        "title": title,
        "state": state,
    }


def main():
    gate = load_target()

    assert gate.normalize_lane_key("#2305") == "issue-2305"
    assert gate.normalize_lane_key("Issue: 2305") == "issue-2305"
    assert gate.normalize_lane_key("issue-2305 <!-- template hint -->") == "issue-2305"
    assert gate.normalize_lane_key("BATCH-UI-AUG17") == "batch-ui-aug17"
    expect_raises(lambda: gate.normalize_lane_key("x"), "3-81")
    expect_raises(lambda: gate.normalize_lane_key("bad/key"), "3-81")

    assert gate.extract_lane_key("Lane-Key: issue-2305\nIssue: #999") == "issue-2305"
    assert gate.extract_lane_key("Issue: #2305\n") == "issue-2305"
    assert gate.extract_lane_key("Fixes #2305") == "issue-2305"
    assert gate.extract_lane_key("No task metadata") is None
    expect_raises(
        lambda: gate.extract_lane_key("Lane-Key: issue-2305\nLane-Key: issue-2306"),
        "conflicting explicit lane keys",
    )
    assert gate.extract_lane_evidence("Lane-Key: issue-2305\nLane-Key: issue-2306") == [
        "issue-2305",
        "issue-2306",
    ]

    assert gate.branch_issue_number("agent/opaque-20260828/issue-4296-collision") == 4296
    assert gate.branch_issue_number("agent/opaque-20260828/task-no-issue") is None
    assert gate.branch_owner_token("agent/opaque-20260828/issue-4296-collision") == "opaque-20260828"
    assert gate.validate_owner_token("opaque-20260828") == "opaque-20260828"
    expect_raises(lambda: gate.validate_owner_token("c02"), "stable 6-161")
    expect_raises(lambda: gate.validate_owner_token("worker"), "generic branch owner token")
    expect_raises(lambda: gate.validate_owner_token("short"), "stable 6-161")

    assert gate.normalize_ownership_key("core.dependency-known-count") == "core.dependency-known-count"
    expect_raises(lambda: gate.normalize_ownership_key("issue-4296"), "semantic ownership")
    expect_raises(lambda: gate.normalize_ownership_key("BAD KEY"), "stable 5-121")

    paths = gate.parse_expected_paths(
        ".github/workflows/ci.yml; scripts/preflight-agent-lane-collision.py; src/QS3D.Core/"
    )
    assert paths == [
        ".github/workflows/ci.yml",
        "scripts/preflight-agent-lane-collision.py",
        "src/QS3D.Core/",
    ]
    assert gate.path_matches_claim("src/QS3D.Core/A.cs", "src/QS3D.Core/")
    assert not gate.path_matches_claim("src/QS3D.Other/A.cs", "src/QS3D.Core/")
    assert gate.path_claims_overlap("src/QS3D.Core/", "src/QS3D.Core/Domain/")
    assert gate.path_claims_overlap(".github/workflows/ci.yml", ".github/workflows/ci.yml")
    assert not gate.path_claims_overlap("src/A.cs", "src/B.cs")
    expect_raises(lambda: gate.parse_expected_paths("src/**/*.cs"), "glob syntax")

    activation = datetime(2026, 8, 28, 6, 30, tzinfo=timezone.utc)
    legacy = issue(100, "2026-08-28T06:20:00Z", "Lane-Key: issue-100")
    future = issue(101, "2026-08-28T06:31:00Z", "Lane-Key: issue-101")
    explicit = issue(
        99,
        "2026-08-28T06:20:00Z",
        "Lane-Key: issue-99\nReservation-Protocol: v2",
    )
    assert not gate.reservation_v2_required(legacy, activation)
    assert gate.reservation_v2_required(future, activation)
    assert gate.reservation_v2_required(explicit, activation)

    body = (
        "Lane-Key: issue-4296\n"
        "Reservation-Protocol: v2\n"
        "Canonical owner/session: account:owner|session:interactive-20260828-collision\n"
        "Canonical carrier: agent/interactive-20260828-collision/issue-4296-agent-reservation-collision\n"
        "Ownership-Key: repo.agent-reservation-collision-enforcement\n"
        "Expected-Paths: .github/workflows/ci.yml; scripts/preflight-agent-lane-collision.py\n"
    )
    current = issue(4296, "2026-08-28T06:32:00Z", body)
    lane, owner, ownership, expected = gate.validate_v2_issue(
        current,
        4296,
        "agent/interactive-20260828-collision/issue-4296-agent-reservation-collision",
        "interactive-20260828-collision",
    )
    assert lane == "issue-4296"
    assert "|session:" in owner
    assert ownership == "repo.agent-reservation-collision-enforcement"
    assert expected[0] == ".github/workflows/ci.yml"

    bad_carrier = {**current, "body": body.replace(
        "agent/interactive-20260828-collision/issue-4296-agent-reservation-collision",
        "agent/other/issue-4296-other",
    )}
    expect_raises(
        lambda: gate.validate_v2_issue(
            bad_carrier,
            4296,
            "agent/interactive-20260828-collision/issue-4296-agent-reservation-collision",
            "interactive-20260828-collision",
        ),
        "Canonical carrier must exactly match",
    )

    earlier_same_key = issue(
        4295,
        "2026-08-28T06:31:00Z",
        body.replace("issue-4296", "issue-4295").replace(
            "agent/interactive-20260828-collision/issue-4296-agent-reservation-collision",
            "agent/opaque-20260828/issue-4295-other",
        ),
        "earlier same semantic ownership",
    )
    conflict = gate.canonical_ownership_conflict(
        current,
        "repo.agent-reservation-collision-enforcement",
        [current, earlier_same_key],
        activation,
    )
    assert conflict == (4295, "earlier same semantic ownership")
    assert gate.canonical_ownership_conflict(
        earlier_same_key,
        "repo.agent-reservation-collision-enforcement",
        [current, earlier_same_key],
        activation,
    ) is None

    earlier_path = issue(
        4294,
        "2026-08-28T06:30:30Z",
        (
            "Lane-Key: issue-4294\nReservation-Protocol: v2\n"
            "Canonical owner/session: account:owner|session:opaque-earlier\n"
            "Canonical carrier: agent/opaque-earlier/issue-4294-path\n"
            "Ownership-Key: repo.other-semantic-key\n"
            "Expected-Paths: scripts/\n"
        ),
    )
    path_conflict = gate.canonical_expected_path_conflict(
        current,
        ["scripts/preflight-agent-lane-collision.py"],
        [current, earlier_path],
        activation,
    )
    assert path_conflict is not None and path_conflict[0] == 4294

    assert gate.requires_lane_lock(
        "agent/opaque-20260828/issue-1-task",
        "trinhtanphat/QS3D-BricsCAD",
        "trinhtanphat/QS3D-BricsCAD",
        "trinhtanphat",
    )
    assert gate.requires_lane_lock(
        "integration/batch-a",
        "trinhtanphat/QS3D-BricsCAD",
        "trinhtanphat/QS3D-BricsCAD",
        "trinhtanphat",
    )
    assert not gate.requires_lane_lock(
        "agent/dependabot/test",
        "trinhtanphat/QS3D-BricsCAD",
        "trinhtanphat/QS3D-BricsCAD",
        "dependabot[bot]",
    )

    peers = [
        {"number": 10, "body": "Lane-Key: issue-2305", "head": {"ref": "agent/a/task"}},
        {"number": 11, "body": "Issue: #2306", "head": {"ref": "agent/b/task"}},
        {"number": 12, "body": "Fixes #2305", "head": {"ref": "agent/c/task"}},
        {"number": 14, "body": "Lane-Key: issue-2305\nLane-Key: issue-9999", "head": {"ref": "agent/bad"}},
    ]
    assert gate.find_duplicate_carriers(99, "issue-2305", peers) == [
        (10, "agent/a/task"),
        (12, "agent/c/task"),
        (14, "agent/bad"),
    ]

    event = {
        "number": 99,
        "sender": {"login": "trinhtanphat"},
        "pull_request": {
            "number": 99,
            "body": "Lane-Key: issue-2305\nCanonical carrier: agent/opaque/task",
            "head": {
                "ref": "agent/opaque/task",
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
        },
    }
    key, conflicts = gate.validate_pull_request_event(
        event, "trinhtanphat/QS3D-BricsCAD", peers
    )
    assert key == "issue-2305"
    assert conflicts == [
        (10, "agent/a/task"),
        (12, "agent/c/task"),
        (14, "agent/bad"),
    ]

    inferred_event = {
        "number": 100,
        "sender": {"login": "trinhtanphat"},
        "pull_request": {
            "number": 100,
            "body": "## Summary\nNo duplicated lane metadata here.",
            "head": {
                "ref": "agent/opaque-20260828/issue-2305-infer-lane",
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
        },
    }
    key, conflicts = gate.validate_pull_request_event(
        inferred_event, "trinhtanphat/QS3D-BricsCAD", peers
    )
    assert key == "issue-2305"
    assert conflicts == [
        (10, "agent/a/task"),
        (12, "agent/c/task"),
        (14, "agent/bad"),
    ]

    mismatched_event = {
        **inferred_event,
        "pull_request": {
            **inferred_event["pull_request"],
            "body": "Lane-Key: issue-9999",
        },
    }
    expect_raises(
        lambda: gate.validate_pull_request_event(
            mismatched_event, "trinhtanphat/QS3D-BricsCAD", peers
        ),
        "does not match branch-derived Lane-Key 'issue-2305'",
    )

    integration_missing_event = {
        "number": 101,
        "sender": {"login": "trinhtanphat"},
        "pull_request": {
            "number": 101,
            "body": "## Summary\nIntegration batch without explicit lane metadata.",
            "head": {
                "ref": "integration/batch-a",
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
        },
    }
    expect_raises(
        lambda: gate.validate_pull_request_event(
            integration_missing_event, "trinhtanphat/QS3D-BricsCAD", peers
        ),
        "requires a Lane-Key in the PR body",
    )

    print("PASS: agent reservation/Lane-Key collision preflight regression")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())