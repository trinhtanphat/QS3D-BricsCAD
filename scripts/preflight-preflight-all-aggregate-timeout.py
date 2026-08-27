#!/usr/bin/env python3
from contextlib import redirect_stdout
from importlib.util import module_from_spec, spec_from_file_location
from io import BytesIO, StringIO
from pathlib import Path
from types import SimpleNamespace
import subprocess

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-all.py"


def load_target():
    spec = spec_from_file_location("qs3d_preflight_all_aggregate_timeout", TARGET)
    if spec is None or spec.loader is None:
        raise RuntimeError("cannot load aggregate preflight runner")
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def fake_gate(module, name="preflight-fake-budget-probe.py"):
    return module.ROOT / "scripts" / name


def test_budget_math(module):
    require(module.AGGREGATE_TIMEOUT_SECONDS == 15 * 60, "aggregate budget must remain 15 minutes")
    require(module.CHILD_TIMEOUT_SECONDS == 180, "per-child timeout must remain 180 seconds")
    require(module.PROCESS_TREE_CLEANUP_TIMEOUT_SECONDS == 10, "tree cleanup must remain separately bounded")
    require(module.remaining_child_timeout(100.0, 100.0) == float(module.CHILD_TIMEOUT_SECONDS), "fresh child must retain normal timeout")
    require(module.remaining_child_timeout(100.0, 850.0) == 150.0, "child timeout must be clipped to remaining aggregate budget")
    require(module.remaining_child_timeout(100.0, 1000.0) == 0.0, "expired aggregate budget must return zero")
    require(module.remaining_child_timeout(100.0, 99.0) == float(module.CHILD_TIMEOUT_SECONDS), "monotonic anomaly must not enlarge child timeout")


def test_process_group_launch_contract(module):
    posix = module._process_group_launch_kwargs("posix")
    require(posix == {"start_new_session": True}, "POSIX gates must launch in a dedicated session/process group")

    windows = module._process_group_launch_kwargs("nt")
    require(set(windows) == {"creationflags"}, "Windows gates must use process-group creation flags only")
    require(windows["creationflags"] == module.WINDOWS_CREATE_NEW_PROCESS_GROUP, "Windows gate launch must use CREATE_NEW_PROCESS_GROUP")
    require(windows["creationflags"] != 0, "Windows process-group creation flag must be nonzero")


def test_run_gate_timeout_requests_tree_cleanup(module):
    gate = fake_gate(module, "preflight-fake-tree-timeout.py")
    launches = []
    cleanup = []

    class FakeProcess:
        pid = 4242

        def __init__(self):
            self.stdout = BytesIO(b"partial-timeout-output")

        def wait(self, timeout=None):
            raise subprocess.TimeoutExpired(cmd="fake", timeout=timeout)

    def fake_popen(*args, **kwargs):
        launches.append((args, kwargs))
        return FakeProcess()

    def fake_cleanup(process, platform_name=None):
        cleanup.append((process.pid, platform_name))
        return None

    module.subprocess.Popen = fake_popen
    module._terminate_process_tree = fake_cleanup
    try:
        module.run_gate(gate, {"QS3D_SENTINEL": "1"}, 12.5)
    except module.GateTimeoutError as exc:
        require(exc.timeout_seconds == 12.5, "timeout exception must preserve the applied child timeout")
        require(exc.cleanup_error is None, "successful owned-tree cleanup must not report cleanup failure")
        require(exc.output_error is None, "successfully drained timeout output must not report output failure")
    else:
        raise AssertionError("run_gate must convert process timeout into GateTimeoutError")

    require(len(launches) == 1, "run_gate must launch exactly one process")
    launch_args, launch_kwargs = launches[0]
    require(launch_args[0] == [module.sys.executable, str(gate)], "gate launch command changed unexpectedly")
    require(launch_kwargs["cwd"] == str(module.ROOT), "gate launch cwd must remain repository root")
    require(launch_kwargs["env"] == {"QS3D_SENTINEL": "1"}, "gate launch must preserve sanitized child environment")
    require(launch_kwargs["stdout"] is subprocess.PIPE, "gate stdout must use the bounded output pipe")
    require(launch_kwargs["stderr"] is subprocess.STDOUT, "gate stderr must share the bounded output pipe")
    require(cleanup == [(4242, None)], "timeout must target cleanup at the exact launched process")


def test_windows_cleanup_is_tree_scoped_and_bounded(module):
    observed = []

    class FakeProcess:
        pid = 5151

        def wait(self, timeout=None):
            observed.append(("wait", timeout))
            return 0

        def kill(self):
            observed.append(("kill", None))

    def fake_run(*args, **kwargs):
        observed.append(("run", args, kwargs))
        return SimpleNamespace(returncode=0)

    module.subprocess.run = fake_run
    error = module._terminate_process_tree(FakeProcess(), "nt")
    require(error is None, "successful Windows taskkill tree cleanup must be accepted")
    runs = [entry for entry in observed if entry[0] == "run"]
    require(len(runs) == 1, "Windows cleanup must invoke exactly one tree-kill command")
    command = runs[0][1][0]
    kwargs = runs[0][2]
    require(command == ["taskkill", "/PID", "5151", "/T", "/F"], "Windows cleanup must target only the owned PID tree")
    require(kwargs["timeout"] == module.PROCESS_TREE_CLEANUP_TIMEOUT_SECONDS, "Windows cleanup command must be bounded")
    require(not [entry for entry in observed if entry[0] == "kill"], "direct fallback kill must not run after successful tree cleanup")


