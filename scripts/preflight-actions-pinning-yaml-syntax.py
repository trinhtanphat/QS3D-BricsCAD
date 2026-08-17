from __future__ import annotations

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CHECKER = ROOT / "scripts" / "check-actions-pinned.py"
PIN = "a" * 40


def load_checker():
    spec = importlib.util.spec_from_file_location("qs3d_check_actions_pinned", CHECKER)
    if spec is None or spec.loader is None:
        raise AssertionError("could not load check-actions-pinned.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def assert_clean(module, name: str, text: str):
    errors = module.scan_workflow_text(name, text)
    if errors:
        raise AssertionError(f"{name}: expected clean result, got {errors}")


def assert_rejected(module, name: str, text: str, expected: str):
    errors = module.scan_workflow_text(name, text)
    if not any(expected in error for error in errors):
        raise AssertionError(f"{name}: expected {expected!r}, got {errors}")


def main():
    checker = load_checker()

    assert_clean(
        checker,
        "block-pinned.yml",
        f"on: push\njobs:\n  test:\n    steps:\n      - uses: actions/checkout@{PIN}\n",
    )
    assert_clean(
        checker,
        "quoted-pinned.yml",
        f"on: push\njobs:\n  test:\n    steps:\n      - 'uses' : 'actions/setup-python@{PIN}'\n",
    )
    assert_clean(
        checker,
        "flow-pinned.yml",
        f'on: push\njobs: {{test: {{steps: [{{run: echo ok, "uses" : "actions/checkout@{PIN}"}}]}}}}\n',
    )
    assert_clean(
        checker,
        "flow-local.yml",
        "on: push\njobs: {test: {steps: [{uses: ./actions/local}]}}\n",
    )
    assert_clean(
        checker,
        "anchored-safe.yml",
        f"on: &events [push]\njobs:\n  test:\n    steps:\n      - uses: &checkout actions/checkout@{PIN}\n",
    )
    assert_clean(
        checker,
        "comments-and-values.yml",
        "# pull_request_target:\n"
        "on: push\n"
        "jobs:\n"
        "  test:\n"
        "    steps:\n"
        "      - run: echo 'pull_request_target:'\n"
        "      - env: {pull_request_target: harmless-value}\n"
        f"      - uses: actions/checkout@{PIN} # pull_request_target:\n",
    )

    for label, workflow in (
        ("single-quoted-event.yml", "on:\n  'pull_request_target' : {}\n"),
        ("double-quoted-event.yml", 'on:\n  "pull_request_target" : {}\n'),
        ("spaced-event.yml", "on:\n  pull_request_target : {}\n"),
        ("flow-event-map.yml", "on: {push: null, 'pull_request_target' : null}\n"),
        ("flow-event-sequence.yml", 'on: [push, "pull_request_target"]\n'),
        ("block-event-sequence.yml", "on:\n  - push\n  - 'pull_request_target'\n"),
        ("single-event-scalar.yml", "on: pull_request_target\n"),
        ("anchored-flow-event.yml", "on: &events [push, pull_request_target]\n"),
        ("anchored-block-event.yml", "on: &events\n  pull_request_target: {}\n"),
    ):
        assert_rejected(checker, label, workflow, "pull_request_target is forbidden")

    assert_rejected(
        checker,
        "root-flow-forbidden.yml",
        "{on: [push, pull_request_target], jobs: {}}\n",
        "root flow-style workflow mapping cannot be safety-checked",
    )
    assert_rejected(
        checker,
        "root-flow-safe.yml",
        f"{{on: [push], jobs: {{test: {{steps: [{{uses: actions/checkout@{PIN}}}]}}}}}}\n",
        "root flow-style workflow mapping cannot be safety-checked",
    )
    assert_rejected(
        checker,
        "aliased-trigger.yml",
        "events: &events [push]\non: *events\n",
        "on alias cannot be safety-checked",
    )
    assert_rejected(
        checker,
        "aliased-uses.yml",
        f"action: &action actions/checkout@{PIN}\non: push\njobs:\n  test:\n    steps:\n      - uses: *action\n",
        "uses alias cannot be safety-checked",
    )
    assert_rejected(
        checker,
        "anchored-mutable-ref.yml",
        "on: push\njobs:\n  test:\n    steps:\n      - uses: &checkout actions/checkout@main\n",
        "full 40-hex commit SHA",
    )
    assert_rejected(
        checker,
        "flow-mutable-ref.yml",
        "on: push\njobs: {test: {steps: [{name: test, uses: actions/checkout@main}]}}\n",
        "full 40-hex commit SHA",
    )
    assert_rejected(
        checker,
        "flow-missing-ref.yml",
        "on: push\njobs: {test: {steps: [{'uses': actions/checkout}]}}\n",
        "must include an immutable ref",
    )
    assert_rejected(
        checker,
        "flow-malformed-quoted-ref.yml",
        'on: push\njobs: {test: {steps: [{"uses": "actions/checkout@main"}]}}\n',
        "full 40-hex commit SHA",
    )
    assert_rejected(
        checker,
        "block-malformed-scalar.yml",
        'on: push\njobs:\n  test:\n    steps:\n      - uses: "actions/checkout@main\n',
        "malformed double-quoted scalar",
    )
    assert_rejected(
        checker,
        "plaintext-http.yml",
        "on: push\njobs:\n  test:\n    steps:\n      - run: curl http://example.invalid\n",
        "plaintext HTTP is forbidden",
    )

    workflow_dir = ROOT / ".github" / "workflows"
    for workflow in sorted([*workflow_dir.glob("*.yml"), *workflow_dir.glob("*.yaml")]):
        text = workflow.read_text(encoding="utf-8")
        assert_clean(checker, str(workflow.relative_to(ROOT)), text)

    print(
        "PASS: Actions pinning guard rejects quoted/flow/anchored pull_request_target, root flow-style workflow mappings, "
        "mutable or aliased uses, and unresolved trigger aliases while preserving pinned/local/comment/value and safe-anchor controls."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
