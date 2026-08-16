#!/usr/bin/env python3
from contextlib import redirect_stdout
from io import StringIO
import importlib.util
import os
from pathlib import Path
import stat
import tempfile
from unittest import mock

ROOT = Path(__file__).resolve().parents[1]
RUNNER_PATH = ROOT / "scripts" / "preflight-all.py"


def load_runner():
    spec = importlib.util.spec_from_file_location("qs3d_preflight_all", RUNNER_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError("unable to load scripts/preflight-all.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def assert_true(condition, message):
    if not condition:
        raise AssertionError(message)


def assert_raises_runtime(callable_obj, contains):
    try:
        callable_obj()
    except RuntimeError as exc:
        assert_true(contains in str(exc), "unexpected RuntimeError: " + str(exc))
        return
    raise AssertionError("expected RuntimeError containing: " + contains)


def write_gate(path, body="raise SystemExit(0)\n"):
    path.write_text(body, encoding="utf-8")


def discovery_regressions(runner):
    with tempfile.TemporaryDirectory() as temp_dir:
        root = Path(temp_dir)
        scripts = root / "scripts"
        scripts.mkdir()
        runner.ROOT = root
        runner.SCRIPTS = scripts
        runner.SELF = scripts / "preflight-all.py"

        beta = scripts / "preflight-beta.py"
        alpha = scripts / "preflight-Alpha.py"
        write_gate(beta)
        write_gate(alpha)
        discovered = runner.discover()
        assert_true([path.name for path in discovered] == ["preflight-Alpha.py", "preflight-beta.py"],
                    "valid gates must retain deterministic casefold/name ordering")

        non_regular = scripts / "preflight-directory.py"
        non_regular.mkdir()
        assert_raises_runtime(runner.discover, "non-regular")
        non_regular.rmdir()

        regular_mode = type("StatResult", (), {"st_mode": stat.S_IFREG | 0o644})()
        synthetic_upper = scripts / "preflight-Collision.py"
        synthetic_lower = scripts / "preflight-collision.py"
        synthetic_upper.write_text("", encoding="utf-8")
        with mock.patch.object(runner.os, "lstat", return_value=regular_mode), \
             mock.patch.object(Path, "is_symlink", return_value=False):
            assert_raises_runtime(
                lambda: runner.validate_candidates([synthetic_upper, synthetic_lower]),
                "case-insensitive preflight filename collision")
        synthetic_upper.unlink()

        symlink_candidate = scripts / "preflight-link.py"
        symlink_candidate.write_text("", encoding="utf-8")
        with mock.patch.object(runner.os, "lstat", return_value=regular_mode), \
             mock.patch.object(Path, "is_symlink", return_value=True):
            assert_raises_runtime(lambda: runner.validate_candidates([symlink_candidate]), "symlink")
        symlink_candidate.unlink()

        # A symlink-like candidate whose resolved target is the aggregate runner
        # must not be silently excluded as SELF before validation. This models the
        # bypass without requiring Windows symlink privileges in CI.
        self_alias = scripts / "preflight-self-alias.py"
        self_alias.write_text("", encoding="utf-8")
        original_resolve = Path.resolve
        original_is_symlink = Path.is_symlink

        def synthetic_resolve(path, *args, **kwargs):
            if path == self_alias:
                return runner.SELF
            return original_resolve(path, *args, **kwargs)

        def synthetic_is_symlink(path):
            if path == self_alias:
                return True
            return original_is_symlink(path)

        with mock.patch.object(Path, "resolve", synthetic_resolve), \
             mock.patch.object(Path, "is_symlink", synthetic_is_symlink):
            assert_raises_runtime(runner.discover, "symlink")
        self_alias.unlink()

        outside = root.parent / "preflight-outside.py"
        assert_raises_runtime(lambda: runner.validate_candidates([outside]), "outside repository root")

        self_gate = scripts / "preflight-all.py"
        write_gate(self_gate)
        discovered = runner.discover()
        assert_true(self_gate not in discovered, "aggregate runner must never recursively discover itself")


def execution_regressions(runner):
    with tempfile.TemporaryDirectory() as temp_dir:
        root = Path(temp_dir)
        scripts = root / "scripts"
        scripts.mkdir()
        runner.ROOT = root
        runner.SCRIPTS = scripts
        runner.SELF = scripts / "preflight-all.py"

        ok = scripts / "preflight-a-ok.py"
        fail = scripts / "preflight-b-fail.py"
        write_gate(ok)
        write_gate(fail, "raise SystemExit(7)\n")
        output = StringIO()
        with redirect_stdout(output):
            result = runner.main()
        text = output.getvalue()
        assert_true(result == 1, "nonzero child must fail aggregate runner")
        assert_true("preflight-b-fail.py exit=7" in text, "child exit code must remain visible")

        fail.unlink()
        slow = scripts / "preflight-b-slow.py"
        write_gate(slow, "import time\ntime.sleep(1)\n")
        runner.CHILD_TIMEOUT_SECONDS = 0.05
        output = StringIO()
        with redirect_stdout(output):
            result = runner.main()
        text = output.getvalue()
        assert_true(result == 1, "timed-out child must fail aggregate runner")
        assert_true("preflight-b-slow.py timeout" in text, "timeout reason must remain visible")

        runner.CHILD_TIMEOUT_SECONDS = 180
        slow.unlink()
        output = StringIO()
        with mock.patch.object(runner.subprocess, "run", side_effect=OSError("synthetic launch failure")), \
             redirect_stdout(output):
            result = runner.main()
        text = output.getvalue()
        assert_true(result == 1, "child launch failure must fail aggregate runner")
        assert_true("preflight-a-ok.py launch" in text, "launch failure reason must remain visible")

        calls = []
        with mock.patch.object(runner.subprocess, "run", side_effect=lambda args, **kwargs: calls.append(args) or mock.Mock(returncode=0)), \
             redirect_stdout(StringIO()):
            result = runner.main()
        assert_true(result == 0, "all-success child execution must pass")
        assert_true(len(calls) == 1 and calls[0][1].endswith("preflight-a-ok.py"),
                    "each discovered gate must execute exactly once")


def annotation_regressions(runner):
    with mock.patch.dict(os.environ, {"GITHUB_ACTIONS": "true"}, clear=False):
        output = StringIO()
        with redirect_stdout(output):
            runner.emit_failure_annotation("scripts/a:b,c%\n.py", "exit=9\nreason")
        text = output.getvalue()
        assert_true("::error file=scripts/a%3Ab%2Cc%25%0A.py::" in text,
                    "GitHub Actions file property escaping regressed")
        assert_true("%0A" in text and "exit=9" in text,
                    "GitHub Actions annotation data escaping regressed")


def main():
    runner = load_runner()
    discovery_regressions(runner)
    execution_regressions(runner)
    annotation_regressions(runner)
    print("PASS: aggregate preflight discovery is deterministic, repository-contained, fail-closed, and preserves failure reporting.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