def test_posix_cleanup_targets_owned_group(module):
    observed = []

    class FakeProcess:
        pid = 6161

        def wait(self, timeout=None):
            observed.append(("wait", timeout))
            return 0

        def kill(self):
            observed.append(("kill", None))

    def fake_killpg(pid, sig):
        observed.append(("killpg", pid, sig))

    had_sigkill = hasattr(module.signal, "SIGKILL")
    original_sigkill = getattr(module.signal, "SIGKILL", None)
    expected_sigkill = original_sigkill if had_sigkill else 9
    if not had_sigkill:
        module.signal.SIGKILL = expected_sigkill
    module.os.killpg = fake_killpg
    try:
        error = module._terminate_process_tree(FakeProcess(), "posix")
    finally:
        if not had_sigkill:
            delattr(module.signal, "SIGKILL")
    require(error is None, "successful POSIX process-group cleanup must be accepted")
    require(("killpg", 6161, expected_sigkill) in observed, "POSIX cleanup must target the launched process group")
    require(not [entry for entry in observed if entry[0] == "kill"], "direct fallback kill must not run after successful group cleanup")


def test_expired_budget_prevents_launch(module):
    gate = fake_gate(module)
    module.discover = lambda: [gate]
    module.build_child_env = lambda source=None: {}
    ticks = iter([100.0, 100.0 + module.AGGREGATE_TIMEOUT_SECONDS])
    module.time.monotonic = lambda: next(ticks)
    launched = []

    def forbidden_run_gate(*args, **kwargs):
        launched.append((args, kwargs))
        raise AssertionError("aggregate runner launched a child after budget exhaustion")

    module.run_gate = forbidden_run_gate
    output = StringIO()
    with redirect_stdout(output):
        result = module.main()
    require(result == 1, "expired aggregate budget must fail closed")
    require(not launched, "expired aggregate budget must fail before child launch")
    require("aggregate-timeout" in output.getvalue(), "expired budget must report aggregate-timeout")


def test_remaining_budget_clips_gate_timeout(module):
    gate = fake_gate(module, "preflight-fake-clipped-budget.py")
    module.discover = lambda: [gate]
    module.build_child_env = lambda source=None: {"QS3D_SENTINEL": "1"}
    ticks = iter([100.0, 100.0 + module.AGGREGATE_TIMEOUT_SECONDS - 25.0])
    module.time.monotonic = lambda: next(ticks)
    observed = []

    def fake_run_gate(*args, **kwargs):
        observed.append((args, kwargs))
        return 0

    module.run_gate = fake_run_gate
    with redirect_stdout(StringIO()):
        result = module.main()
    require(result == 0, "clipped child inside remaining budget must still pass")
    require(len(observed) == 1, "exactly one child launch expected")
    require(observed[0][0][2] == 25.0, "gate timeout must equal remaining aggregate budget")
    require(observed[0][0][2] <= module.CHILD_TIMEOUT_SECONDS, "aggregate clipping must never weaken child timeout ceiling")


def test_timeout_at_budget_edge_stops_following_children(module):
    first = fake_gate(module, "preflight-fake-first.py")
    second = fake_gate(module, "preflight-fake-second.py")
    module.discover = lambda: [first, second]
    module.build_child_env = lambda source=None: {}
    ticks = iter([
        100.0,
        100.0 + module.AGGREGATE_TIMEOUT_SECONDS - 10.0,
        100.0 + module.AGGREGATE_TIMEOUT_SECONDS + 0.1,
    ])
    module.time.monotonic = lambda: next(ticks)
    launches = []

    def timeout_run_gate(*args, **kwargs):
        launches.append((args, kwargs))
        raise module.GateTimeoutError(args[2], None)

    module.run_gate = timeout_run_gate
    output = StringIO()
    with redirect_stdout(output):
        result = module.main()
    require(result == 1, "budget-edge child timeout must fail aggregate")
    require(len(launches) == 1, "aggregate timeout must stop before launching later guards")
    require("aggregate-timeout" in output.getvalue(), "budget-edge timeout must be classified as aggregate-timeout")


def test_cleanup_failure_stops_following_children(module):
    first = fake_gate(module, "preflight-fake-cleanup-failure.py")
    second = fake_gate(module, "preflight-fake-must-not-launch.py")
    module.discover = lambda: [first, second]
    module.build_child_env = lambda source=None: {}
    module.time.monotonic = lambda: 100.0
    launches = []

    def timeout_with_cleanup_failure(*args, **kwargs):
        launches.append((args, kwargs))
        raise module.GateTimeoutError(args[2], "process-tree-cleanup-exit=1")

    module.run_gate = timeout_with_cleanup_failure
    output = StringIO()
    with redirect_stdout(output):
        result = module.main()
    text = output.getvalue()
    require(result == 1, "cleanup failure must fail aggregate")
    require(len(launches) == 1, "cleanup failure must stop before later gates can inherit contaminated runner state")
    require("timeout-cleanup-failed" in text, "cleanup failure must have a deterministic failure classification")
    require("process-tree-cleanup-exit=1" in text, "cleanup diagnostic must identify the bounded cleanup failure")


def main():
    module = load_target()
    test_budget_math(module)

    module = load_target()
    test_process_group_launch_contract(module)

    module = load_target()
    test_run_gate_timeout_requests_tree_cleanup(module)

    module = load_target()
    test_windows_cleanup_is_tree_scoped_and_bounded(module)

    module = load_target()
    test_posix_cleanup_targets_owned_group(module)

    module = load_target()
    test_expired_budget_prevents_launch(module)

    module = load_target()
    test_remaining_budget_clips_gate_timeout(module)

    module = load_target()
    test_timeout_at_budget_edge_stops_following_children(module)

    module = load_target()
    test_cleanup_failure_stops_following_children(module)

    print("PASS: aggregate preflight wall-clock, bounded-output, and process-tree timeout cleanup contracts are fail-closed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
