#!/usr/bin/env python3
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest import mock
import contextlib
import io
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER_PATH = ROOT / "scripts" / "preflight-all.py"


def load_runner():
    spec = spec_from_file_location("qs3d_preflight_all", RUNNER_PATH)
    if spec is None or spec.loader is None:
        raise AssertionError("Unable to load preflight-all.py for regression coverage.")
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def write_gate(path, body="raise SystemExit(0)\n"):
    path.write_text("#!/usr/bin/env python3\n" + body, encoding="utf-8")


def expect_discovery_error(runner, action, needle):
    try:
        action()
    except runner.DiscoveryError as exc:
        if needle not in str(exc):
            raise AssertionError("Expected discovery failure containing %r, got %r" % (needle, str(exc)))
        return
    raise AssertionError("Expected DiscoveryError containing %r." % needle)


def test_deterministic_order_and_self_exclusion(runner):
    with TemporaryDirectory() as temp:
        root = Path(temp)
        scripts = root / "scripts"
        scripts.mkdir()
        self_path = scripts / "preflight-all.py"
        write_gate(self_path)
        write_gate(scripts / "preflight-zeta.py")
        write_gate(scripts / "preflight-Alpha.py")
        write_gate(scripts / "preflight-beta.py")

        names = [p.name for p in runner.discover(scripts=scripts, root=root, self_path=self_path)]
        expected = ["preflight-Alpha.py", "preflight-beta.py", "preflight-zeta.py"]
        if names != expected:
            raise AssertionError("Discovery order/self-exclusion mismatch: %r" % names)


def test_non_regular_and_symlink_rejection(runner):
    with TemporaryDirectory() as temp:
        root = Path(temp)
        scripts = root / "scripts"
        scripts.mkdir()
        self_path = scripts / "preflight-all.py"
        write_gate(self_path)

        directory_gate = scripts / "preflight-directory.py"
        directory_gate.mkdir()
        expect_discovery_error(
            runner,
            lambda: runner.discover(scripts=scripts, root=root, self_path=self_path),
            "regular file",
        )

        directory_gate.rmdir()
        symlink_candidate = scripts / "preflight-symlink.py"
        write_gate(symlink_candidate)
        path_type = type(symlink_candidate)
        original_is_symlink = path_type.is_symlink

        def fake_is_symlink(path):
            if path == symlink_candidate:
                return True
            return original_is_symlink(path)

        with mock.patch.object(path_type, "is_symlink", fake_is_symlink):
            expect_discovery_error(
                runner,
                lambda: runner.validate_candidates([symlink_candidate], root=root, self_path=self_path),
                "must not be a symlink",
            )


def test_out_of_root_and_case_collision_rejection(runner):
    with TemporaryDirectory() as temp:
        base = Path(temp)
        root = base / "repo"
        scripts = root / "scripts"
        scripts.mkdir(parents=True)
        self_path = scripts / "preflight-all.py"
        write_gate(self_path)
        outside = base / "preflight-outside.py"
        write_gate(outside)

        expect_discovery_error(
            runner,
            lambda: runner.validate_candidates([outside], root=root, self_path=self_path),
            "outside repository root",
        )

        expect_discovery_error(
            runner,
            lambda: runner._ensure_unique_casefold_names(
                [Path("preflight-Case.py"), Path("preflight-case.py")]
            ),
            "case-insensitive preflight filename collision",
        )


def test_child_failure_modes_and_exactly_once_execution(runner):
    with TemporaryDirectory() as temp:
        root = Path(temp)
        gate = root / "preflight-child.py"
        write_gate(gate, "raise SystemExit(7)\n")
        reason = runner.run_gate(gate, root=root, child_env=os.environ.copy(), timeout=5)
        if reason != "exit=7":
            raise AssertionError("Expected child exit propagation, got %r" % reason)

        completed = subprocess.CompletedProcess([sys.executable, str(gate)], 0)
        with mock.patch.object(runner.subprocess, "run", return_value=completed) as run:
            reason = runner.run_gate(gate, root=root, child_env={}, timeout=3)
            if reason is not None or run.call_count != 1:
                raise AssertionError("Valid gate must execute exactly once; reason=%r count=%d" % (reason, run.call_count))

        with mock.patch.object(runner.subprocess, "run", side_effect=subprocess.TimeoutExpired("gate", 3)):
            if runner.run_gate(gate, root=root, child_env={}, timeout=3) != "timeout":
                raise AssertionError("Timeout must fail closed with the timeout reason.")

        with mock.patch.object(runner.subprocess, "run", side_effect=OSError("launch failed")):
            if runner.run_gate(gate, root=root, child_env={}, timeout=3) != "launch":
                raise AssertionError("Launch failure must fail closed with the launch reason.")


def test_actions_annotation_escaping(runner):
    old = os.environ.get("GITHUB_ACTIONS")
    os.environ["GITHUB_ACTIONS"] = "true"
    try:
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            runner.emit_failure_annotation("scripts/a:b,c%20.py", "bad\r\nreason%")
        text = output.getvalue()
        for token in ("a%3Ab%2Cc%2520.py", "bad%0D%0Areason%25"):
            if token not in text:
                raise AssertionError("Actions annotation did not escape %r: %r" % (token, text))
    finally:
        if old is None:
            os.environ.pop("GITHUB_ACTIONS", None)
        else:
            os.environ["GITHUB_ACTIONS"] = old


def test_discovery_failure_happens_before_child_execution(runner):
    with TemporaryDirectory() as temp:
        root = Path(temp)
        scripts = root / "scripts"
        scripts.mkdir()
        self_path = scripts / "preflight-all.py"
        write_gate(self_path)
        good = scripts / "preflight-good.py"
        bad = scripts / "preflight-bad.py"
        write_gate(good)
        bad.mkdir()

        with mock.patch.object(runner.subprocess, "run") as run:
            expect_discovery_error(
                runner,
                lambda: runner.discover(scripts=scripts, root=root, self_path=self_path),
                "regular file",
            )
            if run.call_count != 0:
                raise AssertionError("Unsafe discovery must fail before any child gate executes.")


def main():
    runner = load_runner()
    tests = (
        test_deterministic_order_and_self_exclusion,
        test_non_regular_and_symlink_rejection,
        test_out_of_root_and_case_collision_rejection,
        test_child_failure_modes_and_exactly_once_execution,
        test_actions_annotation_escaping,
        test_discovery_failure_happens_before_child_execution,
    )
    for test in tests:
        test(runner)
        print("PASS:", test.__name__)
    print("PASS: aggregate preflight discovery is deterministic, repository-contained, and fail-closed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
