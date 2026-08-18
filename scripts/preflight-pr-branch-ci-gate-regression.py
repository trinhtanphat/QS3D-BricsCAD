#!/usr/bin/env python3
from contextlib import redirect_stderr, redirect_stdout
from io import StringIO
import importlib.util
import json
import os
from pathlib import Path
import tempfile
from unittest import mock

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-pr-branch-ci-gate.py"


def load_target():
    spec = importlib.util.spec_from_file_location("qs3d_pr_branch_ci_gate", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("unable to load branch CI identity guard")
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
        },
    }


def set_path(root, dotted_path, value):
    target = root
    parts = dotted_path.split(".")
    for part in parts[:-1]:
        target = target[part]
    target[parts[-1]] = value


def payload_regressions(gate):
    qualified = gate.qualify_pr_payload(valid_event(), "trinhtanphat/QS3D-BricsCAD")
    check(
        qualified == ("agent/worker/fix", "a" * 40, "trinhtanphat/QS3D-BricsCAD"),
        "valid internal PR identity drifted",
    )

    event = valid_event()
    event["action"] = "synchronize"
    expect_runtime(
        lambda: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"),
        "action=opened",
    )

    malformed_objects = (
        ("pull_request", []),
        ("pull_request.user", "owner"),
        ("pull_request.head", []),
        ("pull_request.base", "main"),
        ("pull_request.head.repo", []),
        ("pull_request.base.repo", []),
    )
    for dotted_path, value in malformed_objects:
        event = valid_event()
        set_path(event, dotted_path, value)
        expect_runtime(
            lambda event=event, dotted_path=dotted_path: gate.qualify_pr_payload(
                event, "trinhtanphat/QS3D-BricsCAD"
            ),
            dotted_path,
        )

    canonicality_mutations = (
        ("pull_request.user.login", " trinhtanphat ", "canonical"),
        ("pull_request.head.ref", " agent/worker/fix", "canonical"),
        ("pull_request.head.sha", "a" * 40 + " ", "canonical"),
        ("pull_request.head.repo.full_name", "trinhtanphat/QS3D-BricsCAD ", "canonical"),
        ("pull_request.base.repo.full_name", " trinhtanphat/QS3D-BricsCAD", "canonical"),
    )
    for dotted_path, value, expected in canonicality_mutations:
        event = valid_event()
        set_path(event, dotted_path, value)
        expect_runtime(
            lambda event=event: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"),
            expected,
        )

    non_string_mutations = (
        ("pull_request.user.login", []),
        ("pull_request.head.ref", {}),
        ("pull_request.head.sha", 123),
        ("pull_request.head.repo.full_name", []),
        ("pull_request.base.repo.full_name", {}),
    )
    for dotted_path, value in non_string_mutations:
        event = valid_event()
        set_path(event, dotted_path, value)
        expect_runtime(
            lambda event=event: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"),
            "must be a string",
        )

    event = valid_event()
    event["pull_request"]["head"]["sha"] = "not-a-sha"
    expect_runtime(
        lambda: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"),
        "invalid PR head SHA",
    )

    event = valid_event()
    event["pull_request"]["head"]["ref"] = "fix/not-watched"
    expect_runtime(
        lambda: gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD"),
        "outside automatic branch-CI namespaces",
    )

    event = valid_event()
    event["pull_request"]["user"]["login"] = gate.DEPENDABOT_LOGIN
    check(
        gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD") is None,
        "Dependabot exception regressed",
    )

    event = valid_event()
    event["pull_request"]["head"]["repo"]["full_name"] = "external/fork"
    check(
        gate.qualify_pr_payload(event, "trinhtanphat/QS3D-BricsCAD") is None,
        "fork exception regressed",
    )


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


def no_timestamp_admission_regression():
    source = TARGET.read_text(encoding="utf-8")
    forbidden = (
        "pr_created_at",
        "updated_at <=",
        "created_at <=",
        "run_attempt",
        "completed SUCCESS on exact head",
    )
    for token in forbidden:
        check(token not in source, f"hard PR-creation-time admission token returned: {token}")
    check(
        "protected current-candidate preflight/core remain the merge gate" in source,
        "protected PR gate handoff message is missing",
    )


def main():
    gate = load_target()
    payload_regressions(gate)
    event_file_regressions(gate)
    no_timestamp_admission_regression()

    output = StringIO()
    errors = StringIO()
    with redirect_stdout(output), redirect_stderr(errors), mock.patch.dict(os.environ, {}, clear=True):
        result = gate.main()
    check(result == 0, "aggregate/static invocation must remain hermetic")
    check("static contract/self-tests PASS" in output.getvalue(), "hermetic aggregate mode message drifted")
    check(not errors.getvalue(), "hermetic aggregate mode emitted unexpected stderr")

    print(
        "PASS: PR branch-CI identity guard preserves namespace/SHA/payload fail-closed checks without "
        "PR-creation timestamp admission churn; protected preflight/core remain authoritative merge gates."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
