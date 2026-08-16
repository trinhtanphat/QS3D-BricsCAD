#!/usr/bin/env python3
from pathlib import Path
import contextlib
import importlib.util
import io
import os
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "preflight-all.py"


def fail(message):
    raise AssertionError(message)


def require(condition, message):
    if not condition:
        fail(message)


def load_runner():
    spec = importlib.util.spec_from_file_location("qs3d_preflight_all_under_test", RUNNER)
    if spec is None or spec.loader is None:
        fail("unable to load preflight-all.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def with_fixture(module, root):
    module.ROOT = root
    module.SCRIPTS = root / "scripts"
    module.SELF = (module.SCRIPTS / "preflight-all.py").resolve()
    module.SCRIPTS.mkdir(parents=True, exist_ok=True)
    module.SELF.write_text("# fixture aggregate runner\n", encoding="utf-8")


def expect_discovery_failure(module, expected):
    try:
        module.discover()
    except RuntimeError as exc:
        require(expected in str(exc), "unexpected discovery failure: " + str(exc))
        return
    fail("expected discovery failure containing: " + expected)


def test_deterministic_order_and_self_exclusion(module, root):
    with_fixture(module, root)
    for name in ("preflight-z.py", "preflight-B.py", "preflight-a.py"):
        (module.SCRIPTS / name).write_text("print('ok')\n", encoding="utf-8")
    names = [path.name for path in module.discover()]
    require(names == ["preflight-a.py", "preflight-B.py", "preflight-z.py"], "discovery order is not deterministic")
    require("preflight-all.py" not in names, "aggregate runner recursively discovered itself")


def test_non_regular_rejected(module, root):
    with_fixture(module, root)
    (module.SCRIPTS / "preflight-directory.py").mkdir()
    expect_discovery_failure(module, "must be a regular file")


def test_symlink_rejected(module, root):
    with_fixture(module, root)
    target = module.SCRIPTS / "target.py"
    target.write_text("print('ok')\n", encoding="utf-8")
    link = module.SCRIPTS / "preflight-linked.py"
    try:
        link.symlink_to(target.name)
    except (OSError, NotImplementedError):
        return
    expect_discovery_failure(module, "must not be a symlink")


def test_case_collision_rejected(module, root):
    with_fixture(module, root)
    first = module.SCRIPTS / "preflight-Case.py"
    second = module.SCRIPTS / "preflight-case.py"
    first.write_text("print('one')\n", encoding="utf-8")
    second.write_text("print('two')\n", encoding="utf-8")
    try:
        distinct = first.samefile(second) is False
    except OSError:
        distinct = True
    if not distinct:
        return
    expect_discovery_failure(module, "case-insensitive feature preflight filename collision")


def test_run_gate_failure_modes(module, root):
    with_fixture(module, root)
    gate = module.SCRIPTS / "preflight-child.py"
    gate.write_text("print('fixture')\n", encoding="utf-8")
    original = module.subprocess.run
    try:
        module.subprocess.run = lambda *args, **kwargs: subprocess.CompletedProcess(args[0], 7)
        require(module.run_gate(gate, {}) == ("scripts/preflight-child.py", "exit=7"), "nonzero exit was not propagated")

        def timeout(*args, **kwargs):
            raise subprocess.TimeoutExpired(args[0], module.CHILD_TIMEOUT_SECONDS)

        module.subprocess.run = timeout
        require(module.run_gate(gate, {}) == ("scripts/preflight-child.py", "timeout"), "timeout was not propagated")

        def launch(*args, **kwargs):
            raise OSError("fixture launch failure")

        module.subprocess.run = launch
        require(module.run_gate(gate, {}) == ("scripts/preflight-child.py", "launch"), "launch failure was not propagated")
    finally:
        module.subprocess.run = original


def test_main_fails_before_child_execution(module, root):
    with_fixture(module, root)
    (module.SCRIPTS / "preflight-directory.py").mkdir()
    original = module.subprocess.run
    calls = []
    module.subprocess.run = lambda *args, **kwargs: calls.append(args) or subprocess.CompletedProcess(args[0], 0)
    try:
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            result = module.main()
        require(result == 1, "aggregate runner did not fail closed on discovery error")
        require("feature preflight discovery failed" in out.getvalue(), "discovery failure diagnostic missing")
        require(not calls, "child execution began before unsafe discovery failed")
    finally:
        module.subprocess.run = original


def test_valid_gates_execute_once_in_order(module, root):
    with_fixture(module, root)
    for name in ("preflight-c.py", "preflight-A.py", "preflight-b.py"):
        (module.SCRIPTS / name).write_text("print('ok')\n", encoding="utf-8")
    original = module.subprocess.run
    calls = []

    def record(*args, **kwargs):
        calls.append(Path(args[0][1]).name)
        return subprocess.CompletedProcess(args[0], 0)

    module.subprocess.run = record
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            result = module.main()
        require(result == 0, "valid aggregate fixture did not pass")
        require(calls == ["preflight-A.py", "preflight-b.py", "preflight-c.py"], "valid gates were not executed exactly once in deterministic order")
    finally:
        module.subprocess.run = original


def test_actions_escaping(module):
    require(module.escape_actions_data("a%b\rc\nd") == "a%25b%0Dc%0Ad", "annotation data escaping regressed")
    require(module.escape_actions_property("a:b,c") == "a%3Ab%2Cc", "annotation property escaping regressed")

    old = os.environ.get("GITHUB_ACTIONS")
    os.environ["GITHUB_ACTIONS"] = "true"
    try:
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            module.emit_failure_annotation("scripts/a:b,c.py", "bad%reason\nnext")
        text = out.getvalue()
        require("file=scripts/a%3Ab%2Cc.py" in text, "annotation file property was not escaped")
        require("bad%25reason%0Anext" in text, "annotation message was not escaped")
    finally:
        if old is None:
            os.environ.pop("GITHUB_ACTIONS", None)
        else:
            os.environ["GITHUB_ACTIONS"] = old


def main():
    module = load_runner()
    with tempfile.TemporaryDirectory(prefix="qs3d-preflight-discovery-") as temp:
        base = Path(temp)
        tests = (
            test_deterministic_order_and_self_exclusion,
            test_non_regular_rejected,
            test_symlink_rejected,
            test_case_collision_rejected,
            test_run_gate_failure_modes,
            test_main_fails_before_child_execution,
            test_valid_gates_execute_once_in_order,
        )
        for index, test in enumerate(tests):
            root = base / ("fixture-" + str(index))
            root.mkdir()
            test(module, root)
    test_actions_escaping(module)
    print("PASS: aggregate product preflight discovery is deterministic, fail-closed, bounded, and hermetically regression-covered.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
