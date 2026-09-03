#!/usr/bin/env python3
"""Regression for aggregate feature-preflight source identity binding."""

from __future__ import annotations

import importlib.util
import os
from pathlib import Path
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "preflight-all.py"


def load_runner():
    spec = importlib.util.spec_from_file_location("qs3d_preflight_all_identity", RUNNER)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load aggregate preflight runner")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    runner = load_runner()
    required = ("admit_gate", "run_gate", "build_child_env")
    missing = [name for name in required if not hasattr(runner, name)]
    if missing:
        raise RuntimeError("aggregate runner is missing source-identity API: " + ", ".join(missing))

    with tempfile.TemporaryDirectory(prefix="qs3d-preflight-identity-") as temp_text:
        temp = Path(temp_text)
        gate_path = temp / "candidate.py"
        marker = temp / "marker.txt"
        original = "from pathlib import Path\nimport os\nPath(os.environ['QS3D_IDENTITY_MARKER']).write_text('original', encoding='utf-8')\n"
        replacement = "from pathlib import Path\nimport os\nPath(os.environ['QS3D_IDENTITY_MARKER']).write_text('replacement', encoding='utf-8')\n"
        gate_path.write_text(original, encoding="utf-8")

        admitted = runner.admit_gate(gate_path, allowed_root=temp)
        gate_path.write_text(replacement, encoding="utf-8")

        env = runner.build_child_env({**os.environ, "QS3D_IDENTITY_MARKER": str(marker)})
        returncode = runner.run_gate(admitted, env, 10)
        if returncode != 0:
            raise RuntimeError(f"admitted gate execution failed with exit={returncode}")
        observed = marker.read_text(encoding="utf-8") if marker.exists() else "<missing>"
        if observed != "original":
            raise RuntimeError(
                "aggregate runner reopened mutable gate pathname after admission; "
                f"observed marker {observed!r}"
            )

    print("PASS: aggregate feature preflight executes the exact source bytes admitted before pathname replacement.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
