#!/usr/bin/env python3
"""Hermetic regression coverage for the PR branch-CI admission gate."""

from __future__ import annotations

from copy import deepcopy
from datetime import datetime, timezone
import importlib.util
import os
from pathlib import Path
import sys
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-pr-branch-ci-gate.py"


def fail(message: str) -> int:
    print(f"ERROR: {message}", file=sys.stderr)
    return 1


def load_target():
    spec = importlib.util.spec_from_file_location("qs3d_pr_branch_ci_gate", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load PR branch-CI gate module")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def expect_value_error(callback, label: str) -> list[str]:
    try:
        callback()
    except ValueError:
        return []
    except Exception as exc:  # pragma: no cover - converted into deterministic regression failure
        return [f"{label}: expected ValueError, got {type(exc).__name__}: {exc}"]
    return [f"{label}: expected ValueError"]


def base_event() -> dict[str, Any]:
    return {
        "action": "opened",
        "pull_request": {
            "created_at": "2026-08-16T10:00:00Z",
            "user": {"login": "trinhtanphat"},
            "head": {
                "ref": "agent/test",
                "sha": "a" * 40,
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
            "base": {"repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"}},
        },
    }


def main() -> int:
    gate = load_target()
    errors: list[str] = []

    self_test_errors = gate.validate_self_tests()
    if self_test_errors:
        errors.append(f"target self-tests failed: {self_test_errors}")

    event = base_event()
    context = gate.extract_pr_context(event, "trinhtanphat/QS3D-BricsCAD")
    if context["dependabot"] or context["external"]:
        errors.append("ordinary internal PR must be subject to admission validation")
    if context["head_ref"] != "agent/test" or context["head_sha"] != "a" * 40:
        errors.append("internal PR context must preserve exact head identity")

    fork_event = deepcopy(event)
    fork_event["pull_request"]["head"]["repo"]["full_name"] = "external/fork"
    if not gate.extract_pr_context(fork_event, "trinhtanphat/QS3D-BricsCAD")["external"]:
        errors.append("fork PR must be classified external")

    dependabot_event = deepcopy(event)
    dependabot_event["pull_request"]["user"]["login"] = "dependabot[bot]"
    if not gate.extract_pr_context(dependabot_event, "trinhtanphat/QS3D-BricsCAD")["dependabot"]:
        errors.append("Dependabot standing exception must remain explicit")

    malformed_paths = (
        ("pull_request", []),
        ("pull_request.user", "bot"),
        ("pull_request.head", []),
        ("pull_request.base", "main"),
        ("pull_request.head.repo", []),
        ("pull_request.base.repo", []),
        ("pull_request.head.ref", 123),
        ("pull_request.head.sha", {"sha": "a" * 40}),
        ("pull_request.created_at", ["2026-08-16T10:00:00Z"]),
    )
    for path, replacement in malformed_paths:
        candidate = deepcopy(event)
        parts = path.split(".")
        cursor: Any = candidate
        for part in parts[:-1]:
            cursor = cursor[part]
        cursor[parts[-1]] = replacement
        errors.extend(
            expect_value_error(
                lambda candidate=candidate: gate.extract_pr_context(candidate, "trinhtanphat/QS3D-BricsCAD"),
                f"malformed {path}",
            )
        )

    for payload in (None, [], {}, {"workflow_runs": {}}, {"workflow_runs": [None]}):
        errors.extend(
            expect_value_error(
                lambda payload=payload: gate.parse_workflow_runs_payload(payload),
                f"malformed workflow payload {payload!r}",
            )
        )

    parsed_runs = gate.parse_workflow_runs_payload({"workflow_runs": [{"id": 1}]})
    if parsed_runs != [{"id": 1}]:
        errors.append("valid workflow_runs payload must be preserved")

    opened = datetime(2026, 8, 16, 10, 0, tzinfo=timezone.utc)
    qualifying_run = {
        "event": "push",
        "head_branch": "agent/test",
        "head_sha": "a" * 40,
        "status": "completed",
        "conclusion": "success",
        "run_attempt": 1,
        "path": ".github/workflows/ci.yml",
        "created_at": "2026-08-16T09:50:00Z",
        "updated_at": "2026-08-16T09:59:00Z",
    }
    if not gate.run_qualifies(qualifying_run, "agent/test", "a" * 40, opened):
        errors.append("valid attempt-1 exact-head pre-PR run must qualify")
    for malformed_attempt in (True, 0, 2, "2", "abc", {}, []):
        candidate = dict(qualifying_run)
        candidate["run_attempt"] = malformed_attempt
        try:
            result = gate.run_qualifies(candidate, "agent/test", "a" * 40, opened)
        except Exception as exc:
            errors.append(f"run_attempt={malformed_attempt!r} raised {type(exc).__name__}: {exc}")
        else:
            if result:
                errors.append(f"run_attempt={malformed_attempt!r} must fail closed")

    original_urlopen = gate.urlopen
    original_runtime = os.environ.pop(gate.RUNTIME_ENV, None)
    try:
        def unexpected_network(*args, **kwargs):
            raise AssertionError("aggregate/static invocation attempted network access")

        gate.urlopen = unexpected_network
        try:
            static_result = gate.main()
        except AssertionError as exc:
            errors.append(str(exc))
        else:
            if static_result != 0:
                errors.append(f"aggregate/static invocation returned {static_result}, expected 0")
    finally:
        gate.urlopen = original_urlopen
        if original_runtime is not None:
            os.environ[gate.RUNTIME_ENV] = original_runtime

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("PR branch CI admission regression: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
