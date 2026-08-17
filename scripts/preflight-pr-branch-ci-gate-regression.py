#!/usr/bin/env python3
from contextlib import redirect_stderr, redirect_stdout
from datetime import datetime, timezone
from io import StringIO
import importlib.util
import json
import os
from pathlib import Path
import tempfile
from unittest import mock
from urllib.error import URLError

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-pr-branch-ci-gate.py"


def load_target():
    spec = importlib.util.spec_from_file_location("qs3d_pr_branch_ci_gate", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("unable to load branch CI admission gate")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def expect_runtime(callable_obj, contains):
    try:
        callable_obj()
    except RuntimeError as exc:
        check(contains in str(exc), f"unexpected RuntimeError: {exc}")
        return
    raise AssertionError(f"expected RuntimeError containing {contains!r}")


def valid_event():
    return {
        "action": "opened",
        "pull_request": {
            "user": {"login": "trinhtanphat"},
            "head": {
                "ref": "agent/worker/fix",
                "sha": "a" * 40,
                "repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"},
            },
            "base": {"repo": {"full_name": "trinhtanphat/QS3D-BricsCAD"}},
            "created_at": "2026-08-16T10:00:00Z",
        },
    }


def payload_regressions(gate):
    qualified = gate.qualify_pr_payload(valid_event(), "trinhtanphat/QS3D-BricsCAD")
    check(qualified is not None, "valid internal PR must qualify for evidence lookup")
    head_ref, head_sha, base_repo, created_at = qualified
    check(head_ref == "agent/worker/fix", "head ref drifted")
    check(head_sha == "a" * 40, "head SHA drifted")
    check(base_repo == "trinhtanphat/QS3D-BricsCAD", "base repo drifted")
    check(created_at == datetime(2026, 8, 16, 10, tzinfo=timezone.utc), "creation timestamp drifted")

    offset_event = valid_event()
    offset_event["pull_request"]["created_at"] = "2026-08-16T12:00:00+02:00"
    offset_qualified = gate.qualify_pr_payload(offset_event, "trinhtanphat/QS3D-BricsCAD")
    check(offset_qualified is not None, "offset-aware GitHub timestamp must remain valid")
    check(
        offset_qualified[3] == datetime(2026, 8, 16, 10, tzinfo=timezone.utc),
        "offset-aware GitHub timestamp must normalize to UTC",
    )

    mutations = (
        ("pull_request payload", {"pull_request": []}),
        ("pull_request.user", {"pull_request.user": "owner"}),
        ("pull_request.head", {"pull_request.head": []}),
        ("pull_request.base", {"pull_request.base": "main"}),
        ("pull_request.head.repo", {"pull_request.head.repo": []}),
        ("pull_request.base.repo", {"pull_request.base.repo": []}),
    )
    for label, mutation in mutations:
        event = valid_event()
        path, value = next(iter(mutation.items()))
        parts = path.split(".")
        target = event
        for part in parts[:-1]:
            target = target[part]
        target[parts[-1]] = value
        expect_runtime(lambda event=event, label=label: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"), label)

    canonicality_mutations = (
        ("pull_request.user.login", " trinhtanphat ", "canonical"),
        ("pull_request.head.ref", " agent/worker/fix", "canonical"),
        ("pull_request.head.sha", "a" * 40 + " ", "canonical"),
        ("pull_request.head.repo.full_name", "trinhtanphat/QS3D-BricsCAD ", "canonical"),
        ("pull_request.base.repo.full_name", " trinhtanphat/QS3D-BricsCAD", "canonical"),
        ("pull_request.created_at", " 2026-08-16T10:00:00Z", "canonical"),
        ("pull_request.created_at", "2026-08-16T10:00:00", "explicit timezone"),
    )
    for path, value, message in canonicality_mutations:
        event = valid_event()
        target = event
        parts = path.split(".")
        for part in parts[:-1]:
            target = target[part]
        target[parts[-1]] = value
        expect_runtime(lambda event=event, message=message: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"), message)

    event = valid_event()
    event["pull_request"]["head"]["sha"] = "not-a-sha"
    expect_runtime(lambda: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"), "invalid PR head SHA")

    event = valid_event()
    event["pull_request"]["created_at"] = []
    expect_runtime(lambda: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"), "timestamp")

    event = valid_event()
    event["pull_request"]["head"]["ref"] = "fix/not-watched"
    expect_runtime(lambda: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"), "outside automatic branch-CI namespaces")

    event = valid_event()
    event["pull_request"]["user"]["login"] = gate.DEPENDABOT_LOGIN
    check(gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD") is None, "Dependabot exception regressed")

    event = valid_event()
    event["pull_request"]["head"]["repo"]["full_name"] = "external/fork"
    check(gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD") is None, "fork exception regressed")


def run_regressions(gate):
    opened = gate.parse_github_time("2026-08-16T10:00:00Z")
    base = {
        "event": "push",
        "head_branch": "agent/worker/fix",
        "head_sha": "a" * 40,
        "status": "completed",
        "conclusion": "success",
        "run_attempt": 1,
        "path": ".github/workflows/ci.yml",
        "created_at": "2026-08-16T09:50:00Z",
        "updated_at": "2026-08-16T09:59:00Z",
    }
    check(gate.run_qualifies(base, "agent/worker/fix", "a" * 40, opened), "valid attempt-1 evidence must qualify")

    offset = dict(base)
    offset["created_at"] = "2026-08-16T11:50:00+02:00"
    offset["updated_at"] = "2026-08-16T11:59:00+02:00"
    check(gate.run_qualifies(offset, "agent/worker/fix", "a" * 40, opened), "aware offset timestamps must qualify")

    for malformed in (True, False, 0, -1, "", "1.0", "abc", [], {}):
        candidate = dict(base)
        candidate["run_attempt"] = malformed
        check(not gate.run_qualifies(candidate, "agent/worker/fix", "a" * 40, opened),
              f"malformed run_attempt must fail closed: {malformed!r}")

    string_attempt = dict(base)
    string_attempt["run_attempt"] = "1"
    check(gate.run_qualifies(string_attempt, "agent/worker/fix", "a" * 40, opened), "canonical string attempt 1 should qualify")

    timestamp_failures = (
        ("created_at", []),
        ("updated_at", {}),
        ("updated_at", "not-a-time"),
        ("created_at", "2026-08-16T09:50:00"),
        ("updated_at", "2026-08-16T09:59:00"),
        ("created_at", "2026-08-16T09:50:00Z "),
    )
    for field, malformed in timestamp_failures:
        candidate = dict(base)
        candidate[field] = malformed
        check(not gate.run_qualifies(candidate, "agent/worker/fix", "a" * 40, opened),
              f"malformed/noncanonical {field} must fail closed")


def event_file_regressions(gate):
    with tempfile.TemporaryDirectory() as temp_dir:
        path = Path(temp_dir) / "event.json"
        with mock.patch.dict(os.environ, {"GITHUB_EVENT_PATH": str(path)}, clear=False):
            path.write_text("{not-json", encoding="utf-8")
            expect_runtime(gate.read_event, "could not read GitHub event payload")
            path.write_text("[]", encoding="utf-8")
            expect_runtime(gate.read_event, "GitHub event payload must be a JSON object")
            path.write_text(json.dumps(valid_event()), encoding="utf-8")
            loaded = gate.read_event()
            check(loaded["action"] == "opened", "valid event JSON must load")


def fetch_regressions(gate):
    class FakeResponse:
        def __init__(self, body):
            self.body = body
        def __enter__(self):
            return self
        def __exit__(self, exc_type, exc, tb):
            return False
        def read(self, *args, **kwargs):
            return self.body

    with mock.patch.object(gate, "urlopen", return_value=FakeResponse(b"not-json")):
        expect_runtime(lambda: gate.fetch_runs("o/r", "a" * 40, "token"), "unreadable JSON")

    for payload, contains in (([], "lookup payload must be a JSON object"), ({}, "invalid workflow_runs payload"), ({"workflow_runs": ["bad"]}, "workflow_runs[0]")):
        body = json.dumps(payload).encode("utf-8")
        with mock.patch.object(gate, "urlopen", return_value=FakeResponse(body)):
            expect_runtime(lambda: gate.fetch_runs("o/r", "a" * 40, "token"), contains)

    too_many = {"workflow_runs": [{} for _ in range(101)]}
    with mock.patch.object(gate, "urlopen", return_value=FakeResponse(json.dumps(too_many).encode("utf-8"))):
        expect_runtime(lambda: gate.fetch_runs("o/r", "a" * 40, "token"), "more workflow runs than requested")

    with mock.patch.object(gate, "urlopen", side_effect=URLError("offline")):
        expect_runtime(lambda: gate.fetch_runs("o/r", "a" * 40, "token"), "GitHub Actions lookup failed")


def main():
    gate = load_target()
    payload_regressions(gate)
    run_regressions(gate)
    event_file_regressions(gate)
    fetch_regressions(gate)

    output = StringIO()
    errors = StringIO()
    with redirect_stdout(output), redirect_stderr(errors), mock.patch.dict(os.environ, {}, clear=True):
        result = gate.main()
    check(result == 0, "aggregate/static invocation must remain hermetic and pass without GitHub credentials")
    check("live admission check not requested" in output.getvalue(), "hermetic aggregate mode message drifted")
    check(not errors.getvalue(), "hermetic aggregate mode emitted unexpected stderr")

    print("PASS: PR branch-CI admission gate rejects noncanonical evidence identities and naive timestamps while preserving aware-time exact-head evidence, malformed-payload containment, and hermetic aggregate execution.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
