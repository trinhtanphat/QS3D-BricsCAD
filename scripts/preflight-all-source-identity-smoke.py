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

        helper_path = temp / "identity_peer.py"
        helper_path.write_text("VALUE = 'peer-import-ok'\n", encoding="utf-8")
        semantics_marker = temp / "semantics.txt"
        semantics_gate = temp / "semantics_gate.py"
        semantics_gate.write_text(
            "from pathlib import Path\n"
            "import os\n"
            "import sys\n"
            "import identity_peer\n"
            "expected = os.environ['QS3D_EXPECTED_GATE']\n"
            "checks = [\n"
            "    sys.argv[0] == expected,\n"
            "    __file__ == expected,\n"
            "    identity_peer.VALUE == 'peer-import-ok',\n"
            "]\n"
            "Path(os.environ['QS3D_SEMANTICS_MARKER']).write_text('ok' if all(checks) else repr((sys.argv[0], __file__, sys.path[0])), encoding='utf-8')\n"
            "raise SystemExit(0 if all(checks) else 23)\n",
            encoding="utf-8",
        )
        semantics = runner.admit_gate(semantics_gate, allowed_root=temp)
        semantics_env = runner.build_child_env(
            {
                **os.environ,
                "QS3D_EXPECTED_GATE": str(semantics_gate),
                "QS3D_SEMANTICS_MARKER": str(semantics_marker),
            }
        )
        semantics_returncode = runner.run_gate(semantics, semantics_env, 10)
        semantics_observed = semantics_marker.read_text(encoding="utf-8") if semantics_marker.exists() else "<missing>"
        if semantics_returncode != 0 or semantics_observed != "ok":
            raise RuntimeError(
                "admitted-byte execution changed direct-script argv/import semantics; "
                f"exit={semantics_returncode}, observed={semantics_observed!r}"
            )

    print("PASS: aggregate feature preflight executes admitted bytes while preserving direct-script execution semantics.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
