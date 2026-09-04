#!/usr/bin/env python3
"""Regression: aggregate feature-gate source admission must have a fleet-wide byte budget."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "preflight-all.py"


def load_runner():
    spec = importlib.util.spec_from_file_location("qs3d_preflight_all_admission_budget", RUNNER)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"could not load {RUNNER}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    try:
        spec.loader.exec_module(module)
    except Exception:
        sys.modules.pop(spec.name, None)
        raise
    return module


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    runner = load_runner()
    require(
        hasattr(runner, "MAX_TOTAL_FEATURE_GATE_SOURCE_BYTES"),
        "aggregate runner must define a fleet-wide admitted source byte budget",
    )

    with tempfile.TemporaryDirectory(prefix="qs3d-preflight-admission-budget-") as temp_text:
        temp = Path(temp_text)
        first = temp / "preflight-a.py"
        second = temp / "preflight-b.py"
        first.write_bytes(b"12345678")
        second.write_bytes(b"abcdefgh")

        original_root = runner.ROOT
        original_scripts = runner.SCRIPTS
        original_self = runner.SELF
        original_gate_limit = runner.MAX_FEATURE_GATES
        original_source_limit = runner.MAX_FEATURE_GATE_SOURCE_BYTES
        original_total_limit = runner.MAX_TOTAL_FEATURE_GATE_SOURCE_BYTES
        try:
            runner.ROOT = temp
            runner.SCRIPTS = temp
            runner.SELF = temp / "preflight-all.py"
            runner.MAX_FEATURE_GATES = 10
            runner.MAX_FEATURE_GATE_SOURCE_BYTES = 32
            runner.MAX_TOTAL_FEATURE_GATE_SOURCE_BYTES = 12
            runner._ADMITTED_GATES.clear()

            try:
                runner.discover()
            except RuntimeError as exc:
                message = str(exc)
                require(
                    "aggregate" in message.lower() and "source" in message.lower() and "bytes" in message.lower(),
                    f"aggregate source-budget failure must be explicit, got: {message!r}",
                )
            else:
                raise AssertionError("aggregate source admission exceeded its byte budget without failing closed")

            require(
                not runner._ADMITTED_GATES,
                "failed aggregate admission must not publish a partial admitted-gate fleet",
            )
        finally:
            runner.ROOT = original_root
            runner.SCRIPTS = original_scripts
            runner.SELF = original_self
            runner.MAX_FEATURE_GATES = original_gate_limit
            runner.MAX_FEATURE_GATE_SOURCE_BYTES = original_source_limit
            runner.MAX_TOTAL_FEATURE_GATE_SOURCE_BYTES = original_total_limit
            runner._ADMITTED_GATES.clear()

    print("PASS aggregate feature preflight admitted-source byte budget")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
