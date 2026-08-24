#!/usr/bin/env python3
from contextlib import redirect_stdout
from importlib.util import module_from_spec, spec_from_file_location
from io import StringIO
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
    require(module.remaining_child_timeout(100.0, 100.0) == float(module.CHILD_TIMEOUT_SECONDS), "fresh child must retain normal timeout")
    require(module.remaining_child_timeout(100.0, 850.0) == 150.0, "child timeout must be clipped to remaining aggregate budget")
    require(module.remaining_child_timeout(100.0, 1000.0) == 0.0, "expired aggregate budget must return zero")
    require(module.remaining_child_timeout(100.0, 99.0) == float(module.CHILD_TIMEOUT_SECONDS), "monotonic anomaly must not enlarge child timeout")


def test_expired_budget_prevents_launch(module):
    gate = fake_gate(module)
    module.discover = lambda: [gate]
    module.build_child_env = lambda source=None: {}
    ticks = iter([100.0, 100.0 + module.AGGREGATE_TIMEOUT_SECONDS])
    module.time.monotonic = lambda: next(ticks)
    launched = []

    def forbidden_run(*args, **kwargs):
        launched.append((args, kwargs))
        raise AssertionError("aggregate runner launched a child after budget exhaustion")

    module.subprocess.run = forbidden_run
    output = StringIO()
    with redirect_stdout(output):
        result = module.main()
    require(result == 1, "expired aggregate budget must fail closed")
    require(not launched, "expired aggregate budget must fail before child launch")
    require("aggregate-timeout" in output.getvalue(), "expired budget must report aggregate-timeout")


def test_remaining_budget_clips_subprocess_timeout(module):
    gate = fake_gate(module, "preflight-fake-clipped-budget.py")
    module.discover = lambda: [gate]
    module.build_child_env = lambda source=None: {"QS3D_SENTINEL": "1"}
    ticks = iter([100.0, 100.0 + module.AGGREGATE_TIMEOUT_SECONDS - 25.0])
    module.time.monotonic = lambda: next(ticks)
    observed = []

    def fake_run(*args, **kwargs):
        observed.append((args, kwargs))
        return SimpleNamespace(returncode=0)

    module.subprocess.run = fake_run
    with redirect_stdout(StringIO()):
        result = module.main()
    require(result == 0, "clipped child inside remaining budget must still pass")
    require(len(observed) == 1, "exactly one child launch expected")
    require(observed[0][1]["timeout"] == 25.0, "subprocess timeout must equal remaining aggregate budget")
    require(observed[0][1]["timeout"] <= module.CHILD_TIMEOUT_SECONDS, "aggregate clipping must never weaken child timeout ceiling")


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

    def timeout_run(*args, **kwargs):
        launches.append((args, kwargs))
        raise subprocess.TimeoutExpired(cmd=args[0] if args else "fake", timeout=kwargs["timeout"])

    module.subprocess.run = timeout_run
    output = StringIO()
    with redirect_stdout(output):
        result = module.main()
    require(result == 1, "budget-edge child timeout must fail aggregate")
    require(len(launches) == 1, "aggregate timeout must stop before launching later guards")
    require("aggregate-timeout" in output.getvalue(), "budget-edge timeout must be classified as aggregate-timeout")


def main():
    module = load_target()
    test_budget_math(module)

    module = load_target()
    test_expired_budget_prevents_launch(module)

    module = load_target()
    test_remaining_budget_clips_subprocess_timeout(module)

    module = load_target()
    test_timeout_at_budget_edge_stops_following_children(module)

    print("PASS: aggregate preflight wall-clock budget is bounded and fail-closed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
