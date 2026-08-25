#!/usr/bin/env python3
from contextlib import redirect_stdout
from io import BytesIO, StringIO
import importlib.util
from pathlib import Path
import subprocess
from unittest import mock

ROOT = Path(__file__).resolve().parents[1]
RUNNER_PATH = ROOT / "scripts" / "preflight-all.py"


def load_runner():
    spec = importlib.util.spec_from_file_location("qs3d_preflight_all_output_bounds", RUNNER_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError("unable to load scripts/preflight-all.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def assert_true(condition, message):
    if not condition:
        raise AssertionError(message)


class TextStream:
    def read(self, _size):
        return "not-bytes"


class FakeProcess:
    def __init__(self, payload, returncode=0, timeout=False):
        self.pid = 4242
        self.stdout = BytesIO(payload)
        self.returncode = returncode
        self.timeout = timeout
        self.killed = False

    def wait(self, timeout=None):
        if self.timeout:
            raise subprocess.TimeoutExpired(["python", "gate.py"], timeout)
        return self.returncode

    def kill(self):
        self.killed = True


def bounded_copy_regressions(runner):
    limit = 17
    payload = b"0123456789abcdefghijklmnop"
    target = BytesIO()
    emitted, truncated = runner.copy_bounded_output(BytesIO(payload), target=target, limit_bytes=limit)
    assert_true(emitted == limit, "bounded output must report exactly the emitted byte count")
    assert_true(truncated, "payload beyond the byte budget must be marked truncated")
    assert_true(target.getvalue() == payload[:limit], "bounded output must retain the deterministic prefix only")

    exact_target = BytesIO()
    emitted, truncated = runner.copy_bounded_output(BytesIO(payload[:limit]), target=exact_target, limit_bytes=limit)
    assert_true(emitted == limit, "exact-limit output byte count regressed")
    assert_true(not truncated, "exact-limit output must not be marked truncated")

    zero_target = BytesIO()
    emitted, truncated = runner.copy_bounded_output(BytesIO(b"x"), target=zero_target, limit_bytes=0)
    assert_true(emitted == 0 and truncated, "zero-byte budget must still drain and mark output truncated")
    assert_true(zero_target.getvalue() == b"", "zero-byte budget must not emit child bytes")

    try:
        runner.copy_bounded_output(TextStream(), target=StringIO(), limit_bytes=limit)
    except runner.GateOutputError:
        pass
    else:
        raise AssertionError("non-bytes child output must fail closed")


def run_gate_regressions(runner):
    payload = b"x" * (runner.MAX_FEATURE_GATE_OUTPUT_BYTES + 3)
    fake = FakeProcess(payload)
    output = StringIO()
    gate_path = runner.ROOT / "scripts" / "preflight-synthetic.py"
    with mock.patch.object(runner.subprocess, "Popen", return_value=fake) as popen, redirect_stdout(output):
        returncode = runner.run_gate(gate_path, {}, 1.0)
    assert_true(returncode == 0, "successful child exit must remain successful")
    kwargs = popen.call_args.kwargs
    assert_true(kwargs.get("stdout") is subprocess.PIPE, "child stdout must be piped for bounded draining")
    assert_true(kwargs.get("stderr") is subprocess.STDOUT, "child stderr must share the bounded output pipe")
    assert_true(
        "aggregate output truncated after" in output.getvalue(),
        "truncated child output must emit an explicit deterministic marker",
    )

    timeout_fake = FakeProcess(b"partial-output", timeout=True)
    with mock.patch.object(runner.subprocess, "Popen", return_value=timeout_fake), \
         mock.patch.object(runner, "_terminate_process_tree", return_value=None), \
         redirect_stdout(StringIO()):
        try:
            runner.run_gate(gate_path, {}, 0.05)
        except runner.GateTimeoutError as exc:
            assert_true(exc.cleanup_error is None, "successful timeout cleanup must remain clean")
            assert_true(exc.output_error is None, "drained timeout output must not create a false output failure")
        else:
            raise AssertionError("timed-out child must still raise GateTimeoutError")


def drain_failure_regressions(runner):
    completed_thread = mock.Mock()
    completed_thread.is_alive.return_value = False
    state = {"error": runner.GateOutputError("synthetic")}
    error = runner._finish_output_drain(completed_thread, state)
    assert_true(error == "output-drain-error=GateOutputError", "output drain exceptions must be fail-closed and classified")

    stuck_thread = mock.Mock()
    stuck_thread.is_alive.return_value = True
    error = runner._finish_output_drain(stuck_thread, {})
    assert_true(error == "output-drain-timeout", "stuck output drain must be fail-closed and bounded")
    stuck_thread.join.assert_called_once_with(timeout=runner.OUTPUT_DRAIN_JOIN_TIMEOUT_SECONDS)


def main():
    runner = load_runner()
    bounded_copy_regressions(runner)
    run_gate_regressions(runner)
    drain_failure_regressions(runner)
    print("PASS: aggregate preflight child output is continuously drained, byte-bounded, truncation-visible, and fail-closed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
