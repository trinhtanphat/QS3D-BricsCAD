#!/usr/bin/env python3
"""Hermetic regression coverage for PR branch-CI admission payload hardening."""

from __future__ import annotations

import importlib.util
import io
import json
from pathlib import Path
import sys
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-pr-branch-ci-gate.py"


def load_target():
    spec = importlib.util.spec_from_file_location("qs3d_pr_branch_ci_gate", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load PR branch-CI gate")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def canonical_event() -> dict[str, Any]:
    return {
        "action": "opened",
        "pull_request": {
            "created_at": "2026-08-17T01:00:00Z",
            "user": {"login": "trinhtanphat"},
            "head": {
                "ref": "agent/test",
                "sha": "a" * 40,
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
            "base": {
                "ref": "main",
                "sha": "b" * 40,
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
        },
    }


def expect_runtime_error(label: str, callback) -> None:
    try:
        callback()
    except RuntimeError:
        return
    except Exception as exc:  # pragma: no cover - diagnostic path
        raise AssertionError(f"{label}: wrong exception type {type(exc).__name__}: {exc}") from exc
    raise AssertionError(f"{label}: expected RuntimeError")


def replace_path(root: dict[str, Any], path: tuple[str, ...], value: Any) -> dict[str, Any]:
    clone = json.loads(json.dumps(root))
    cursor: dict[str, Any] = clone
    for token in path[:-1]:
        cursor = cursor[token]
    cursor[path[-1]] = value
    return clone


class FakeResponse:
    def __init__(self, payload: bytes):
        self._stream = io.BytesIO(payload)

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc, tb):
        return False

    def read(self, *args, **kwargs):
        return self._stream.read(*args, **kwargs)


def main() -> int:
    gate = load_target()
    errors: list[str] = []

    def check(condition: bool, message: str) -> None:
        if not condition:
            errors.append(message)

    canonical = canonical_event()
    try:
        context = gate.parse_pr_context(canonical, "trinhtanphat/QS3D-BricsCAD")
        check(context["author"] == "trinhtanphat", "canonical author must parse")
        check(context["head_ref"] == "agent/test", "canonical head ref must parse")
        check(context["head_sha"] == "a" * 40, "canonical head SHA must parse")
    except Exception as exc:
        errors.append(f"canonical PR context unexpectedly failed: {exc}")

    malformed_paths = (
        (("pull_request",), []),
        (("pull_request", "user"), "trinhtanphat"),
        (("pull_request", "user", "login"), ["trinhtanphat"]),
        (("pull_request", "head"), "agent/test"),
        (("pull_request", "head", "repo"), []),
        (("pull_request", "head", "repo", "full_name"), 7),
        (("pull_request", "base"), []),
        (("pull_request", "base", "repo"), "repo"),
        (("pull_request", "base", "repo", "full_name"), {}),
        (("pull_request", "head", "ref"), True),
        (("pull_request", "head", "sha"), "short"),
        (("pull_request", "head", "sha"), True),
        (("pull_request", "created_at"), []),
        (("pull_request", "created_at"), "not-a-time"),
    )
    for path, value in malformed_paths:
        candidate = replace_path(canonical, path, value)
        try:
            expect_runtime_error(
                "malformed " + ".".join(path),
                lambda candidate=candidate: gate.parse_pr_context(candidate, "trinhtanphat/QS3D-BricsCAD"),
            )
        except AssertionError as exc:
            errors.append(str(exc))

    dependabot = canonical_event()
    dependabot["pull_request"]["user"]["login"] = "dependabot[bot]"
    try:
        context = gate.parse_pr_context(dependabot, "trinhtanphat/QS3D-BricsCAD")
        check(context["author"] == gate.DEPENDABOT_LOGIN, "Dependabot identity must remain parseable")
    except Exception as exc:
        errors.append(f"Dependabot boundary unexpectedly failed: {exc}")

    fork = canonical_event()
    fork["pull_request"]["head"]["repo"]["full_name"] = "external/fork"
    try:
        context = gate.parse_pr_context(fork, "trinhtanphat/QS3D-BricsCAD")
        check(context["head_repo"] != context["base_repo"], "fork boundary must remain distinguishable")
    except Exception as exc:
        errors.append(f"fork boundary unexpectedly failed: {exc}")

    opened = gate.parse_github_time("2026-08-17T01:00:00Z")
    valid_run = {
        "event": "push",
        "head_branch": "agent/test",
        "head_sha": "a" * 40,
        "status": "completed",
        "conclusion": "success",
        "run_attempt": 1,
        "path": ".github/workflows/ci.yml",
        "created_at": "2026-08-17T00:55:00Z",
        "updated_at": "2026-08-17T00:59:00Z",
    }
    check(gate.run_qualifies(valid_run, "agent/test", "a" * 40, opened), "canonical branch run must qualify")
    for invalid_attempt in (True, False, 0, -1, "1", 1.0, [], {}):
        candidate = dict(valid_run)
        candidate["run_attempt"] = invalid_attempt
        check(
            not gate.run_qualifies(candidate, "agent/test", "a" * 40, opened),
            f"malformed run_attempt must not qualify: {invalid_attempt!r}",
        )

    original_urlopen = gate.urlopen
    try:
        gate.urlopen = lambda request, timeout=20: FakeResponse(b'{"workflow_runs":[{"id":1}]}')
        runs = gate.fetch_runs("trinhtanphat/QS3D-BricsCAD", "a" * 40, "token")
        check(len(runs) == 1 and runs[0]["id"] == 1, "canonical Actions payload must parse")

        gate.urlopen = lambda request, timeout=20: FakeResponse(b"not-json")
        try:
            expect_runtime_error(
                "malformed Actions JSON",
                lambda: gate.fetch_runs("trinhtanphat/QS3D-BricsCAD", "a" * 40, "token"),
            )
        except AssertionError as exc:
            errors.append(str(exc))

        gate.urlopen = lambda request, timeout=20: FakeResponse(b"[]")
        try:
            expect_runtime_error(
                "non-object Actions payload",
                lambda: gate.fetch_runs("trinhtanphat/QS3D-BricsCAD", "a" * 40, "token"),
            )
        except AssertionError as exc:
            errors.append(str(exc))

        gate.urlopen = lambda request, timeout=20: FakeResponse(b'{"workflow_runs":{}}')
        try:
            expect_runtime_error(
                "non-list workflow_runs",
                lambda: gate.fetch_runs("trinhtanphat/QS3D-BricsCAD", "a" * 40, "token"),
            )
        except AssertionError as exc:
            errors.append(str(exc))

        gate.urlopen = lambda request, timeout=20: FakeResponse(b'{"workflow_runs":[null]}')
        try:
            expect_runtime_error(
                "non-object workflow run",
                lambda: gate.fetch_runs("trinhtanphat/QS3D-BricsCAD", "a" * 40, "token"),
            )
        except AssertionError as exc:
            errors.append(str(exc))

        oversized = {"workflow_runs": [{"id": index} for index in range(gate.MAX_WORKFLOW_RUNS + 1)]}
        oversized_bytes = json.dumps(oversized).encode("utf-8")
        gate.urlopen = lambda request, timeout=20: FakeResponse(oversized_bytes)
        try:
            expect_runtime_error(
                "oversized workflow run response",
                lambda: gate.fetch_runs("trinhtanphat/QS3D-BricsCAD", "a" * 40, "token"),
            )
        except AssertionError as exc:
            errors.append(str(exc))
    finally:
        gate.urlopen = original_urlopen

    static_errors = gate.validate_self_tests() + gate.validate_static_contract()
    errors.extend(f"target self/static contract: {error}" for error in static_errors)

    if errors:
        print("PR branch-CI admission regression FAILED:")
        for error in errors:
            print(f" - {error}")
        return 1

    print(
        "PASS: PR branch-CI admission gate fails closed on malformed nested PR/run/API payloads, "
        "retains valid internal/fork/Dependabot boundaries, and bounds Actions response materialization."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
